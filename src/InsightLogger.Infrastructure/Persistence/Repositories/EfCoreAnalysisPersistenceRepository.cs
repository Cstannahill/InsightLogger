using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Analyses.Persistence;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.Infrastructure.Persistence.Entities;

namespace InsightLogger.Infrastructure.Persistence.Repositories;

public sealed class EfCoreAnalysisPersistenceRepository : IAnalysisPersistenceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly InsightLoggerDbContext _dbContext;

    public EfCoreAnalysisPersistenceRepository(InsightLoggerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(AnalysisPersistenceRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _dbContext.Analyses.AddAsync(CreateAnalysisEntity(request), cancellationToken);
        await _dbContext.Diagnostics.AddRangeAsync(CreateDiagnosticEntities(request), cancellationToken);
        await _dbContext.DiagnosticGroups.AddRangeAsync(CreateDiagnosticGroupEntities(request), cancellationToken);
    }

    private static AnalysisEntity CreateAnalysisEntity(AnalysisPersistenceRequest request) =>
        new()
        {
            Id = request.AnalysisId,
            InputType = request.InputType.ToString(),
            ToolDetected = request.ToolDetected.ToString(),
            TotalDiagnostics = request.Summary.TotalDiagnostics,
            GroupCount = request.Summary.GroupCount,
            PrimaryIssueCount = request.Summary.PrimaryIssueCount,
            ErrorCount = request.Summary.ErrorCount,
            WarningCount = request.Summary.WarningCount,
            UsedAi = request.Processing.UsedAi,
            DurationMs = request.Processing.DurationMs,
            Parser = request.Processing.Parser,
            CorrelationId = request.Processing.CorrelationId,
            ToolDetectionConfidence = request.Processing.ToolDetectionConfidence,
            ParseConfidence = request.Processing.ParseConfidence,
            UnparsedSegmentCount = request.Processing.UnparsedSegmentCount,
            Notes = request.Processing.Notes,
            NarrativeSummary = request.Narrative?.Summary,
            NarrativeGroupSummariesJson = SerializeList(request.Narrative?.GroupSummaries),
            NarrativeRecommendedNextStepsJson = SerializeList(request.Narrative?.RecommendedNextSteps),
            NarrativeSource = request.Narrative?.Source,
            NarrativeProvider = request.Narrative?.Provider,
            NarrativeModel = request.Narrative?.Model,
            NarrativeStatus = request.Narrative?.Status,
            NarrativeFallbackUsed = request.Narrative?.FallbackUsed ?? false,
            NarrativeReason = request.Narrative?.Reason,
            ProjectName = request.ProjectName,
            Repository = request.Repository,
            RawContentHash = request.RawContentHash,
            RawContent = request.RawContent,
            ContextJson = request.Context is null ? null : JsonSerializer.Serialize(request.Context, JsonOptions),
            AnalysisSnapshotJson = JsonSerializer.Serialize(AnalysisPersistenceService.BuildPersistedAnalysisDto(request), JsonOptions),
            CreatedAtUtc = request.CreatedAtUtc
        };

    private static List<DiagnosticEntity> CreateDiagnosticEntities(AnalysisPersistenceRequest request)
    {
        var diagnostics = new List<DiagnosticEntity>(request.Diagnostics.Count);
        for (var i = 0; i < request.Diagnostics.Count; i++)
        {
            var diagnostic = request.Diagnostics[i];
            diagnostics.Add(new DiagnosticEntity
            {
                Id = diagnostic.Id,
                AnalysisId = request.AnalysisId,
                ToolKind = diagnostic.ToolKind.ToString(),
                Source = diagnostic.Source,
                Code = diagnostic.Code,
                Severity = diagnostic.Severity.ToString(),
                Message = diagnostic.Message,
                NormalizedMessage = diagnostic.NormalizedMessage,
                FilePath = diagnostic.Location?.FilePath,
                Line = diagnostic.Location?.Line,
                Column = diagnostic.Location?.Column,
                EndLine = diagnostic.Location?.EndLine,
                EndColumn = diagnostic.Location?.EndColumn,
                RawSnippet = diagnostic.RawSnippet,
                Category = diagnostic.Category.ToString(),
                Subcategory = diagnostic.Subcategory,
                IsPrimaryCandidate = diagnostic.IsPrimaryCandidate,
                Fingerprint = diagnostic.Fingerprint?.Value,
                MetadataJson = SerializeMetadata(diagnostic.Metadata),
                OrderIndex = i
            });
        }

        return diagnostics;
    }

    private static List<DiagnosticGroupEntity> CreateDiagnosticGroupEntities(AnalysisPersistenceRequest request)
    {
        var groups = new List<DiagnosticGroupEntity>(request.Groups.Count);
        for (var i = 0; i < request.Groups.Count; i++)
        {
            var group = request.Groups[i];
            groups.Add(new DiagnosticGroupEntity
            {
                Id = $"grp_{request.AnalysisId}_{i + 1}",
                AnalysisId = request.AnalysisId,
                Fingerprint = group.Fingerprint.Value,
                Count = group.Count,
                GroupReason = group.GroupReason,
                PrimaryDiagnosticId = group.PrimaryDiagnosticId,
                RelatedDiagnosticIdsJson = JsonSerializer.Serialize(group.RelatedDiagnosticIds, JsonOptions),
                OrderIndex = i
            });
        }

        return groups;
    }

    private static string? SerializeMetadata(IReadOnlyDictionary<string, string> metadata)
        => metadata.Count == 0
            ? null
            : JsonSerializer.Serialize(metadata, JsonOptions);

    private static string? SerializeList(IReadOnlyList<string>? items)
        => items is null || items.Count == 0
            ? null
            : JsonSerializer.Serialize(items, JsonOptions);
}
