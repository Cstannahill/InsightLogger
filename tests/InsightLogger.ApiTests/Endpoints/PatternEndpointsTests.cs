using System.Net;
using System.Net.Http.Json;
using System.IO;
using FluentAssertions;
using InsightLogger.Contracts.Common;
using InsightLogger.Contracts.Diagnostics;
using InsightLogger.Contracts.Patterns;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace InsightLogger.ApiTests.Endpoints;

public sealed class PatternEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _connectionString;
    private readonly string _databasePath;

    public PatternEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"insightlogger-api-pattern-tests-{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_databasePath};Pooling=False";

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Persistence:Enabled"] = "true",
                    ["Persistence:AutoMigrate"] = "false",
                    ["Persistence:ConnectionString"] = _connectionString
                });
            });
        });
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<InsightLoggerDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        await using var dbContext = new InsightLoggerDbContext(options);

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.ErrorPatterns.AddRange(
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

        dbContext.Analyses.Add(new AnalysisEntity
        {
            Id = "anl_1",
            InputType = "BuildLog",
            ToolDetected = ToolKind.DotNet.ToString(),
            TotalDiagnostics = 1,
            GroupCount = 1,
            PrimaryIssueCount = 1,
            ErrorCount = 1,
            WarningCount = 0,
            UsedAi = false,
            DurationMs = 12,
            RawContentHash = "hash_1",
            CreatedAtUtc = new DateTimeOffset(2026, 03, 24, 15, 21, 00, TimeSpan.Zero)
        });

        dbContext.Diagnostics.Add(new DiagnosticEntity
        {
            Id = "diag_1",
            AnalysisId = "anl_1",
            ToolKind = ToolKind.DotNet.ToString(),
            Code = "CS0103",
            Severity = Severity.Error.ToString(),
            Message = "The name 'builderz' does not exist in the current context",
            NormalizedMessage = "The name '{identifier}' does not exist in the current context",
            FilePath = "Program.cs",
            Line = 14,
            Column = 9,
            RawSnippet = "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            Category = DiagnosticCategory.MissingSymbol.ToString(),
            IsPrimaryCandidate = true,
            Fingerprint = "fp_cs0103_name_missing",
            OrderIndex = 0
        });

        dbContext.PatternOccurrences.Add(new PatternOccurrenceEntity
        {
            Id = "occ_1",
            Fingerprint = "fp_cs0103_name_missing",
            AnalysisId = "anl_1",
            DiagnosticId = "diag_1",
            SeenAtUtc = new DateTimeOffset(2026, 03, 24, 15, 21, 00, TimeSpan.Zero)
        });

        dbContext.Rules.AddRange(
            new RuleEntity
            {
                Id = "rule_1",
                Name = "Exact missing symbol guidance",
                IsEnabled = true,
                Priority = 100,
                ToolKindCondition = ToolKind.DotNet.ToString(),
                CodeCondition = "CS0103",
                CategoryCondition = DiagnosticCategory.MissingSymbol.ToString(),
                FingerprintCondition = "fp_cs0103_name_missing",
                ExplanationAction = "Check symbol scope.",
                MatchCount = 12,
                LastMatchedAtUtc = new DateTimeOffset(2026, 03, 24, 15, 30, 00, TimeSpan.Zero),
                CreatedAtUtc = new DateTimeOffset(2026, 03, 23, 10, 00, 00, TimeSpan.Zero),
                UpdatedAtUtc = new DateTimeOffset(2026, 03, 24, 15, 30, 00, TimeSpan.Zero)
            },
            new RuleEntity
            {
                Id = "rule_2",
                Name = "Generic dotnet missing symbol guidance",
                IsEnabled = true,
                Priority = 60,
                ToolKindCondition = ToolKind.DotNet.ToString(),
                CategoryCondition = DiagnosticCategory.MissingSymbol.ToString(),
                ExplanationAction = "Check declarations.",
                MatchCount = 3,
                LastMatchedAtUtc = new DateTimeOffset(2026, 03, 24, 12, 00, 00, TimeSpan.Zero),
                CreatedAtUtc = new DateTimeOffset(2026, 03, 23, 11, 00, 00, TimeSpan.Zero),
                UpdatedAtUtc = new DateTimeOffset(2026, 03, 24, 12, 00, 00, TimeSpan.Zero)
            });

        await dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetErrorByFingerprint_ShouldReturnStructuredResult_WhenFingerprintExists()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/errors/fp_cs0103_name_missing");
        var payload = await response.Content.ReadFromJsonAsync<GetErrorByFingerprintResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Fingerprint.Should().Be("fp_cs0103_name_missing");
        payload.Tool.Should().Be("dotnet");
        payload.Category.Should().Be("missing-symbol");
        payload.RelatedRules.Should().HaveCount(2);
        payload.RelatedRules[0].MatchedBy.Should().Contain(new[] { "fingerprint", "code", "category" });
        payload.RelatedRules[0].MatchCount.Should().Be(12);
        payload.RelatedRules[0].LastMatchedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetErrorByFingerprint_ShouldReturnNotFoundEnvelope_WhenFingerprintDoesNotExist()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/errors/fp_missing");
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        payload.Should().NotBeNull();
        payload!.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetTopPatterns_ShouldReturnOrderedItems()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/patterns/top?tool=dotnet&limit=2");
        var payload = await response.Content.ReadFromJsonAsync<GetTopPatternsResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Items.Should().HaveCount(2);
        payload.Items[0].Fingerprint.Should().Be("fp_cs0103_name_missing");
    }

    [Fact]
    public async Task GetTopPatterns_ShouldReturnValidationError_ForInvalidLimit()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/patterns/top?limit=0");
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        payload.Should().NotBeNull();
        payload!.Error.Code.Should().Be("validation_failed");
        payload.Error.Details.Should().Contain(detail => detail.Field == "limit");
    }

    [Fact]
    public async Task GetTopPatterns_ShouldReturnValidationError_ForInvalidTool()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/patterns/top?tool=banana");
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        payload.Should().NotBeNull();
        payload!.Error.Code.Should().Be("validation_failed");
        payload.Error.Details.Should().Contain(detail => detail.Field == "tool");
    }
}
