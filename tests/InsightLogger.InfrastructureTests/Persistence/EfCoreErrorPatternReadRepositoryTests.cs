using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.Infrastructure.Persistence.Entities;
using InsightLogger.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InsightLogger.InfrastructureTests.Persistence;

public sealed class EfCoreErrorPatternReadRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly InsightLoggerDbContext _dbContext;

    public EfCoreErrorPatternReadRepositoryTests()
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

        _dbContext.ErrorPatterns.AddRange(
            new ErrorPatternEntity
            {
                Fingerprint = "fp_cs0103_name_missing",
                Title = "Unknown symbol in current context",
                CanonicalMessage = "The name '{identifier}' does not exist in the current context",
                ToolKind = ToolKind.DotNet.ToString(),
                Category = DiagnosticCategory.MissingSymbol.ToString(),
                FirstSeenAtUtc = new DateTimeOffset(2026, 03, 23, 10, 00, 00, TimeSpan.Zero),
                LastSeenAtUtc = new DateTimeOffset(2026, 03, 24, 15, 21, 00, TimeSpan.Zero),
                OccurrenceCount = 38,
                LastSuggestedFix = "Check spelling of the symbol."
            },
            new ErrorPatternEntity
            {
                Fingerprint = "fp_cs8618_non_nullable_uninitialized",
                Title = "Non-nullable member not initialized",
                CanonicalMessage = "Non-nullable property must contain a non-null value when exiting constructor",
                ToolKind = ToolKind.DotNet.ToString(),
                Category = DiagnosticCategory.NullableSafety.ToString(),
                FirstSeenAtUtc = new DateTimeOffset(2026, 03, 23, 09, 30, 00, TimeSpan.Zero),
                LastSeenAtUtc = new DateTimeOffset(2026, 03, 24, 12, 01, 00, TimeSpan.Zero),
                OccurrenceCount = 21
            },
            new ErrorPatternEntity
            {
                Fingerprint = "fp_python_nameerror_not_defined",
                Title = "Name not defined",
                CanonicalMessage = "name '{identifier}' is not defined",
                ToolKind = ToolKind.Python.ToString(),
                Category = DiagnosticCategory.MissingSymbol.ToString(),
                FirstSeenAtUtc = new DateTimeOffset(2026, 03, 22, 08, 00, 00, TimeSpan.Zero),
                LastSeenAtUtc = new DateTimeOffset(2026, 03, 24, 11, 45, 00, TimeSpan.Zero),
                OccurrenceCount = 8
            });

        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task GetByFingerprintAsync_ShouldReturnExpectedDetailShape()
    {
        var repository = new EfCoreErrorPatternReadRepository(_dbContext);

        var result = await repository.GetByFingerprintAsync("fp_cs0103_name_missing");

        Assert.NotNull(result);
        Assert.Equal("fp_cs0103_name_missing", result!.Fingerprint);
        Assert.Equal(ToolKind.DotNet, result.ToolKind);
        Assert.Equal(DiagnosticCategory.MissingSymbol, result.Category);
        Assert.Equal(38, result.OccurrenceCount);
        Assert.Contains("Check spelling of the symbol.", result.KnownFixes);
    }

    [Fact]
    public async Task GetTopPatternsAsync_ShouldFilterByTool_AndOrderByOccurrenceCount()
    {
        var repository = new EfCoreErrorPatternReadRepository(_dbContext);

        var result = await repository.GetTopPatternsAsync(ToolKind.DotNet, limit: 10);

        Assert.Equal(2, result.Count);
        Assert.Equal("fp_cs0103_name_missing", result[0].Fingerprint);
        Assert.Equal("fp_cs8618_non_nullable_uninitialized", result[1].Fingerprint);
    }

    [Fact]
    public async Task GetTopPatternsAsync_ShouldRespectLimit()
    {
        var repository = new EfCoreErrorPatternReadRepository(_dbContext);

        var result = await repository.GetTopPatternsAsync(null, limit: 2);

        Assert.Equal(2, result.Count);
    }
}
