using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Contracts.Common;
using InsightLogger.ApiTests.Infrastructure;
using Xunit;

namespace InsightLogger.ApiTests.Endpoints;

public sealed class AnalyzeBuildLogEndpointTests : IClassFixture<ApiTestWebApplicationFactory>
{
    private readonly ApiTestWebApplicationFactory _factory;

    public AnalyzeBuildLogEndpointTests(ApiTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostBuildLog_ShouldReturnStructuredAnalysisResponse()
    {
        using var client = _factory.CreateClient();

        var request = new AnalyzeBuildLogRequest(
            Tool: "dotnet",
            Content: """
Build started...
Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context
Build FAILED.
""",
            ProjectName: "InsightLogger.Api",
            Repository: "InsightLogger",
            Branch: "main",
            CommitSha: "abc123",
            Environment: new AnalyzeEnvironmentContract(
                Os: "windows",
                Ci: false,
                MachineName: "DEVBOX"),
            Options: new AnalyzeRequestOptionsContract(
                Persist: false,
                UseAiEnrichment: false,
                IncludeRawDiagnostics: true,
                IncludeGroups: true,
                IncludeProcessingMetadata: true));

        using var response = await client.PostAsJsonAsync("/analyze/build-log", request);
        var payload = await response.Content.ReadFromJsonAsync<AnalyzeBuildLogResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Correlation-Id");

        payload.Should().NotBeNull();
        payload!.ToolDetected.Should().Be("dotnet");
        payload.Summary.TotalDiagnostics.Should().BeGreaterThanOrEqualTo(1);
        payload.Summary.ErrorCount.Should().BeGreaterThanOrEqualTo(1);
        payload.RootCauseCandidates.Should().NotBeEmpty();
        payload.RootCauseCandidates[0].Fingerprint.Should().NotBeNullOrWhiteSpace();
        payload.Diagnostics.Should().Contain(d => d.Code == "CS0103");
        payload.Diagnostics.Should().Contain(d => d.Category == "missing-symbol");
        payload.Diagnostics.Should().Contain(d => d.NormalizedMessage == "The name '{identifier}' does not exist in the current context");
        payload.Processing.Should().NotBeNull();
        payload.Processing!.UsedAi.Should().BeFalse();
        payload.Processing.Parser.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PostBuildLog_ShouldReturnValidationError_ForEmptyContent()
    {
        using var client = _factory.CreateClient();

        var request = new AnalyzeBuildLogRequest(
            Tool: "dotnet",
            Content: "   ");

        using var response = await client.PostAsJsonAsync("/analyze/build-log", request);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        payload.Should().NotBeNull();
        payload!.Error.Code.Should().Be("validation_failed");
        payload.Error.Details.Should().Contain(d => d.Field == "content");
    }

    [Fact]
    public async Task PostBuildLog_ShouldReturnUnsupportedMediaType_ForPlainText()
    {
        using var client = _factory.CreateClient();
        using var content = JsonContent.Create(new { hello = "world" });

        content.Headers.ContentType!.MediaType = "text/plain";

        using var response = await client.PostAsync("/analyze/build-log", content);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        payload.Should().NotBeNull();
        payload!.Error.Code.Should().Be("unsupported_media_type");
    }
}
