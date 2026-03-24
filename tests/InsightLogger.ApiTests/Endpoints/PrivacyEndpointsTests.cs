using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Contracts.Privacy;
using InsightLogger.ApiTests.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace InsightLogger.ApiTests.Endpoints;

public sealed class PrivacyEndpointsTests : IClassFixture<ApiTestWebApplicationFactory>
{
    private readonly ApiTestWebApplicationFactory _factory;

    public PrivacyEndpointsTests(ApiTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPrivacySettings_ShouldReturnConfiguredValues()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Privacy:RawContentStorageEnabled"] = "true",
                    ["Privacy:RedactRawContentOnWrite"] = "true",
                    ["Privacy:RawContentRetentionDays"] = "7",
                    ["Privacy:AnalysisRetentionDays"] = "90"
                });
            });
        });

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/privacy/settings");
        var payload = await response.Content.ReadFromJsonAsync<GetPrivacySettingsResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.RawContentStorageEnabled.Should().BeTrue();
        payload.RedactRawContentOnWrite.Should().BeTrue();
        payload.RawContentRetentionDays.Should().Be(7);
        payload.AnalysisRetentionDays.Should().Be(90);
    }

    [Fact]
    public async Task ApplyRetention_ShouldReturnConfiguredCutoffsAndCounts()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Privacy:RawContentRetentionDays"] = "7",
                    ["Privacy:AnalysisRetentionDays"] = "90"
                });
            });
        });

        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/privacy/retention/apply", content: null);
        var payload = await response.Content.ReadFromJsonAsync<ApplyRetentionPoliciesResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.RawContentRetentionDays.Should().Be(7);
        payload.AnalysisRetentionDays.Should().Be(90);
        payload.RawContentCutoffUtc.Should().NotBeNull();
        payload.AnalysisCutoffUtc.Should().NotBeNull();
        payload.RawContentPurgedCount.Should().BeGreaterThanOrEqualTo(0);
        payload.AnalysesDeletedCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task DeleteRawContent_ThenDeleteAnalysis_ShouldUpdatePersistedHistory()
    {
        using var client = _factory.CreateClient();

        var analyzeResponse = await client.PostAsJsonAsync("/analyze/build-log", new AnalyzeBuildLogRequest(
            Tool: "dotnet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context. token=abc123 contact dev@example.com see https://example.com and C:\\src\\Program.cs",
            Options: new AnalyzeRequestOptionsContract(
                Persist: true,
                PersistRawContent: true)));

        analyzeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var analyzePayload = await analyzeResponse.Content.ReadFromJsonAsync<AnalyzeBuildLogResponse>();
        analyzePayload.Should().NotBeNull();

        var analysisId = analyzePayload!.AnalysisId;

        var detailBefore = await client.GetFromJsonAsync<GetAnalysisResponse>($"/analyses/{analysisId}");
        detailBefore.Should().NotBeNull();
        detailBefore!.RawContentStored.Should().BeTrue();
        detailBefore.RawContentRedacted.Should().BeTrue();
        detailBefore.RawContent.Should().Contain("[redacted-url]");
        detailBefore.RawContent.Should().Contain("[redacted-email]");
        detailBefore.RawContent.Should().Contain("[redacted-token]");
        detailBefore.RawContent.Should().Contain("[redacted-path]");

        using var purgeResponse = await client.DeleteAsync($"/analyses/{analysisId}/raw-content");
        purgeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailAfterPurge = await client.GetFromJsonAsync<GetAnalysisResponse>($"/analyses/{analysisId}");
        detailAfterPurge.Should().NotBeNull();
        detailAfterPurge!.RawContentStored.Should().BeFalse();
        detailAfterPurge.RawContent.Should().BeNull();
        detailAfterPurge.RawContentRedacted.Should().BeFalse();

        using var deleteResponse = await client.DeleteAsync($"/analyses/{analysisId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var notFoundResponse = await client.GetAsync($"/analyses/{analysisId}");
        notFoundResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
