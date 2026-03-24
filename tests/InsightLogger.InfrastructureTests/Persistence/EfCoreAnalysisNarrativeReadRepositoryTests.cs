using System;
using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.Infrastructure.Persistence.Entities;
using InsightLogger.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InsightLogger.InfrastructureTests.Persistence;

public sealed class EfCoreAnalysisNarrativeReadRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly InsightLoggerDbContext _dbContext;

    public EfCoreAnalysisNarrativeReadRepositoryTests()
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

        _dbContext.Analyses.AddRange(
            new AnalysisEntity
            {
                Id = "anl_001",
                InputType = "BuildLog",
                ToolDetected = "DotNet",
                TotalDiagnostics = 3,
                GroupCount = 2,
                PrimaryIssueCount = 2,
                ErrorCount = 2,
                WarningCount = 1,
                NarrativeSummary = "The .NET log contains two likely issue clusters.",
                NarrativeGroupSummariesJson = "[\"Unknown symbol cluster\",\"Nullable warning cluster\"]",
                NarrativeRecommendedNextStepsJson = "[\"Fix the first symbol error.\"]",
                NarrativeSource = "deterministic",
                NarrativeStatus = "completed",
                ProjectName = "InsightLogger.Api",
                Repository = "InsightLogger",
                RawContentHash = "hash_001",
                CreatedAtUtc = new DateTimeOffset(2026, 03, 24, 6, 0, 0, TimeSpan.Zero)
            },
            new AnalysisEntity
            {
                Id = "anl_002",
                InputType = "BuildLog",
                ToolDetected = "DotNet",
                TotalDiagnostics = 5,
                GroupCount = 3,
                PrimaryIssueCount = 2,
                ErrorCount = 4,
                WarningCount = 1,
                NarrativeSummary = "AI narrative summary for nullable cluster.",
                NarrativeGroupSummariesJson = "[\"Primary nullable cluster\"]",
                NarrativeRecommendedNextStepsJson = "[\"Start with the nullable cluster.\"]",
                NarrativeSource = "ai",
                NarrativeProvider = "ollama",
                NarrativeModel = "qwen3:8b",
                NarrativeStatus = "completed",
                NarrativeFallbackUsed = false,
                ProjectName = "InsightLogger.Api",
                Repository = "InsightLogger",
                RawContentHash = "hash_002",
                CreatedAtUtc = new DateTimeOffset(2026, 03, 24, 7, 0, 0, TimeSpan.Zero)
            },
            new AnalysisEntity
            {
                Id = "anl_003",
                InputType = "SingleDiagnostic",
                ToolDetected = "DotNet",
                TotalDiagnostics = 1,
                GroupCount = 1,
                PrimaryIssueCount = 1,
                ErrorCount = 1,
                WarningCount = 0,
                RawContentHash = "hash_003",
                CreatedAtUtc = new DateTimeOffset(2026, 03, 24, 8, 0, 0, TimeSpan.Zero)
            });

        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetByAnalysisIdAsync_ShouldReturnNarrativeDetail()
    {
        var repository = new EfCoreAnalysisNarrativeReadRepository(_dbContext);

        var result = await repository.GetByAnalysisIdAsync("anl_001");

        result.Should().NotBeNull();
        result!.AnalysisId.Should().Be("anl_001");
        result.ProjectName.Should().Be("InsightLogger.Api");
        result.Repository.Should().Be("InsightLogger");
        result.Narrative.Source.Should().Be("deterministic");
        result.Narrative.GroupSummaries.Should().Contain("Unknown symbol cluster");
    }

    [Fact]
    public async Task GetRecentAsync_ShouldFilterAndOrderNarrativeHistory()
    {
        var repository = new EfCoreAnalysisNarrativeReadRepository(_dbContext);

        var result = await repository.GetRecentAsync(
            toolKind: null,
            source: "ai",
            projectName: "InsightLogger.Api",
            repository: "InsightLogger",
            text: null,
            limit: 10);

        result.Should().HaveCount(1);
        result[0].AnalysisId.Should().Be("anl_002");
        result[0].Source.Should().Be("ai");
        result[0].Provider.Should().Be("ollama");
        result[0].MatchedFields.Should().BeEmpty();
        result[0].MatchSnippet.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentAsync_ShouldSearchNarrativeText_AndReturnMatchMetadata()
    {
        var repository = new EfCoreAnalysisNarrativeReadRepository(_dbContext);

        var result = await repository.GetRecentAsync(
            toolKind: null,
            source: null,
            projectName: null,
            repository: null,
            text: "nullable",
            limit: 10);

        result.Should().ContainSingle(item => item.AnalysisId == "anl_002");
        var match = result[0];
        match.MatchedFields.Should().Contain(field => field == "summary" || field == "groupSummaries" || field == "recommendedNextSteps");
        match.MatchSnippet.Should().NotBeNullOrWhiteSpace();
        match.MatchSnippet.Should().ContainEquivalentOf("nullable");
    }
}



