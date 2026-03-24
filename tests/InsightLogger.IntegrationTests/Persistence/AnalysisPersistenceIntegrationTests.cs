using System;
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
            Persist: true));

        result.Diagnostics.Should().NotBeEmpty();

        (await dbContext.Analyses.CountAsync()).Should().Be(1);
        (await dbContext.Diagnostics.CountAsync()).Should().Be(result.Diagnostics.Count);
        (await dbContext.DiagnosticGroups.CountAsync()).Should().Be(result.Groups.Count);
        (await dbContext.PatternOccurrences.CountAsync()).Should().BeGreaterThan(0);
        (await dbContext.ErrorPatterns.CountAsync()).Should().BeGreaterThan(0);
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
