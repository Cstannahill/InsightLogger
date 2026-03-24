using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Contracts.Common;
using InsightLogger.ApiTests.Infrastructure;
using Xunit;

namespace InsightLogger.ApiTests.Endpoints;

public sealed class AnalyzeCompilerErrorEndpointTests : IClassFixture<ApiTestWebApplicationFactory>
{
    private readonly ApiTestWebApplicationFactory _factory;

    public AnalyzeCompilerErrorEndpointTests(ApiTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostCompilerError_ShouldReturnCompactAnalysisResponse()
    {
        using var client = _factory.CreateClient();

        var request = new AnalyzeCompilerErrorRequest(
            Tool: "dotnet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            Context: new AnalyzeRequestContextContract(
                ProjectName: "InsightLogger.Api",
                Repository: "InsightLogger",
                Branch: "main",
                CommitSha: "abc123"),
            Options: new AnalyzeRequestOptionsContract(
                Persist: false,
                UseAiEnrichment: false,
                IncludeRawDiagnostics: false,
                IncludeGroups: false,
                IncludeProcessingMetadata: true));

        using var response = await client.PostAsJsonAsync("/analyze/compiler-error", request);
        var payload = await response.Content.ReadFromJsonAsync<AnalyzeCompilerErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Correlation-Id");
        response.Headers.Contains("X-Request-Id").Should().BeTrue();

        payload.Should().NotBeNull();
        payload!.ToolDetected.Should().Be("dotnet");
        payload.Fingerprint.Should().Be("fp_cs0103_name_missing");
        payload.Diagnostic.Should().NotBeNull();
        payload.Diagnostic!.Code.Should().Be("CS0103");
        payload.Diagnostic.Category.Should().Be("missing-symbol");
        payload.Diagnostic.FilePath.Should().Be("Program.cs");
        payload.Diagnostic.Line.Should().Be(14);
        payload.Diagnostic.Column.Should().Be(9);
        payload.Explanation.Should().NotBeNullOrWhiteSpace();
        payload.LikelyCauses.Should().NotBeEmpty();
        payload.SuggestedFixes.Should().NotBeEmpty();
        payload.Confidence.Should().BeGreaterThan(0);
        payload.Processing.Should().NotBeNull();
    }


    [Fact]
    public async Task PostCompilerError_ShouldReturnDeterministicResultWithWarning_WhenAiIsRequestedButDisabled()
    {
        using var client = _factory.CreateClient();

        var request = new AnalyzeCompilerErrorRequest(
            Tool: "dotnet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            Options: new AnalyzeRequestOptionsContract(
                Persist: false,
                UseAiEnrichment: true,
                IncludeRawDiagnostics: false,
                IncludeGroups: false,
                IncludeProcessingMetadata: true));

        using var response = await client.PostAsJsonAsync("/analyze/compiler-error", request);
        var payload = await response.Content.ReadFromJsonAsync<AnalyzeCompilerErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        payload.Should().NotBeNull();
        payload!.Processing.Should().NotBeNull();
        payload.Processing!.UsedAi.Should().BeFalse();
        payload.Processing.Ai.Should().NotBeNull();
        payload.Processing.Ai!.Requested.Should().BeTrue();
        payload.Processing.Ai.Status.Should().Be("disabled");
        payload.Warnings.Should().ContainSingle();
    }

    [Fact]
    public async Task PostCompilerError_ShouldReturnValidationError_ForUnsupportedTool()
    {
        using var client = _factory.CreateClient();

        var request = new AnalyzeCompilerErrorRequest(
            Tool: "java-but-not-yet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context");

        using var response = await client.PostAsJsonAsync("/analyze/compiler-error", request);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        payload.Should().NotBeNull();
        payload!.Error.Code.Should().Be("validation_failed");
        payload.Error.Details.Should().Contain(d => d.Field == "tool");
    }
}


