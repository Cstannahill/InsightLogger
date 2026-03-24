using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Diagnostics.DTOs;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InsightLogger.Infrastructure.Persistence.Repositories;

public sealed class EfCoreRuleRepository : IRuleRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly InsightLoggerDbContext _dbContext;

    public EfCoreRuleRepository(InsightLoggerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByNameAsync(string name, string? excludingId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalized = name.Trim();

        var query = _dbContext.Rules.Where(rule => rule.Name == normalized);
        if (!string.IsNullOrWhiteSpace(excludingId))
        {
            var excludedId = excludingId.Trim();
            query = query.Where(rule => rule.Id != excludedId);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task<Rule> CreateAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var entity = ToEntity(rule);
        _dbContext.Rules.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return rule;
    }

    public async Task<Rule?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var entity = await _dbContext.Rules
            .AsNoTracking()
            .SingleOrDefaultAsync(rule => rule.Id == id.Trim(), cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Rule>> ListAsync(
        bool? isEnabled,
        ToolKind? toolKind,
        string? tag,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var query = BuildBaseQuery(isEnabled, toolKind);

        var entities = await query
            .OrderByDescending(static rule => rule.Priority)
            .ThenByDescending(static rule => rule.MatchCount)
            .ThenBy(static rule => rule.Name)
            .ToListAsync(cancellationToken);

        var filtered = FilterByTag(entities, tag);

        return filtered
            .Skip(offset)
            .Take(limit)
            .Select(ToDomain)
            .ToArray();
    }

    public async Task<int> CountAsync(
        bool? isEnabled,
        ToolKind? toolKind,
        string? tag,
        CancellationToken cancellationToken = default)
    {
        var query = BuildBaseQuery(isEnabled, toolKind);
        var entities = await query.ToListAsync(cancellationToken);
        return FilterByTag(entities, tag).Count;
    }

    public async Task<Rule> UpdateAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var entity = await _dbContext.Rules.SingleAsync(existing => existing.Id == rule.Id, cancellationToken);
        Apply(entity, rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return rule;
    }

    public async Task<IReadOnlyList<Rule>> GetEnabledRulesAsync(
        ToolKind? toolKind,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Rules
            .AsNoTracking()
            .Where(static rule => rule.IsEnabled);

        if (toolKind is not null)
        {
            var tool = toolKind.Value.ToString();
            query = query.Where(rule => rule.ToolKindCondition == null || rule.ToolKindCondition == tool);
        }

        var entities = await query
            .OrderByDescending(static rule => rule.Priority)
            .ThenByDescending(static rule => rule.MatchCount)
            .ThenBy(static rule => rule.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToArray();
    }

    public async Task RecordMatchesAsync(
        IReadOnlyList<string> ruleIds,
        DateTimeOffset matchedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (ruleIds.Count == 0)
        {
            return;
        }

        var grouped = ruleIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .GroupBy(static id => id, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);

        if (grouped.Count == 0)
        {
            return;
        }

        var ids = grouped.Keys.ToArray();
        var entities = await _dbContext.Rules
            .Where(rule => ids.Contains(rule.Id))
            .ToListAsync(cancellationToken);

        foreach (var entity in entities)
        {
            if (!grouped.TryGetValue(entity.Id, out var hitCount))
            {
                continue;
            }

            entity.MatchCount += hitCount;
            if (entity.LastMatchedAtUtc is null || entity.LastMatchedAtUtc < matchedAtUtc)
            {
                entity.LastMatchedAtUtc = matchedAtUtc;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RelatedRuleSummaryDto>> GetRelatedRuleSummariesByFingerprintAsync(
        string fingerprint,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fingerprint);

        var context = await LoadRelatedRuleContextAsync(fingerprint.Trim(), cancellationToken);
        if (context is null)
        {
            return Array.Empty<RelatedRuleSummaryDto>();
        }

        var entities = await _dbContext.Rules
            .AsNoTracking()
            .Where(static rule => rule.IsEnabled)
            .ToListAsync(cancellationToken);

        return entities
            .Select(rule => TryBuildRelatedSummary(rule, context))
            .Where(static summary => summary is not null)
            .OrderByDescending(static summary => summary!.Score)
            .ThenByDescending(static summary => summary!.Rule.Priority)
            .ThenByDescending(static summary => summary!.Rule.MatchCount)
            .ThenBy(static summary => summary!.Rule.Name)
            .Take(5)
            .Select(static summary => new RelatedRuleSummaryDto(
                summary!.Rule.Id,
                summary.Rule.Name,
                summary.MatchedBy,
                summary.Rule.MatchCount,
                summary.Rule.LastMatchedAtUtc,
                summary.Rule.ProjectNameCondition,
                summary.Rule.RepositoryCondition))
            .ToArray();
    }

    private IQueryable<RuleEntity> BuildBaseQuery(bool? isEnabled, ToolKind? toolKind)
    {
        var query = _dbContext.Rules.AsNoTracking().AsQueryable();

        if (isEnabled.HasValue)
        {
            query = query.Where(rule => rule.IsEnabled == isEnabled.Value);
        }

        if (toolKind is not null)
        {
            var tool = toolKind.Value.ToString();
            query = query.Where(rule => rule.ToolKindCondition == tool);
        }

        return query;
    }

    private async Task<RelatedRuleContext?> LoadRelatedRuleContextAsync(string fingerprint, CancellationToken cancellationToken)
    {
        var pattern = await _dbContext.ErrorPatterns
            .AsNoTracking()
            .Where(entity => entity.Fingerprint == fingerprint)
            .Select(entity => new
            {
                entity.Fingerprint,
                entity.ToolKind,
                entity.Category,
                entity.CanonicalMessage
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (pattern is null)
        {
            return null;
        }

        // SQLite DateTimeOffset ordering can be provider-sensitive; fetch then order on the client.
        var diagnosticCandidates = await (
            from occurrence in _dbContext.PatternOccurrences.AsNoTracking()
            join diagnostic in _dbContext.Diagnostics.AsNoTracking() on occurrence.DiagnosticId equals diagnostic.Id
            join analysis in _dbContext.Analyses.AsNoTracking() on occurrence.AnalysisId equals analysis.Id
            where occurrence.Fingerprint == fingerprint
            select new
            {
                occurrence.SeenAtUtc,
                diagnostic.OrderIndex,
                diagnostic.Code,
                diagnostic.Severity,
                diagnostic.FilePath,
                analysis.ContextJson
            })
            .ToListAsync(cancellationToken);

        var latestDiagnostic = diagnosticCandidates
            .OrderByDescending(static candidate => candidate.SeenAtUtc)
            .ThenBy(static candidate => candidate.OrderIndex)
            .FirstOrDefault();

        var context = DeserializeContext(latestDiagnostic?.ContextJson);

        return new RelatedRuleContext(
            Fingerprint: fingerprint,
            ToolKind: pattern.ToolKind,
            Code: latestDiagnostic?.Code,
            Severity: latestDiagnostic?.Severity,
            Category: pattern.Category,
            CanonicalMessage: pattern.CanonicalMessage,
            FilePath: latestDiagnostic?.FilePath,
            ProjectName: GetContextValue(context, "projectName"),
            Repository: GetContextValue(context, "repository"));
    }

    private static RelatedRuleSummaryCandidate? TryBuildRelatedSummary(RuleEntity rule, RelatedRuleContext context)
    {
        var matchedBy = new List<string>();
        var score = 0;

        if (!MatchesValue(rule.FingerprintCondition, context.Fingerprint, "fingerprint", 120, matchedBy, ref score))
        {
            return null;
        }

        if (!MatchesValue(rule.ToolKindCondition, context.ToolKind, "tool", 10, matchedBy, ref score))
        {
            return null;
        }

        if (!MatchesValue(rule.CodeCondition, context.Code, "code", 50, matchedBy, ref score))
        {
            return null;
        }

        if (!MatchesValue(rule.SeverityCondition, context.Severity, "severity", 12, matchedBy, ref score))
        {
            return null;
        }

        if (!MatchesValue(rule.CategoryCondition, context.Category, "category", 30, matchedBy, ref score))
        {
            return null;
        }

        if (!MatchesRegex(rule.MessageRegexCondition, context.CanonicalMessage, "message-regex", 35, matchedBy, ref score))
        {
            return null;
        }

        if (!MatchesRegex(rule.FilePathRegexCondition, context.FilePath, "file-path-regex", 15, matchedBy, ref score))
        {
            return null;
        }

        if (!MatchesValue(rule.ProjectNameCondition, context.ProjectName, "projectName", 18, matchedBy, ref score))
        {
            return null;
        }

        if (!MatchesValue(rule.RepositoryCondition, context.Repository, "repository", 18, matchedBy, ref score))
        {
            return null;
        }

        if (matchedBy.Count == 0)
        {
            return null;
        }

        return new RelatedRuleSummaryCandidate(rule, matchedBy, score);
    }

    private static bool MatchesValue(
        string? condition,
        string? candidate,
        string matchedByValue,
        int scoreWeight,
        List<string> matchedBy,
        ref int score)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(candidate) || !string.Equals(condition.Trim(), candidate.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        matchedBy.Add(matchedByValue);
        score += scoreWeight;
        return true;
    }

    private static bool MatchesRegex(
        string? pattern,
        string? candidate,
        string matchedByValue,
        int scoreWeight,
        List<string> matchedBy,
        ref int score)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        try
        {
            if (!Regex.IsMatch(candidate, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                return false;
            }
        }
        catch (ArgumentException)
        {
            return false;
        }

        matchedBy.Add(matchedByValue);
        score += scoreWeight;
        return true;
    }

    private static List<RuleEntity> FilterByTag(List<RuleEntity> entities, string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return entities;
        }

        var normalized = tag.Trim();
        return entities
            .Where(entity => DeserializeStringArray(entity.TagsJson)
                .Any(existingTag => string.Equals(existingTag, normalized, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static RuleEntity ToEntity(Rule rule)
    {
        var entity = new RuleEntity { Id = rule.Id };
        Apply(entity, rule);
        return entity;
    }

    private static void Apply(RuleEntity entity, Rule rule)
    {
        entity.Name = rule.Name;
        entity.Description = rule.Description;
        entity.IsEnabled = rule.IsEnabled;
        entity.Priority = rule.Priority;
        entity.ToolKindCondition = rule.Condition.ToolKind?.ToString();
        entity.CodeCondition = rule.Condition.Code;
        entity.SeverityCondition = rule.Condition.Severity?.ToString();
        entity.CategoryCondition = rule.Condition.Category?.ToString();
        entity.MessageRegexCondition = rule.Condition.MessageRegex;
        entity.FilePathRegexCondition = rule.Condition.FilePathRegex;
        entity.FingerprintCondition = rule.Condition.Fingerprint;
        entity.ProjectNameCondition = rule.Condition.ProjectName;
        entity.RepositoryCondition = rule.Condition.Repository;
        entity.TitleAction = rule.Action.Title;
        entity.ExplanationAction = rule.Action.Explanation;
        entity.SuggestedFixesJson = SerializeStringArray(rule.Action.SuggestedFixesOrEmpty);
        entity.ConfidenceAdjustmentAction = rule.Action.ConfidenceAdjustment;
        entity.MarkAsPrimaryCauseAction = rule.Action.MarkAsPrimaryCause;
        entity.TagsJson = SerializeStringArray(rule.Tags);
        entity.MatchCount = rule.MatchCount;
        entity.LastMatchedAtUtc = rule.LastMatchedAtUtc;
        entity.CreatedAtUtc = rule.CreatedAtUtc;
        entity.UpdatedAtUtc = rule.UpdatedAtUtc;
    }

    private static Rule ToDomain(RuleEntity entity)
    {
        return new Rule(
            id: entity.Id,
            name: entity.Name,
            description: entity.Description,
            isEnabled: entity.IsEnabled,
            priority: entity.Priority,
            condition: new RuleCondition(
                ToolKind: Enum.TryParse<ToolKind>(entity.ToolKindCondition, ignoreCase: true, out var toolKind) ? toolKind : null,
                Code: entity.CodeCondition,
                Severity: Enum.TryParse<Severity>(entity.SeverityCondition, ignoreCase: true, out var severity) ? severity : null,
                Category: Enum.TryParse<DiagnosticCategory>(entity.CategoryCondition, ignoreCase: true, out var category) ? category : null,
                MessageRegex: entity.MessageRegexCondition,
                FilePathRegex: entity.FilePathRegexCondition,
                Fingerprint: entity.FingerprintCondition,
                ProjectName: entity.ProjectNameCondition,
                Repository: entity.RepositoryCondition),
            action: new RuleAction(
                Title: entity.TitleAction,
                Explanation: entity.ExplanationAction,
                SuggestedFixes: DeserializeStringArray(entity.SuggestedFixesJson),
                ConfidenceAdjustment: entity.ConfidenceAdjustmentAction,
                MarkAsPrimaryCause: entity.MarkAsPrimaryCauseAction),
            tags: DeserializeStringArray(entity.TagsJson),
            createdAtUtc: entity.CreatedAtUtc,
            updatedAtUtc: entity.UpdatedAtUtc,
            matchCount: entity.MatchCount,
            lastMatchedAtUtc: entity.LastMatchedAtUtc);
    }

    private static string? SerializeStringArray(IReadOnlyList<string> values)
    {
        return values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeStringArray(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? Array.Empty<string>();
    }

    private static IReadOnlyDictionary<string, string>? DeserializeContext(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetContextValue(IReadOnlyDictionary<string, string>? context, string key)
    {
        if (context is null || context.Count == 0)
        {
            return null;
        }

        if (context.TryGetValue(key, out var value))
        {
            return value;
        }

        foreach (var pair in context)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }

    private sealed record RelatedRuleContext(
        string Fingerprint,
        string? ToolKind,
        string? Code,
        string? Severity,
        string? Category,
        string CanonicalMessage,
        string? FilePath,
        string? ProjectName,
        string? Repository);

    private sealed record RelatedRuleSummaryCandidate(
        RuleEntity Rule,
        IReadOnlyList<string> MatchedBy,
        int Score);
}
