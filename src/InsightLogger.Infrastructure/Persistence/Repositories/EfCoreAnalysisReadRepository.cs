using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Analyses.DTOs;
using InsightLogger.Application.Knowledge.DTOs;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InsightLogger.Infrastructure.Persistence.Repositories;

public sealed class EfCoreAnalysisReadRepository : IAnalysisReadRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly InsightLoggerDbContext _dbContext;

    public EfCoreAnalysisReadRepository(InsightLoggerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PersistedAnalysisDto?> GetByAnalysisIdAsync(
        string analysisId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Analyses
            .AsNoTracking()
            .Include(x => x.Diagnostics)
            .Include(x => x.Groups)
            .SingleOrDefaultAsync(x => x.Id == analysisId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(entity.AnalysisSnapshotJson))
        {
            var snapshot = JsonSerializer.Deserialize<PersistedAnalysisDto>(entity.AnalysisSnapshotJson, JsonOptions);
            if (snapshot is not null)
            {
                return snapshot with
                {
                    RawContent = entity.RawContent,
                    RawContentRedacted = entity.RawContentRedacted
                };
            }
        }

        return MapFallback(entity);
    }

    public async Task<IReadOnlyList<RelatedAnalysisReferenceDto>> GetRecentRelatedAnalysesAsync(
        IReadOnlyCollection<string> fingerprints,
        string? excludeAnalysisId,
        string? projectName,
        string? repository,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (fingerprints.Count == 0 || limit <= 0)
        {
            return Array.Empty<RelatedAnalysisReferenceDto>();
        }

        IQueryable<PatternOccurrenceEntity> query = _dbContext.PatternOccurrences
            .AsNoTracking()
            .Include(occurrence => occurrence.Analysis)
            .Where(occurrence => fingerprints.Contains(occurrence.Fingerprint));

        if (!string.IsNullOrWhiteSpace(excludeAnalysisId))
        {
            query = query.Where(occurrence => occurrence.AnalysisId != excludeAnalysisId);
        }

        if (!string.IsNullOrWhiteSpace(projectName))
        {
            query = query.Where(occurrence => occurrence.Analysis.ProjectName == projectName);
        }

        if (!string.IsNullOrWhiteSpace(repository))
        {
            query = query.Where(occurrence => occurrence.Analysis.Repository == repository);
        }

        var occurrences = await query.ToListAsync(cancellationToken);

        return occurrences
            .GroupBy(occurrence => occurrence.AnalysisId, StringComparer.Ordinal)
            .Select(group =>
            {
                var latest = group
                    .OrderByDescending(static occurrence => occurrence.Analysis.CreatedAtUtc)
                    .First();

                return new RelatedAnalysisReferenceDto(
                    AnalysisId: latest.AnalysisId,
                    ToolKind: ParseToolKind(latest.Analysis.ToolDetected),
                    CreatedAtUtc: latest.Analysis.CreatedAtUtc,
                    SummaryText: BuildSummaryText(latest.Analysis),
                    ProjectName: latest.Analysis.ProjectName,
                    Repository: latest.Analysis.Repository,
                    MatchingFingerprints: group
                        .Select(static occurrence => occurrence.Fingerprint)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray());
            })
            .OrderByDescending(static item => item.CreatedAtUtc)
            .Take(limit)
            .ToArray();
    }

    public async Task<IReadOnlyList<RelatedAnalysisReferenceDto>> SearchSimilarAnalysesAsync(
        ToolKind toolKind,
        IReadOnlyCollection<string> fingerprints,
        IReadOnlyCollection<string> diagnosticCodes,
        IReadOnlyCollection<DiagnosticCategory> categories,
        IReadOnlyCollection<string> normalizedMessages,
        string? excludeAnalysisId,
        string? projectName,
        string? repository,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return Array.Empty<RelatedAnalysisReferenceDto>();
        }

        var normalizedFingerprints = fingerprints
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var normalizedCodes = diagnosticCodes
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var normalizedCategories = categories
            .Distinct()
            .ToArray();

        var queryTokens = normalizedMessages
            .SelectMany(Tokenize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toolValue = toolKind.ToString();

        var rawCandidates = await _dbContext.Diagnostics
            .AsNoTracking()
            .Include(diagnostic => diagnostic.Analysis)
            .Where(diagnostic => diagnostic.ToolKind == toolValue)
            .Where(diagnostic => string.IsNullOrWhiteSpace(excludeAnalysisId) || diagnostic.AnalysisId != excludeAnalysisId)
            .Where(diagnostic => string.IsNullOrWhiteSpace(projectName) || diagnostic.Analysis.ProjectName == projectName)
            .Where(diagnostic => string.IsNullOrWhiteSpace(repository) || diagnostic.Analysis.Repository == repository)
            .Take(Math.Max(limit * 12, 24))
            .Select(diagnostic => new
            {
                diagnostic.AnalysisId,
                diagnostic.Analysis.CreatedAtUtc,
                diagnostic.ToolKind,
                diagnostic.Code,
                diagnostic.Category,
                diagnostic.Fingerprint,
                diagnostic.NormalizedMessage,
                diagnostic.Message,
                diagnostic.Analysis.ProjectName,
                diagnostic.Analysis.Repository
            })
            .ToListAsync(cancellationToken);

        var candidates = rawCandidates
            .OrderByDescending(static candidate => candidate.CreatedAtUtc)
            .Select(static candidate => new HistoricalCandidate(
                candidate.AnalysisId,
                candidate.CreatedAtUtc,
                ParseToolKind(candidate.ToolKind),
                candidate.Code,
                ParseCategory(candidate.Category),
                candidate.Fingerprint,
                candidate.NormalizedMessage,
                candidate.Message,
                candidate.ProjectName,
                candidate.Repository))
            .ToList();

        if (candidates.Count == 0)
        {
            return Array.Empty<RelatedAnalysisReferenceDto>();
        }

        var ranked = candidates
            .Select(candidate => ScoreCandidate(
                candidate,
                normalizedFingerprints,
                normalizedCodes,
                normalizedCategories,
                queryTokens))
            .Where(static scored => scored.Score >= 0.45d)
            .OrderByDescending(static scored => scored.Score)
            .ThenByDescending(static scored => scored.Candidate.CreatedAtUtc)
            .GroupBy(static scored => scored.Candidate.AnalysisId, StringComparer.Ordinal)
            .Select(static group =>
            {
                var best = group.First();
                return new RelatedAnalysisReferenceDto(
                    AnalysisId: best.Candidate.AnalysisId,
                    ToolKind: best.Candidate.ToolKind,
                    CreatedAtUtc: best.Candidate.CreatedAtUtc,
                    SummaryText: BuildHistoricalSummaryText(best.Candidate, best.MatchType),
                    ProjectName: best.Candidate.ProjectName,
                    Repository: best.Candidate.Repository,
                    MatchingFingerprints: group
                        .Where(static entry => !string.IsNullOrWhiteSpace(entry.Candidate.Fingerprint))
                        .Select(static entry => entry.Candidate.Fingerprint!)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray(),
                    MatchingDiagnosticCodes: group
                        .Where(static entry => !string.IsNullOrWhiteSpace(entry.Candidate.Code))
                        .Select(static entry => entry.Candidate.Code!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    MatchType: best.MatchType,
                    MatchScore: Math.Round(best.Score, 2))
                ;
            })
            .Take(limit)
            .ToArray();

        return ranked;
    }

    private static PersistedAnalysisDto MapFallback(AnalysisEntity entity)
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
            RootCauseCandidates: Array.Empty<RootCauseCandidate>(),
            Groups: entity.Groups
                .OrderBy(x => x.OrderIndex)
                .Select(MapGroup)
                .ToArray(),
            Diagnostics: entity.Diagnostics
                .OrderBy(x => x.OrderIndex)
                .Select(MapDiagnostic)
                .ToArray(),
            MatchedRules: Array.Empty<InsightLogger.Domain.Rules.RuleMatch>(),
            Narrative: MapNarrative(entity),
            Processing: new ProcessingMetadata(
                UsedAi: entity.UsedAi,
                DurationMs: entity.DurationMs,
                Parser: entity.Parser,
                CorrelationId: entity.CorrelationId,
                ToolDetectionConfidence: entity.ToolDetectionConfidence,
                ParseConfidence: entity.ParseConfidence,
                UnparsedSegmentCount: entity.UnparsedSegmentCount,
                Notes: entity.Notes,
                Ai: null,
                AiTasks: Array.Empty<AiProcessingMetadata>()),
            Warnings: Array.Empty<string>(),
            Context: DeserializeContext(entity.ContextJson),
            ProjectName: entity.ProjectName,
            Repository: entity.Repository,
            RawContentHash: entity.RawContentHash,
            RawContentRedacted: entity.RawContentRedacted,
            RawContent: entity.RawContent,
            KnowledgeReferences: Array.Empty<InsightLogger.Application.Abstractions.Knowledge.KnowledgeReference>());

    private static DiagnosticRecord MapDiagnostic(DiagnosticEntity entity)
        => new(
            id: entity.Id,
            toolKind: ParseToolKind(entity.ToolKind),
            severity: ParseSeverity(entity.Severity),
            message: entity.Message,
            rawSnippet: entity.RawSnippet,
            source: entity.Source,
            code: entity.Code,
            normalizedMessage: entity.NormalizedMessage,
            location: new DiagnosticLocation(entity.FilePath, entity.Line, entity.Column, entity.EndLine, entity.EndColumn),
            category: ParseCategory(entity.Category),
            subcategory: entity.Subcategory,
            isPrimaryCandidate: entity.IsPrimaryCandidate,
            fingerprint: string.IsNullOrWhiteSpace(entity.Fingerprint) ? null : new DiagnosticFingerprint(entity.Fingerprint),
            metadata: DeserializeContext(entity.MetadataJson));

    private static DiagnosticGroup MapGroup(DiagnosticGroupEntity entity)
    {
        var relatedDiagnosticIds = DeserializeStringList(entity.RelatedDiagnosticIdsJson);
        var primaryDiagnosticId = !string.IsNullOrWhiteSpace(entity.PrimaryDiagnosticId)
            ? entity.PrimaryDiagnosticId
            : relatedDiagnosticIds.FirstOrDefault() ?? "diag_unknown";

        return new DiagnosticGroup(
            fingerprint: new DiagnosticFingerprint(entity.Fingerprint),
            primaryDiagnosticId: primaryDiagnosticId,
            relatedDiagnosticIds: relatedDiagnosticIds,
            groupReason: entity.GroupReason ?? "exact-fingerprint-dedupe");
    }

    private static AnalysisNarrative? MapNarrative(AnalysisEntity entity)
        => string.IsNullOrWhiteSpace(entity.NarrativeSummary)
            ? null
            : new AnalysisNarrative(
                Summary: entity.NarrativeSummary,
                GroupSummaries: DeserializeStringList(entity.NarrativeGroupSummariesJson),
                RecommendedNextSteps: DeserializeStringList(entity.NarrativeRecommendedNextStepsJson),
                Source: entity.NarrativeSource ?? "deterministic",
                Provider: entity.NarrativeProvider,
                Model: entity.NarrativeModel,
                Status: entity.NarrativeStatus,
                FallbackUsed: entity.NarrativeFallbackUsed,
                Reason: entity.NarrativeReason);

    private static IReadOnlyDictionary<string, string>? DeserializeContext(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
    }

    private static IReadOnlyList<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? Array.Empty<string>();
    }

    private static string BuildSummaryText(AnalysisEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.NarrativeSummary))
        {
            return entity.NarrativeSummary!;
        }

        return $"{entity.TotalDiagnostics} diagnostics across {entity.GroupCount} groups; {entity.PrimaryIssueCount} primary issues identified.";
    }

    private static (HistoricalCandidate Candidate, string MatchType, double Score) ScoreCandidate(
        HistoricalCandidate candidate,
        IReadOnlyCollection<string> fingerprints,
        IReadOnlyCollection<string> diagnosticCodes,
        IReadOnlyCollection<DiagnosticCategory> categories,
        IReadOnlyCollection<string> queryTokens)
    {
        var score = 0d;
        var matchType = "message-similarity";

        if (!string.IsNullOrWhiteSpace(candidate.Fingerprint) &&
            fingerprints.Contains(candidate.Fingerprint, StringComparer.Ordinal))
        {
            score += 0.72d;
            matchType = "fingerprint";
        }

        if (!string.IsNullOrWhiteSpace(candidate.Code) &&
            diagnosticCodes.Contains(candidate.Code, StringComparer.OrdinalIgnoreCase))
        {
            score += 0.55d;
            if (!string.Equals(matchType, "fingerprint", StringComparison.Ordinal))
            {
                matchType = "diagnostic-code";
            }
        }

        if (categories.Contains(candidate.Category))
        {
            score += 0.15d;
        }

        if (queryTokens.Count > 0)
        {
            var candidateTokens = Tokenize(candidate.NormalizedMessage);
            if (candidateTokens.Count > 0)
            {
                var overlapCount = candidateTokens.Intersect(queryTokens, StringComparer.OrdinalIgnoreCase).Count();
                if (overlapCount > 0)
                {
                    score += 0.25d * (double)overlapCount / Math.Max(queryTokens.Count, candidateTokens.Count);
                }
            }
        }

        return (candidate, matchType, Math.Min(score, 0.96d));
    }

    private static IReadOnlyCollection<string> Tokenize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        var separators = new[]
        {
            ' ', '\t', '\r', '\n', ',', '.', ':', ';', '/', '\\', '-', '_', '(', ')', '[', ']', '{', '}', '\'', '"', '`'
        };

        return value
            .Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static token => token.Length >= 3)
            .Select(static token => token.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string BuildHistoricalSummaryText(HistoricalCandidate candidate, string matchType)
    {
        var prefix = matchType switch
        {
            "fingerprint" => "Exact fingerprint match from persisted analysis history.",
            "diagnostic-code" => "Similar persisted diagnostic matched by diagnostic code.",
            _ => "Similar persisted diagnostic from analysis history."
        };

        var message = string.IsNullOrWhiteSpace(candidate.Message) ? candidate.NormalizedMessage : candidate.Message;
        var trimmed = message.Trim();
        if (trimmed.Length > 180)
        {
            trimmed = trimmed[..177] + "...";
        }

        return $"{prefix} {trimmed}";
    }

    private static ToolKind ParseToolKind(string? value)
        => Enum.TryParse<ToolKind>(value, ignoreCase: true, out var parsed) ? parsed : ToolKind.Unknown;

    private static InputType ParseInputType(string? value)
        => Enum.TryParse<InputType>(value, ignoreCase: true, out var parsed) ? parsed : InputType.BuildLog;

    private static Severity ParseSeverity(string? value)
        => Enum.TryParse<Severity>(value, ignoreCase: true, out var parsed) ? parsed : Severity.Error;

    private static DiagnosticCategory ParseCategory(string? value)
        => Enum.TryParse<DiagnosticCategory>(value, ignoreCase: true, out var parsed) ? parsed : DiagnosticCategory.Unknown;

    private sealed record HistoricalCandidate(
        string AnalysisId,
        DateTimeOffset CreatedAtUtc,
        ToolKind ToolKind,
        string? Code,
        DiagnosticCategory Category,
        string? Fingerprint,
        string NormalizedMessage,
        string Message,
        string? ProjectName,
        string? Repository);
}


