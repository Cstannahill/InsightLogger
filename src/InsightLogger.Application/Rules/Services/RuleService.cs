using System;
using System.Diagnostics;
using System.Linq;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Analyses.Services;
using InsightLogger.Application.Rules.Commands;
using InsightLogger.Application.Rules.DTOs;
using InsightLogger.Application.Rules.Exceptions;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;

namespace InsightLogger.Application.Rules.Services;

public sealed class RuleService : IRuleService
{
    private const string InlineRuleId = "rule_test_inline";

    private readonly IRuleRepository _ruleRepository;
    private readonly IToolDetector _toolDetector;
    private readonly IDiagnosticParserCoordinator _parserCoordinator;
    private readonly DiagnosticGroupingService _groupingService;
    private readonly RootCauseRankingService _rankingService;
    private readonly RuleMatchingService _ruleMatchingService;

    public RuleService(
        IRuleRepository ruleRepository,
        IToolDetector toolDetector,
        IDiagnosticParserCoordinator parserCoordinator,
        DiagnosticGroupingService groupingService,
        RootCauseRankingService rankingService,
        RuleMatchingService ruleMatchingService)
    {
        _ruleRepository = ruleRepository;
        _toolDetector = toolDetector;
        _parserCoordinator = parserCoordinator;
        _groupingService = groupingService;
        _rankingService = rankingService;
        _ruleMatchingService = ruleMatchingService;
    }

    public async Task<CreatedRuleDto> CreateAsync(CreateRuleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await _ruleRepository.ExistsByNameAsync(command.Name, cancellationToken: cancellationToken))
        {
            throw new RuleAlreadyExistsException(command.Name);
        }

        var rule = BuildRule(command, id: $"rule_{Guid.NewGuid():N}");
        var created = await _ruleRepository.CreateAsync(rule, cancellationToken);

        return new CreatedRuleDto(
            Id: created.Id,
            Name: created.Name,
            IsEnabled: created.IsEnabled,
            Priority: created.Priority,
            CreatedAtUtc: created.CreatedAtUtc);
    }

    public async Task<RuleListResultDto> ListAsync(
        bool? isEnabled,
        string? tool,
        string? tag,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        if (limit is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 100.");
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be greater than or equal to zero.");
        }

        var toolKind = ParseToolOrNull(tool);
        var items = await _ruleRepository.ListAsync(isEnabled, toolKind, tag, limit, offset, cancellationToken);
        var total = await _ruleRepository.CountAsync(isEnabled, toolKind, tag, cancellationToken);

        return new RuleListResultDto(items.Select(ToListItemDto).ToArray(), total);
    }

    public async Task<RuleDetailsDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var rule = await _ruleRepository.GetByIdAsync(id.Trim(), cancellationToken);
        return rule is null ? null : ToDetailsDto(rule);
    }

    public async Task<RuleDetailsDto?> UpdateAsync(UpdateRuleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await _ruleRepository.GetByIdAsync(command.Id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (await _ruleRepository.ExistsByNameAsync(command.Name, command.Id, cancellationToken))
        {
            throw new RuleAlreadyExistsException(command.Name);
        }

        var updated = BuildRule(
            command,
            existing.Id,
            existing.CreatedAtUtc,
            DateTimeOffset.UtcNow,
            existing.MatchCount,
            existing.LastMatchedAtUtc);

        var persisted = await _ruleRepository.UpdateAsync(updated, cancellationToken);
        return ToDetailsDto(persisted);
    }

    public async Task<RuleEnabledStateDto?> SetEnabledAsync(string id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var existing = await _ruleRepository.GetByIdAsync(id.Trim(), cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (existing.IsEnabled == isEnabled)
        {
            return new RuleEnabledStateDto(existing.Id, existing.IsEnabled, existing.UpdatedAtUtc);
        }

        var updated = new Rule(
            id: existing.Id,
            name: existing.Name,
            description: existing.Description,
            isEnabled: isEnabled,
            priority: existing.Priority,
            condition: existing.Condition,
            action: existing.Action,
            tags: existing.Tags,
            createdAtUtc: existing.CreatedAtUtc,
            updatedAtUtc: DateTimeOffset.UtcNow,
            matchCount: existing.MatchCount,
            lastMatchedAtUtc: existing.LastMatchedAtUtc);

        var persisted = await _ruleRepository.UpdateAsync(updated, cancellationToken);
        return new RuleEnabledStateDto(persisted.Id, persisted.IsEnabled, persisted.UpdatedAtUtc);
    }

    public async Task<RuleTestResultDto?> TestAsync(TestRuleCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Content);

        var testedRule = await ResolveRuleForTestAsync(command, cancellationToken);
        if (testedRule is null)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();
        var toolDetection = await _toolDetector.DetectAsync(command.Content, command.ToolHint, cancellationToken);
        var coordinatedParse = await _parserCoordinator.ParseAsync(
            command.Content,
            command.InputType,
            toolDetection.ToolKind,
            correlationId: null,
            cancellationToken: cancellationToken);

        var diagnostics = coordinatedParse.ParseResult?.Diagnostics ?? Array.Empty<DiagnosticRecord>();
        var groups = _groupingService.Group(diagnostics);
        var rootCauseCandidatesBefore = _rankingService.Rank(
            diagnostics,
            groups,
            coordinatedParse.ParseResult?.ParseConfidence ?? 0d);

        var preview = await _ruleMatchingService.EvaluateAsync(
            new[] { testedRule },
            diagnostics,
            groups,
            rootCauseCandidatesBefore,
            command.Context,
            cancellationToken);

        stopwatch.Stop();

        var processing = new ProcessingMetadata(
            UsedAi: false,
            DurationMs: (int)Math.Max(1, stopwatch.ElapsedMilliseconds),
            Parser: coordinatedParse.SelectedParserName,
            CorrelationId: null,
            ToolDetectionConfidence: toolDetection.Confidence,
            ParseConfidence: coordinatedParse.ParseResult?.ParseConfidence ?? 0d,
            UnparsedSegmentCount: coordinatedParse.ParseResult?.UnparsedSegments?.Count ?? 0,
            Notes: coordinatedParse.FailureReason);

        return new RuleTestResultDto(
            RuleId: command.RuleId is null ? null : testedRule.Id,
            RuleName: testedRule.Name,
            IsEnabled: testedRule.IsEnabled,
            Priority: testedRule.Priority,
            IsPersisted: command.RuleId is not null,
            ToolDetected: toolDetection.ToolKind,
            Diagnostics: diagnostics,
            Groups: groups,
            RootCauseCandidatesBefore: rootCauseCandidatesBefore,
            RootCauseCandidatesAfter: preview.RootCauseCandidates,
            Matches: preview.Applications.Select(ToTestMatchDto).ToArray(),
            Processing: processing);
    }

    private async Task<Rule?> ResolveRuleForTestAsync(TestRuleCommand command, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.RuleId))
        {
            return await _ruleRepository.GetByIdAsync(command.RuleId.Trim(), cancellationToken);
        }

        if (command.DraftRule is null)
        {
            return null;
        }

        return BuildRule(command.DraftRule, InlineRuleId);
    }

    private static RuleTestMatchDto ToTestMatchDto(RuleApplicationResult application)
        => new(
            RuleId: application.Rule.Id,
            RuleName: application.Rule.Name,
            TargetType: application.TargetType,
            TargetId: application.TargetId,
            MatchedFingerprint: application.MatchedFingerprint,
            MatchedConditions: application.MatchedConditions,
            AppliedActions: application.AppliedActions);

    private static Rule BuildRule(
        CreateRuleCommand command,
        string id,
        DateTimeOffset? createdAtUtc = null,
        DateTimeOffset? updatedAtUtc = null)
    {
        return new Rule(
            id: id,
            name: command.Name,
            description: command.Description,
            isEnabled: command.IsEnabled,
            priority: command.Priority,
            condition: new RuleCondition(
                ToolKind: command.ToolKind,
                Code: command.Code,
                Severity: command.Severity,
                Category: command.Category,
                MessageRegex: command.MessageRegex,
                FilePathRegex: command.FilePathRegex,
                Fingerprint: command.Fingerprint,
                ProjectName: command.ProjectName,
                Repository: command.Repository),
            action: new RuleAction(
                Title: command.Title,
                Explanation: command.Explanation,
                SuggestedFixes: command.SuggestedFixes,
                ConfidenceAdjustment: command.ConfidenceAdjustment,
                MarkAsPrimaryCause: command.MarkAsPrimaryCause),
            tags: command.Tags,
            createdAtUtc: createdAtUtc,
            updatedAtUtc: updatedAtUtc);
    }

    private static Rule BuildRule(
        UpdateRuleCommand command,
        string id,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int matchCount,
        DateTimeOffset? lastMatchedAtUtc)
    {
        return new Rule(
            id: id,
            name: command.Name,
            description: command.Description,
            isEnabled: command.IsEnabled,
            priority: command.Priority,
            condition: new RuleCondition(
                ToolKind: command.ToolKind,
                Code: command.Code,
                Severity: command.Severity,
                Category: command.Category,
                MessageRegex: command.MessageRegex,
                FilePathRegex: command.FilePathRegex,
                Fingerprint: command.Fingerprint,
                ProjectName: command.ProjectName,
                Repository: command.Repository),
            action: new RuleAction(
                Title: command.Title,
                Explanation: command.Explanation,
                SuggestedFixes: command.SuggestedFixes,
                ConfidenceAdjustment: command.ConfidenceAdjustment,
                MarkAsPrimaryCause: command.MarkAsPrimaryCause),
            tags: command.Tags,
            createdAtUtc: createdAtUtc,
            updatedAtUtc: updatedAtUtc,
            matchCount: matchCount,
            lastMatchedAtUtc: lastMatchedAtUtc);
    }

    private static RuleListItemDto ToListItemDto(Rule rule)
        => new(
            Id: rule.Id,
            Name: rule.Name,
            Description: rule.Description,
            IsEnabled: rule.IsEnabled,
            Priority: rule.Priority,
            Tags: rule.Tags,
            MatchCount: rule.MatchCount,
            LastMatchedAtUtc: rule.LastMatchedAtUtc,
            UpdatedAtUtc: rule.UpdatedAtUtc,
            ProjectName: rule.Condition.ProjectName,
            Repository: rule.Condition.Repository);

    private static RuleDetailsDto ToDetailsDto(Rule rule)
        => new(
            Id: rule.Id,
            Name: rule.Name,
            Description: rule.Description,
            Priority: rule.Priority,
            IsEnabled: rule.IsEnabled,
            Tool: rule.Condition.ToolKind?.ToString(),
            Code: rule.Condition.Code,
            Severity: rule.Condition.Severity?.ToString(),
            Category: rule.Condition.Category?.ToString(),
            MessageRegex: rule.Condition.MessageRegex,
            FilePathRegex: rule.Condition.FilePathRegex,
            Fingerprint: rule.Condition.Fingerprint,
            Title: rule.Action.Title,
            Explanation: rule.Action.Explanation,
            SuggestedFixes: rule.Action.SuggestedFixesOrEmpty,
            ConfidenceAdjustment: rule.Action.ConfidenceAdjustment,
            MarkAsPrimaryCause: rule.Action.MarkAsPrimaryCause,
            Tags: rule.Tags,
            MatchCount: rule.MatchCount,
            LastMatchedAtUtc: rule.LastMatchedAtUtc,
            CreatedAtUtc: rule.CreatedAtUtc,
            UpdatedAtUtc: rule.UpdatedAtUtc,
            ProjectName: rule.Condition.ProjectName,
            Repository: rule.Condition.Repository);

    private static ToolKind? ParseToolOrNull(string? tool)
    {
        if (string.IsNullOrWhiteSpace(tool))
        {
            return null;
        }

        return tool.Trim().ToLowerInvariant() switch
        {
            "dotnet" or ".net" or "roslyn" => ToolKind.DotNet,
            "typescript" or "ts" or "tsc" => ToolKind.TypeScript,
            "npm" => ToolKind.Npm,
            "vite" => ToolKind.Vite,
            "python" or "py" => ToolKind.Python,
            "generic" => ToolKind.Generic,
            _ => Enum.TryParse<ToolKind>(tool, ignoreCase: true, out var parsed) ? parsed : null
        };
    }
}
