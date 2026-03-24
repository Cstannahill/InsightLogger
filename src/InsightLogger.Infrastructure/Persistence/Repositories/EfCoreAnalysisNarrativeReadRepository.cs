using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Analyses.DTOs;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InsightLogger.Infrastructure.Persistence.Repositories;

public sealed class EfCoreAnalysisNarrativeReadRepository : IAnalysisNarrativeReadRepository
{
    private const int SearchCandidateCap = 250;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly InsightLoggerDbContext _dbContext;

    public EfCoreAnalysisNarrativeReadRepository(InsightLoggerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PersistedAnalysisNarrativeDto?> GetByAnalysisIdAsync(
        string analysisId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Analyses
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == analysisId && x.NarrativeSummary != null, cancellationToken);

        return entity is null ? null : MapDetail(entity);
    }

    public async Task<IReadOnlyList<AnalysisNarrativeHistoryItemDto>> GetRecentAsync(
        ToolKind? toolKind,
        string? source,
        string? projectName,
        string? repository,
        string? text,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Analyses
            .AsNoTracking()
            .Where(x => x.NarrativeSummary != null);

        if (toolKind.HasValue)
        {
            var tool = toolKind.Value.ToString();
            query = query.Where(x => x.ToolDetected == tool);
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(x => x.NarrativeSource == source);
        }

        if (!string.IsNullOrWhiteSpace(projectName))
        {
            query = query.Where(x => x.ProjectName == projectName);
        }

        if (!string.IsNullOrWhiteSpace(repository))
        {
            query = query.Where(x => x.Repository == repository);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            var recentEntities = await query.ToListAsync(cancellationToken);

            return recentEntities
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(limit)
                .Select(entity => MapHistory(entity, match: null))
                .ToArray();
        }

        var likePattern = BuildContainsLikePattern(text);
        var candidates = await query
            .Where(x =>
                (x.NarrativeSummary != null && EF.Functions.Like(x.NarrativeSummary, likePattern, "\\")) ||
                (x.NarrativeGroupSummariesJson != null && EF.Functions.Like(x.NarrativeGroupSummariesJson, likePattern, "\\")) ||
                (x.NarrativeRecommendedNextStepsJson != null && EF.Functions.Like(x.NarrativeRecommendedNextStepsJson, likePattern, "\\")) ||
                (x.NarrativeReason != null && EF.Functions.Like(x.NarrativeReason, likePattern, "\\")) ||
                (x.ProjectName != null && EF.Functions.Like(x.ProjectName, likePattern, "\\")) ||
                (x.Repository != null && EF.Functions.Like(x.Repository, likePattern, "\\")) ||
                (x.NarrativeProvider != null && EF.Functions.Like(x.NarrativeProvider, likePattern, "\\")) ||
                (x.NarrativeModel != null && EF.Functions.Like(x.NarrativeModel, likePattern, "\\")))
            .ToListAsync(cancellationToken);

        return candidates
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(SearchCandidateCap)
            .Select(entity => new { Entity = entity, Match = BuildSearchMatch(entity, text) })
            .Where(x => x.Match is not null)
            .OrderByDescending(x => x.Match!.Score)
            .ThenByDescending(x => x.Entity.CreatedAtUtc)
            .Take(limit)
            .Select(x => MapHistory(x.Entity, x.Match))
            .ToArray();
    }

    private static PersistedAnalysisNarrativeDto MapDetail(AnalysisEntity entity)
        => new(
            AnalysisId: entity.Id,
            InputType: ParseInputType(entity.InputType),
            ToolDetected: ParseToolKind(entity.ToolDetected),
            CreatedAtUtc: entity.CreatedAtUtc,
            Summary: new AnalysisSummary(
                TotalDiagnostics: entity.TotalDiagnostics,
                GroupCount: entity.GroupCount,
                PrimaryIssueCount: entity.PrimaryIssueCount,
                ErrorCount: entity.ErrorCount,
                WarningCount: entity.WarningCount),
            Narrative: new AnalysisNarrative(
                Summary: entity.NarrativeSummary ?? string.Empty,
                GroupSummaries: DeserializeItems(entity.NarrativeGroupSummariesJson),
                RecommendedNextSteps: DeserializeItems(entity.NarrativeRecommendedNextStepsJson),
                Source: entity.NarrativeSource ?? "deterministic",
                Provider: entity.NarrativeProvider,
                Model: entity.NarrativeModel,
                Status: entity.NarrativeStatus,
                FallbackUsed: entity.NarrativeFallbackUsed,
                Reason: entity.NarrativeReason),
            ProjectName: entity.ProjectName,
            Repository: entity.Repository);

    private static AnalysisNarrativeHistoryItemDto MapHistory(AnalysisEntity entity, NarrativeSearchMatch? match)
        => new(
            AnalysisId: entity.Id,
            ToolDetected: ParseToolKind(entity.ToolDetected),
            CreatedAtUtc: entity.CreatedAtUtc,
            Summary: new AnalysisSummary(
                TotalDiagnostics: entity.TotalDiagnostics,
                GroupCount: entity.GroupCount,
                PrimaryIssueCount: entity.PrimaryIssueCount,
                ErrorCount: entity.ErrorCount,
                WarningCount: entity.WarningCount),
            SummaryText: entity.NarrativeSummary ?? string.Empty,
            Source: entity.NarrativeSource ?? "deterministic",
            Provider: entity.NarrativeProvider,
            Model: entity.NarrativeModel,
            Status: entity.NarrativeStatus,
            FallbackUsed: entity.NarrativeFallbackUsed,
            ProjectName: entity.ProjectName,
            Repository: entity.Repository,
            MatchedFields: match?.MatchedFields ?? Array.Empty<string>(),
            MatchSnippet: match?.Snippet);

    private static NarrativeSearchMatch? BuildSearchMatch(AnalysisEntity entity, string searchText)
    {
        var normalizedSearchText = searchText.Trim();
        if (normalizedSearchText.Length == 0)
        {
            return null;
        }

        var matchedFields = new List<string>();
        string? snippet = null;
        var score = 0;

        EvaluateField(entity.NarrativeSummary, "summary", 40);
        EvaluateField(string.Join(" ", DeserializeItems(entity.NarrativeGroupSummariesJson)), "groupSummaries", 25);
        EvaluateField(string.Join(" ", DeserializeItems(entity.NarrativeRecommendedNextStepsJson)), "recommendedNextSteps", 20);
        EvaluateField(entity.NarrativeReason, "reason", 15);
        EvaluateField(entity.ProjectName, "projectName", 10);
        EvaluateField(entity.Repository, "repository", 10);
        EvaluateField(entity.NarrativeProvider, "provider", 5);
        EvaluateField(entity.NarrativeModel, "model", 5);

        if (matchedFields.Count == 0)
        {
            return null;
        }

        return new NarrativeSearchMatch(score, matchedFields, snippet);

        void EvaluateField(string? value, string fieldName, int weight)
        {
            if (!Contains(value, normalizedSearchText))
            {
                return;
            }

            matchedFields.Add(fieldName);
            score += weight;
            snippet ??= BuildSnippet(value!, normalizedSearchText);
        }
    }

    private static IReadOnlyList<string> DeserializeItems(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? Array.Empty<string>();
    }

    private static string BuildContainsLikePattern(string value)
        => $"%{EscapeLikeValue(value.Trim())}%";

    private static string EscapeLikeValue(string value)
        => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);

    private static bool Contains(string? value, string searchText)
        => !string.IsNullOrWhiteSpace(value) &&
           value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

    private static string BuildSnippet(string value, string searchText)
    {
        var normalized = NormalizeWhitespace(value);
        var matchIndex = normalized.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
        if (matchIndex < 0)
        {
            return normalized.Length <= 140 ? normalized : normalized[..140] + "...";
        }

        const int radius = 60;
        var start = Math.Max(0, matchIndex - radius);
        var end = Math.Min(normalized.Length, matchIndex + searchText.Length + radius);
        var snippet = normalized[start..end];

        if (start > 0)
        {
            snippet = "..." + snippet;
        }

        if (end < normalized.Length)
        {
            snippet += "...";
        }

        return snippet;
    }

    private static string NormalizeWhitespace(string value)
    {
        var normalized = string.Join(' ', value
            .Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));

        return normalized.Trim();
    }

    private static ToolKind ParseToolKind(string? value)
        => Enum.TryParse<ToolKind>(value, ignoreCase: true, out var parsed) ? parsed : ToolKind.Unknown;

    private static InputType ParseInputType(string? value)
        => Enum.TryParse<InputType>(value, ignoreCase: true, out var parsed) ? parsed : InputType.BuildLog;

    private sealed record NarrativeSearchMatch(int Score, IReadOnlyList<string> MatchedFields, string? Snippet);
}
