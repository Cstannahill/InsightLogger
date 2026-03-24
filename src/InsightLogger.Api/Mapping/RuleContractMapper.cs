using System;
using System.Collections.Generic;
using System.Linq;
using InsightLogger.Application.Rules.Commands;
using InsightLogger.Application.Rules.DTOs;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Contracts.Rules;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Api.Mapping;

public static class RuleContractMapper
{
    public static CreateRuleCommand ToCommand(CreateRuleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Conditions);
        ArgumentNullException.ThrowIfNull(request.Actions);

        return new CreateRuleCommand(
            Name: request.Name?.Trim() ?? string.Empty,
            Description: request.Description?.Trim(),
            IsEnabled: request.IsEnabled,
            Priority: request.Priority,
            ToolKind: ParseToolOrNull(request.Conditions.Tool),
            Code: request.Conditions.Code?.Trim(),
            Severity: ParseSeverityOrNull(request.Conditions.Severity),
            Category: ParseCategoryOrNull(request.Conditions.Category),
            MessageRegex: request.Conditions.MessageRegex?.Trim(),
            FilePathRegex: request.Conditions.FilePathRegex?.Trim(),
            Fingerprint: request.Conditions.Fingerprint?.Trim(),
            Title: request.Actions.Title?.Trim(),
            Explanation: request.Actions.Explanation?.Trim(),
            SuggestedFixes: NormalizeValues(request.Actions.SuggestedFixes),
            ConfidenceAdjustment: request.Actions.ConfidenceAdjustment ?? 0d,
            MarkAsPrimaryCause: request.Actions.MarkAsPrimaryCause ?? false,
            Tags: NormalizeValues(request.Tags),
            ProjectName: request.Conditions.ProjectName?.Trim(),
            Repository: request.Conditions.Repository?.Trim());
    }

    public static UpdateRuleCommand ToCommand(string id, UpdateRuleRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Conditions);
        ArgumentNullException.ThrowIfNull(request.Actions);

        return new UpdateRuleCommand(
            Id: id.Trim(),
            Name: request.Name?.Trim() ?? string.Empty,
            Description: request.Description?.Trim(),
            IsEnabled: request.IsEnabled,
            Priority: request.Priority,
            ToolKind: ParseToolOrNull(request.Conditions.Tool),
            Code: request.Conditions.Code?.Trim(),
            Severity: ParseSeverityOrNull(request.Conditions.Severity),
            Category: ParseCategoryOrNull(request.Conditions.Category),
            MessageRegex: request.Conditions.MessageRegex?.Trim(),
            FilePathRegex: request.Conditions.FilePathRegex?.Trim(),
            Fingerprint: request.Conditions.Fingerprint?.Trim(),
            Title: request.Actions.Title?.Trim(),
            Explanation: request.Actions.Explanation?.Trim(),
            SuggestedFixes: NormalizeValues(request.Actions.SuggestedFixes),
            ConfidenceAdjustment: request.Actions.ConfidenceAdjustment ?? 0d,
            MarkAsPrimaryCause: request.Actions.MarkAsPrimaryCause ?? false,
            Tags: NormalizeValues(request.Tags),
            ProjectName: request.Conditions.ProjectName?.Trim(),
            Repository: request.Conditions.Repository?.Trim());
    }

    public static TestRuleCommand ToCommand(RuleTestRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new TestRuleCommand(
            Content: request.Content?.Trim() ?? string.Empty,
            InputType: ParseInputTypeOrDefault(request.InputType),
            ToolHint: ParseToolOrNull(request.Tool),
            RuleId: request.RuleId?.Trim(),
            DraftRule: request.Rule is null ? null : ToCommand(request.Rule),
            Context: BuildRuleTestContext(request));
    }

    public static CreateRuleResponse ToResponse(CreatedRuleDto dto)
        => new(
            Id: dto.Id,
            Name: dto.Name,
            IsEnabled: dto.IsEnabled,
            Priority: dto.Priority,
            CreatedAt: dto.CreatedAtUtc);

    public static GetRulesResponse ToResponse(RuleListResultDto dto)
        => new(
            Items: dto.Items.Select(ToContract).ToArray(),
            Total: dto.Total);

    public static GetRuleResponse ToResponse(RuleDetailsDto dto)
        => new(
            Id: dto.Id,
            Name: dto.Name,
            Description: dto.Description,
            Priority: dto.Priority,
            IsEnabled: dto.IsEnabled,
            Conditions: new RuleConditionContract(
                Tool: dto.Tool is null ? null : ToContractTool(ParseToolOrNull(dto.Tool) ?? ToolKind.Unknown),
                Code: dto.Code,
                Severity: dto.Severity is null ? null : ToContractSeverity(ParseSeverityOrNull(dto.Severity) ?? Severity.Unknown),
                Category: dto.Category is null ? null : ToContractCategory(ParseCategoryOrNull(dto.Category) ?? DiagnosticCategory.Unknown),
                MessageRegex: dto.MessageRegex,
                FilePathRegex: dto.FilePathRegex,
                Fingerprint: dto.Fingerprint,
                ProjectName: dto.ProjectName,
                Repository: dto.Repository),
            Actions: new RuleActionContract(
                Title: dto.Title,
                Explanation: dto.Explanation,
                SuggestedFixes: dto.SuggestedFixes,
                ConfidenceAdjustment: dto.ConfidenceAdjustment,
                MarkAsPrimaryCause: dto.MarkAsPrimaryCause),
            Tags: dto.Tags,
            MatchCount: dto.MatchCount,
            LastMatchedAt: dto.LastMatchedAtUtc,
            CreatedAt: dto.CreatedAtUtc,
            UpdatedAt: dto.UpdatedAtUtc);

    public static SetRuleEnabledResponse ToResponse(RuleEnabledStateDto dto)
        => new(
            Id: dto.Id,
            IsEnabled: dto.IsEnabled,
            UpdatedAt: dto.UpdatedAtUtc);

    public static RuleTestResponse ToResponse(RuleTestResultDto dto)
        => new(
            Matched: dto.Matches.Count > 0,
            Rule: new RuleTestedRuleContract(
                Id: dto.RuleId,
                Name: dto.RuleName,
                IsEnabled: dto.IsEnabled,
                Priority: dto.Priority,
                IsPersisted: dto.IsPersisted),
            ToolDetected: ToContractTool(dto.ToolDetected),
            DiagnosticCount: dto.Diagnostics.Count,
            GroupCount: dto.Groups.Count,
            Diagnostics: dto.Diagnostics.Select(ToContract).ToArray(),
            Groups: dto.Groups.Select(ToContract).ToArray(),
            RootCauseCandidatesBefore: dto.RootCauseCandidatesBefore.Select(ToContract).ToArray(),
            RootCauseCandidatesAfter: dto.RootCauseCandidatesAfter.Select(ToContract).ToArray(),
            Matches: dto.Matches.Select(ToContract).ToArray(),
            Processing: ToContract(dto.Processing));

    private static RuleListItemContract ToContract(RuleListItemDto dto)
        => new(
            Id: dto.Id,
            Name: dto.Name,
            Description: dto.Description,
            IsEnabled: dto.IsEnabled,
            Priority: dto.Priority,
            Tags: dto.Tags,
            MatchCount: dto.MatchCount,
            LastMatchedAt: dto.LastMatchedAtUtc,
            UpdatedAt: dto.UpdatedAtUtc,
            ProjectName: dto.ProjectName,
            Repository: dto.Repository);

    private static RuleTestMatchContract ToContract(RuleTestMatchDto dto)
        => new(
            RuleId: dto.RuleId,
            RuleName: dto.RuleName,
            TargetType: dto.TargetType,
            TargetId: dto.TargetId,
            MatchedFingerprint: dto.MatchedFingerprint,
            MatchedConditions: dto.MatchedConditions,
            AppliedActions: dto.AppliedActions);

    private static DiagnosticContract ToContract(DiagnosticRecord diagnostic) =>
        new(
            Id: diagnostic.Id,
            Tool: ToContractTool(diagnostic.ToolKind),
            Code: diagnostic.Code,
            Severity: ToContractSeverity(diagnostic.Severity),
            Message: diagnostic.Message,
            NormalizedMessage: diagnostic.NormalizedMessage,
            FilePath: diagnostic.Location?.FilePath,
            Line: diagnostic.Location?.Line,
            Column: diagnostic.Location?.Column,
            EndLine: diagnostic.Location?.EndLine,
            EndColumn: diagnostic.Location?.EndColumn,
            Category: ToContractCategory(diagnostic.Category),
            Subcategory: diagnostic.Subcategory,
            Fingerprint: diagnostic.Fingerprint?.Value,
            IsPrimaryCandidate: diagnostic.IsPrimaryCandidate);

    private static DiagnosticGroupContract ToContract(DiagnosticGroup group) =>
        new(
            Fingerprint: group.Fingerprint.Value,
            Count: group.Count,
            GroupReason: group.GroupReason,
            PrimaryDiagnosticId: group.PrimaryDiagnosticId,
            RelatedDiagnosticIds: group.RelatedDiagnosticIds);

    private static RootCauseCandidateContract ToContract(RootCauseCandidate candidate) =>
        new(
            Fingerprint: candidate.Fingerprint.Value,
            Title: candidate.Title,
            Explanation: candidate.Explanation,
            Confidence: candidate.Confidence,
            Signals: candidate.Signals,
            SuggestedFixes: candidate.SuggestedFixes);

    private static ProcessingMetadataContract ToContract(ProcessingMetadata metadata) =>
        new(
            UsedAi: metadata.UsedAi,
            DurationMs: metadata.DurationMs,
            Parser: metadata.Parser,
            CorrelationId: metadata.CorrelationId,
            ToolDetectionConfidence: metadata.ToolDetectionConfidence,
            ParseConfidence: metadata.ParseConfidence,
            UnparsedSegmentCount: metadata.UnparsedSegmentCount,
            Notes: metadata.Notes);

    private static IReadOnlyList<string> NormalizeValues(IReadOnlyList<string>? values)
        => values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();

    private static IReadOnlyDictionary<string, string>? BuildRuleTestContext(RuleTestRequest request)
    {
        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(request.ProjectName))
        {
            context["projectName"] = request.ProjectName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Repository))
        {
            context["repository"] = request.Repository.Trim();
        }

        return context.Count == 0 ? null : context;
    }

    private static ToolKind? ParseToolOrNull(string? value)
        => AnalysisContractMapper.TryParseTool(value, out var parsed) ? parsed : null;

    private static Severity? ParseSeverityOrNull(string? value)
        => Enum.TryParse<Severity>(value, ignoreCase: true, out var parsed) ? parsed : null;

    private static InputType ParseInputTypeOrDefault(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return InputType.SingleDiagnostic;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "build-log" => InputType.BuildLog,
            "compiler-error" or "single-diagnostic" => InputType.SingleDiagnostic,
            _ => InputType.SingleDiagnostic
        };
    }

    private static DiagnosticCategory? ParseCategoryOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);

        return Enum.TryParse<DiagnosticCategory>(normalized, ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }

    private static string ToContractTool(ToolKind toolKind) => toolKind switch
    {
        ToolKind.DotNet => "dotnet",
        ToolKind.TypeScript => "typescript",
        ToolKind.Npm => "npm",
        ToolKind.Vite => "vite",
        ToolKind.Python => "python",
        ToolKind.Generic => "generic",
        _ => "unknown"
    };

    private static string ToContractSeverity(Severity severity) => severity switch
    {
        Severity.Info => "info",
        Severity.Warning => "warning",
        Severity.Error => "error",
        Severity.Fatal => "fatal",
        _ => "unknown"
    };

    private static string ToContractCategory(DiagnosticCategory category) => category switch
    {
        DiagnosticCategory.MissingSymbol => "missing-symbol",
        DiagnosticCategory.TypeMismatch => "type-mismatch",
        DiagnosticCategory.NullableSafety => "nullable-safety",
        DiagnosticCategory.RuntimeEnvironment => "runtime-environment",
        DiagnosticCategory.BuildSystem => "build-system",
        DiagnosticCategory.TestFailure => "test-failure",
        _ => category.ToString().ToLowerInvariant()
    };
}
