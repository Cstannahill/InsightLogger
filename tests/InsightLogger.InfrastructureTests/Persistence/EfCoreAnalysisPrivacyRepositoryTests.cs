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

public sealed class EfCoreAnalysisPrivacyRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly InsightLoggerDbContext _dbContext;

    public EfCoreAnalysisPrivacyRepositoryTests()
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
                Id = "anl_old",
                InputType = "BuildLog",
                ToolDetected = "DotNet",
                TotalDiagnostics = 1,
                GroupCount = 1,
                PrimaryIssueCount = 1,
                ErrorCount = 1,
                WarningCount = 0,
                RawContentHash = "hash_old",
                RawContent = "old raw",
                RawContentRedacted = true,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-45)
            },
            new AnalysisEntity
            {
                Id = "anl_recent",
                InputType = "BuildLog",
                ToolDetected = "DotNet",
                TotalDiagnostics = 1,
                GroupCount = 1,
                PrimaryIssueCount = 1,
                ErrorCount = 1,
                WarningCount = 0,
                RawContentHash = "hash_recent",
                RawContent = "recent raw",
                RawContentRedacted = true,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-2)
            });

        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task ApplyRetentionAsync_ShouldPurgeOldRawContent_And_DeleteExpiredAnalyses()
    {
        var repository = new EfCoreAnalysisPrivacyRepository(_dbContext);

        var result = await repository.ApplyRetentionAsync(rawContentRetentionDays: 1, analysisRetentionDays: 30);

        result.RawContentPurgedCount.Should().Be(2);
        result.AnalysesDeletedCount.Should().Be(1);

        var oldAnalysis = await _dbContext.Analyses.SingleOrDefaultAsync(x => x.Id == "anl_old");
        oldAnalysis.Should().BeNull();

        var recentAnalysis = await _dbContext.Analyses.SingleAsync(x => x.Id == "anl_recent");
        recentAnalysis.RawContent.Should().BeNull();
        recentAnalysis.RawContentRedacted.Should().BeFalse();
    }

    [Fact]
    public async Task PurgeRawContentAsync_ShouldClearOnlyRawContentForOneAnalysis()
    {
        var repository = new EfCoreAnalysisPrivacyRepository(_dbContext);

        var purged = await repository.PurgeRawContentAsync("anl_recent");

        purged.Should().BeTrue();
        var recentAnalysis = await _dbContext.Analyses.SingleAsync(x => x.Id == "anl_recent");
        recentAnalysis.RawContent.Should().BeNull();
        recentAnalysis.RawContentRedacted.Should().BeFalse();
    }
}
