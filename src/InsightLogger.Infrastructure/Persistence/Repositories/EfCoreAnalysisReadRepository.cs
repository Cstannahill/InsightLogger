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
                return snapshot;
            }
        }

        return MapFallback(entity);
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
            RawContent: entity.RawContent);

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

    private static ToolKind ParseToolKind(string? value)
        => Enum.TryParse<ToolKind>(value, ignoreCase: true, out var parsed) ? parsed : ToolKind.Unknown;

    private static InputType ParseInputType(string? value)
        => Enum.TryParse<InputType>(value, ignoreCase: true, out var parsed) ? parsed : InputType.BuildLog;

    private static Severity ParseSeverity(string? value)
        => Enum.TryParse<Severity>(value, ignoreCase: true, out var parsed) ? parsed : Severity.Error;

    private static DiagnosticCategory ParseCategory(string? value)
        => Enum.TryParse<DiagnosticCategory>(value, ignoreCase: true, out var parsed) ? parsed : DiagnosticCategory.Unknown;
}
