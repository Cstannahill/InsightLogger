using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Application.Analyses.Persistence;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InsightLogger.InfrastructureTests.Persistence;

public sealed class EfCoreErrorPatternRepositoryTests
{
    [Fact]
    public async Task UpsertFromAnalysis_repeated_fingerprint_increments_occurrence_count()
    {
        var options = new DbContextOptionsBuilder<InsightLoggerDbContext>()
            .UseInMemoryDatabase($"patterns-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new InsightLoggerDbContext(options);
        var repository = new EfCoreErrorPatternRepository(dbContext);

        var requestA = BuildRequest("anl_one", "diag_one");
        var requestB = BuildRequest("anl_two", "diag_two");

        await repository.UpsertFromAnalysisAsync(requestA);
        await repository.UpsertFromAnalysisAsync(requestB);
        await dbContext.SaveChangesAsync();

        var pattern = await dbContext.ErrorPatterns.SingleAsync();
        pattern.OccurrenceCount.Should().Be(2);
        pattern.Fingerprint.Should().Be("fp_dotnet_cs0103_missing-symbol");
        (await dbContext.PatternOccurrences.CountAsync()).Should().Be(2);
    }

    private static AnalysisPersistenceRequest BuildRequest(string analysisId, string diagnosticId)
    {
        var fingerprint = new DiagnosticFingerprint("fp_dotnet_cs0103_missing-symbol");
        var location = new DiagnosticLocation("Program.cs", 14, 9, null, null);
        var diagnostic = new DiagnosticRecord(
            toolKind: ToolKind.DotNet,
            severity: Severity.Error,
            message: "The name 'builderz' does not exist in the current context",
            rawSnippet: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            id: diagnosticId,
            code: "CS0103",
            normalizedMessage: "The name '{identifier}' does not exist in the current context",
            location: location,
            category: DiagnosticCategory.MissingSymbol,
            fingerprint: fingerprint);

        var summary = new AnalysisSummary(
            TotalDiagnostics: 1,
            GroupCount: 1,
            PrimaryIssueCount: 1,
            ErrorCount: 1,
            WarningCount: 0);

        var processing = new ProcessingMetadata(
            UsedAi: false,
            DurationMs: 7,
            Parser: "dotnet-diagnostic-parser-v1",
            CorrelationId: null,
            ToolDetectionConfidence: 0.98,
            ParseConfidence: 0.99,
            UnparsedSegmentCount: 0,
            Notes: null);

        return new AnalysisPersistenceRequest(
            AnalysisId: analysisId,
            InputType: InputType.SingleDiagnostic,
            ToolDetected: ToolKind.DotNet,
            Summary: summary,
            Diagnostics: new[] { diagnostic },
            Groups: Array.Empty<DiagnosticGroup>(),
            RootCauseCandidates: Array.Empty<RootCauseCandidate>(),
            MatchedRules: Array.Empty<InsightLogger.Domain.Rules.RuleMatch>(),
            Narrative: null,
            Processing: processing,
            Warnings: Array.Empty<string>(),
            Context: null,
            ProjectName: null,
            Repository: null,
            RawContentHash: $"hash_{analysisId}",
            RawContent: null,
            RawContentRedacted: false,
            CreatedAtUtc: DateTimeOffset.UtcNow);
    }
}


