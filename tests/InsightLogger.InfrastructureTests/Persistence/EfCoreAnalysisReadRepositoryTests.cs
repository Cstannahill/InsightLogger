using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Application.Analyses.DTOs;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.Infrastructure.Persistence.Entities;
using InsightLogger.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace InsightLogger.InfrastructureTests.Persistence;

public sealed class EfCoreAnalysisReadRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly InsightLoggerDbContext _dbContext;

    public EfCoreAnalysisReadRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<InsightLoggerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new InsightLoggerDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();

        var fingerprint = new DiagnosticFingerprint("fp_cs0103_name_missing");
        var diagnostic = new DiagnosticRecord(
            id: "diag_001",
            toolKind: ToolKind.DotNet,
            severity: Severity.Error,
            message: "The name 'builderz' does not exist in the current context",
            rawSnippet: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            code: "CS0103",
            normalizedMessage: "The name '{identifier}' does not exist in the current context",
            location: new DiagnosticLocation("Program.cs", 14, 9),
            category: DiagnosticCategory.MissingSymbol,
            isPrimaryCandidate: true,
            fingerprint: fingerprint);

        var group = new DiagnosticGroup(
            fingerprint: fingerprint,
            primaryDiagnosticId: diagnostic.Id,
            relatedDiagnosticIds: new[] { diagnostic.Id },
            groupReason: "single-primary-diagnostic");

        var snapshot = new PersistedAnalysisDto(
            AnalysisId: "anl_001",
            InputType: InputType.BuildLog,
            ToolDetected: ToolKind.DotNet,
            CreatedAtUtc: new DateTimeOffset(2026, 03, 24, 9, 0, 0, TimeSpan.Zero),
            Summary: new AnalysisSummary(1, 1, 1, 1, 0),
            RootCauseCandidates: new[]
            {
                new RootCauseCandidate(
                    Fingerprint: fingerprint,
                    Title: "Unknown symbol in current context",
                    Explanation: "The compiler cannot resolve a referenced name in the current scope.",
                    Confidence: 0.96,
                    Signals: new[] { "diagnostic-code:CS0103" },
                    LikelyCauses: new[] { "Typo in variable or member name" },
                    SuggestedFixes: new[] { "Check the symbol spelling." },
                    DiagnosticId: diagnostic.Id,
                    GroupId: fingerprint.Value)
            },
            Groups: new[] { group },
            Diagnostics: new[] { diagnostic },
            MatchedRules: new[]
            {
                new RuleMatch(
                    RuleId: "rule_001",
                    TargetType: "candidate",
                    TargetId: fingerprint.Value,
                    MatchedConditions: new[] { "tool", "code" },
                    AppliedActions: new[] { "explanation" },
                    AppliedAt: new DateTimeOffset(2026, 03, 24, 9, 0, 0, TimeSpan.Zero))
            },
            Narrative: AnalysisNarrative.Deterministic(
                summary: "The .NET log contains a single missing-symbol issue.",
                groupSummaries: new[] { "Unknown symbol cluster." },
                recommendedNextSteps: new[] { "Check the symbol spelling." }),
            Processing: new ProcessingMetadata(
                UsedAi: false,
                DurationMs: 15,
                Parser: "dotnet-diagnostic-parser-v1",
                CorrelationId: "corr_001",
                ToolDetectionConfidence: 1.0,
                ParseConfidence: 0.98,
                UnparsedSegmentCount: 0,
                Notes: null),
            Warnings: new[] { "sample warning" },
            Context: new Dictionary<string, string>
            {
                ["projectName"] = "InsightLogger.Api",
                ["repository"] = "InsightLogger"
            },
            ProjectName: "InsightLogger.Api",
            Repository: "InsightLogger",
            RawContentHash: "hash_001",
            RawContentRedacted: false,
            RawContent: null);

        _dbContext.Analyses.Add(new AnalysisEntity
        {
            Id = "anl_001",
            InputType = "BuildLog",
            ToolDetected = "DotNet",
            TotalDiagnostics = 1,
            GroupCount = 1,
            PrimaryIssueCount = 1,
            ErrorCount = 1,
            WarningCount = 0,
            UsedAi = false,
            DurationMs = 15,
            Parser = "dotnet-diagnostic-parser-v1",
            CorrelationId = "corr_001",
            ToolDetectionConfidence = 1.0,
            ParseConfidence = 0.98,
            UnparsedSegmentCount = 0,
            NarrativeSummary = snapshot.Narrative!.Summary,
            NarrativeGroupSummariesJson = JsonSerializer.Serialize(snapshot.Narrative.GroupSummaries),
            NarrativeRecommendedNextStepsJson = JsonSerializer.Serialize(snapshot.Narrative.RecommendedNextSteps),
            NarrativeSource = snapshot.Narrative.Source,
            NarrativeStatus = snapshot.Narrative.Status,
            ProjectName = snapshot.ProjectName,
            Repository = snapshot.Repository,
            RawContentHash = snapshot.RawContentHash,
            ContextJson = JsonSerializer.Serialize(snapshot.Context),
            AnalysisSnapshotJson = JsonSerializer.Serialize(snapshot),
            CreatedAtUtc = snapshot.CreatedAtUtc
        });

        _dbContext.Analyses.Add(new AnalysisEntity
        {
            Id = "anl_legacy",
            InputType = "BuildLog",
            ToolDetected = "DotNet",
            TotalDiagnostics = 1,
            GroupCount = 1,
            PrimaryIssueCount = 1,
            ErrorCount = 1,
            WarningCount = 0,
            UsedAi = false,
            DurationMs = 9,
            Parser = "dotnet-diagnostic-parser-v1",
            CorrelationId = "corr_legacy",
            ToolDetectionConfidence = 1.0,
            ParseConfidence = 0.95,
            UnparsedSegmentCount = 0,
            NarrativeSummary = "Legacy narrative",
            NarrativeGroupSummariesJson = "[\"Legacy group\"]",
            NarrativeRecommendedNextStepsJson = "[\"Legacy next step\"]",
            NarrativeSource = "deterministic",
            ProjectName = "Legacy.Project",
            Repository = "LegacyRepo",
            RawContentHash = "hash_legacy",
            CreatedAtUtc = new DateTimeOffset(2026, 03, 24, 8, 0, 0, TimeSpan.Zero),
            Diagnostics =
            {
                new DiagnosticEntity
                {
                    Id = "diag_legacy",
                    AnalysisId = "anl_legacy",
                    ToolKind = "DotNet",
                    Code = "CS0103",
                    Severity = "Error",
                    Message = "The name 'servicez' does not exist in the current context",
                    NormalizedMessage = "The name '{identifier}' does not exist in the current context",
                    RawSnippet = "Legacy snippet",
                    Category = "MissingSymbol",
                    OrderIndex = 0
                }
            },
            Groups =
            {
                new DiagnosticGroupEntity
                {
                    Id = "grp_anl_legacy_1",
                    AnalysisId = "anl_legacy",
                    Fingerprint = "fp_legacy",
                    Count = 1,
                    GroupReason = "single-primary-diagnostic",
                    PrimaryDiagnosticId = "diag_legacy",
                    RelatedDiagnosticIdsJson = "[\"diag_legacy\"]",
                    OrderIndex = 0
                }
            }
        });

        _dbContext.ErrorPatterns.Add(new ErrorPatternEntity
        {
            Fingerprint = "fp_cs0103_name_missing",
            Title = "Unknown symbol in current context",
            CanonicalMessage = "The name '{identifier}' does not exist in the current context",
            ToolKind = "DotNet",
            Category = "MissingSymbol",
            DiagnosticCode = "CS0103",
            FirstSeenAtUtc = new DateTimeOffset(2026, 03, 23, 9, 0, 0, TimeSpan.Zero),
            LastSeenAtUtc = new DateTimeOffset(2026, 03, 24, 9, 0, 0, TimeSpan.Zero),
            OccurrenceCount = 2
        });

        _dbContext.PatternOccurrences.Add(new PatternOccurrenceEntity
        {
            Id = "po_001",
            Fingerprint = "fp_cs0103_name_missing",
            AnalysisId = "anl_001",
            DiagnosticId = "diag_001",
            SeenAtUtc = new DateTimeOffset(2026, 03, 24, 9, 0, 0, TimeSpan.Zero)
        });

        _dbContext.PatternOccurrences.Add(new PatternOccurrenceEntity
        {
            Id = "po_002",
            Fingerprint = "fp_cs0103_name_missing",
            AnalysisId = "anl_legacy",
            DiagnosticId = "diag_legacy",
            SeenAtUtc = new DateTimeOffset(2026, 03, 24, 8, 0, 0, TimeSpan.Zero)
        });

        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetByAnalysisIdAsync_ShouldReturnSnapshotBackedDetail()
    {
        var repository = new EfCoreAnalysisReadRepository(_dbContext);

        var result = await repository.GetByAnalysisIdAsync("anl_001");

        result.Should().NotBeNull();
        result!.Diagnostics.Should().HaveCount(1);
        result.RootCauseCandidates.Should().HaveCount(1);
        result.MatchedRules.Should().HaveCount(1);
        result.Warnings.Should().Contain("sample warning");
        result.Context.Should().ContainKey("projectName");
    }


    [Fact]
    public async Task GetRecentRelatedAnalysesAsync_ShouldReturnPriorAnalysesForMatchingFingerprints()
    {
        var repository = new EfCoreAnalysisReadRepository(_dbContext);

        var result = await repository.GetRecentRelatedAnalysesAsync(
            fingerprints: new[] { "fp_cs0103_name_missing" },
            excludeAnalysisId: "anl_001",
            projectName: null,
            repository: null,
            limit: 10);

        result.Should().ContainSingle();
        result[0].AnalysisId.Should().Be("anl_legacy");
        result[0].MatchingFingerprints.Should().Contain("fp_cs0103_name_missing");
    }

    [Fact]
    public async Task SearchSimilarAnalysesAsync_ShouldReturnRankedSimilarHistory()
    {
        var repository = new EfCoreAnalysisReadRepository(_dbContext);

        var result = await repository.SearchSimilarAnalysesAsync(
            toolKind: ToolKind.DotNet,
            fingerprints: Array.Empty<string>(),
            diagnosticCodes: new[] { "CS0103" },
            categories: new[] { DiagnosticCategory.MissingSymbol },
            normalizedMessages: new[] { "The name '{identifier}' does not exist in the current context" },
            excludeAnalysisId: "anl_001",
            projectName: null,
            repository: null,
            limit: 5);

        result.Should().ContainSingle();
        result[0].AnalysisId.Should().Be("anl_legacy");
        result[0].MatchType.Should().Be("diagnostic-code");
        result[0].MatchScore.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByAnalysisIdAsync_ShouldFallbackToNormalizedRows_WhenSnapshotMissing()
    {
        var repository = new EfCoreAnalysisReadRepository(_dbContext);

        var result = await repository.GetByAnalysisIdAsync("anl_legacy");

        result.Should().NotBeNull();
        result!.Diagnostics.Should().HaveCount(1);
        result.Groups.Should().HaveCount(1);
        result.RootCauseCandidates.Should().BeEmpty();
        result.Narrative.Should().NotBeNull();
        result.ProjectName.Should().Be("Legacy.Project");
    }
}




