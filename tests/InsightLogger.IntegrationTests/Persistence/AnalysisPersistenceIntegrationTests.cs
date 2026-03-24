using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Application.Analyses.Commands;
using InsightLogger.Application.Analyses.Services;
using InsightLogger.Application.DependencyInjection;
using InsightLogger.Domain.Analyses;
using InsightLogger.Infrastructure.DependencyInjection;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InsightLogger.IntegrationTests.Persistence;

public sealed class AnalysisPersistenceIntegrationTests : IDisposable
{
    private readonly string _dbPath;

    public AnalysisPersistenceIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"insightlogger-tests-{Guid.NewGuid():N}.db");
    }

    [Fact]
    public async Task Analyze_single_diagnostic_persist_true_writes_analysis_diagnostic_group_and_pattern_rows()
    {
        await using var serviceProvider = BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<InsightLoggerDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var analysisService = scope.ServiceProvider.GetRequiredService<IAnalysisService>();
        var content = ReadSampleLog("samples/logs/dotnet/cs0103-single-diagnostic.log");

        var result = await analysisService.AnalyzeAsync(new AnalyzeInputCommand(
            Content: content,
            InputType: InputType.SingleDiagnostic,
            Persist: true));

        result.Diagnostics.Should().ContainSingle();

        (await dbContext.Analyses.CountAsync()).Should().Be(1);
        (await dbContext.Diagnostics.CountAsync()).Should().Be(1);
        (await dbContext.DiagnosticGroups.CountAsync()).Should().Be(1);
        (await dbContext.PatternOccurrences.CountAsync()).Should().Be(1);
        (await dbContext.ErrorPatterns.CountAsync()).Should().Be(1);

        var analysis = await dbContext.Analyses.SingleAsync();
        analysis.RawContentHash.Should().NotBeNullOrWhiteSpace();
        analysis.RawContent.Should().BeNull();
        analysis.AnalysisSnapshotJson.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Analyze_build_log_persist_true_writes_multiple_diagnostics_groups_and_occurrences()
    {
        await using var serviceProvider = BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<InsightLoggerDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var analysisService = scope.ServiceProvider.GetRequiredService<IAnalysisService>();
        var content = ReadSampleLog("samples/logs/dotnet/build-failed-mixed.log");

        var result = await analysisService.AnalyzeAsync(new AnalyzeInputCommand(
            Content: content,
            InputType: InputType.BuildLog,
            Context: new Dictionary<string, string>
            {
                ["projectName"] = "InsightLogger.Api",
                ["repository"] = "InsightLogger"
            },
            Persist: true));

        result.Diagnostics.Should().NotBeEmpty();

        (await dbContext.Analyses.CountAsync()).Should().Be(1);
        (await dbContext.Diagnostics.CountAsync()).Should().Be(result.Diagnostics.Count);
        (await dbContext.DiagnosticGroups.CountAsync()).Should().Be(result.Groups.Count);
        (await dbContext.PatternOccurrences.CountAsync()).Should().BeGreaterThan(0);
        (await dbContext.ErrorPatterns.CountAsync()).Should().BeGreaterThan(0);

        var analysis = await dbContext.Analyses.SingleAsync();
        analysis.NarrativeSummary.Should().NotBeNullOrWhiteSpace();
        analysis.ProjectName.Should().Be("InsightLogger.Api");
        analysis.Repository.Should().Be("InsightLogger");
        analysis.AnalysisSnapshotJson.Should().NotBeNullOrWhiteSpace();
    }


    [Fact]
    public async Task Analyze_build_log_persist_raw_content_true_should_store_redacted_raw_content()
    {
        await using var serviceProvider = BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<InsightLoggerDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        var analysisService = scope.ServiceProvider.GetRequiredService<IAnalysisService>();
        var content = "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context. token=abc123 contact dev@example.com see https://example.com and C:\\src\\Program.cs";

        await analysisService.AnalyzeAsync(new AnalyzeInputCommand(
            Content: content,
            InputType: InputType.BuildLog,
            Persist: true,
            StoreRawContentWhenPersisting: true));

        var analysis = await dbContext.Analyses.SingleAsync();
        analysis.RawContent.Should().NotBeNull();
        analysis.RawContentRedacted.Should().BeTrue();
        analysis.RawContent.Should().Contain("[redacted-token]");
        analysis.RawContent.Should().Contain("[redacted-email]");
        analysis.RawContent.Should().Contain("[redacted-url]");
        analysis.RawContent.Should().Contain("[redacted-path]");
    }
    private ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddInsightLoggerApplication();
        services.AddInsightLoggerInfrastructureParsing();
        services.AddInsightLoggerInfrastructurePersistence($"Data Source={_dbPath};Pooling=False");

        return services.BuildServiceProvider();
    }

    private static string ReadSampleLog(string relativePath)
    {
        var repoRoot = RepositoryPathResolver.FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(repoRoot, relativePath));
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}



