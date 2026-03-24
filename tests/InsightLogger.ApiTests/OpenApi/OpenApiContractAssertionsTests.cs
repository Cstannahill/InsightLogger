using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace InsightLogger.ApiTests.OpenApi;

public sealed class OpenApiContractAssertionsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OpenApiContractAssertionsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpenApiDocument_ShouldExposeAnalysisContracts()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        paths.TryGetProperty("/analyze/build-log", out var buildLogPath).Should().BeTrue();
        paths.TryGetProperty("/analyze/compiler-error", out var compilerErrorPath).Should().BeTrue();

        schemas.TryGetProperty("AnalyzeBuildLogRequest", out _).Should().BeTrue();
        schemas.TryGetProperty("AnalyzeBuildLogResponse", out _).Should().BeTrue();
        schemas.TryGetProperty("AnalyzeCompilerErrorRequest", out _).Should().BeTrue();
        schemas.TryGetProperty("AnalyzeCompilerErrorResponse", out _).Should().BeTrue();
        schemas.TryGetProperty("ApiErrorResponse", out _).Should().BeTrue();

        AssertOperation(buildLogPath.GetProperty("post"), "AnalyzeBuildLogRequest", "AnalyzeBuildLogResponse");
        AssertOperation(compilerErrorPath.GetProperty("post"), "AnalyzeCompilerErrorRequest", "AnalyzeCompilerErrorResponse");
    }

    [Fact]
    public async Task OpenApiDocument_ShouldExpose_AiProcessing_And_Warnings_On_Analysis_Responses()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");
        var json = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(json);
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        var buildLogSchema = schemas.GetProperty("AnalyzeBuildLogResponse").GetProperty("properties");
        buildLogSchema.TryGetProperty("warnings", out _).Should().BeTrue();
        buildLogSchema.TryGetProperty("narrative", out _).Should().BeTrue();

        schemas.TryGetProperty("AnalyzeRequestOptionsContract", out var requestOptionsSchema).Should().BeTrue();
        requestOptionsSchema.GetProperty("properties").TryGetProperty("useAiRootCauseNarrative", out _).Should().BeTrue();


        var compilerSchema = schemas.GetProperty("AnalyzeCompilerErrorResponse").GetProperty("properties");
        compilerSchema.TryGetProperty("warnings", out _).Should().BeTrue();


        var rootCauseCandidateSchema = schemas.GetProperty("RootCauseCandidateContract").GetProperty("properties");
        rootCauseCandidateSchema.TryGetProperty("likelyCauses", out _).Should().BeTrue();

        var processingSchema = schemas.GetProperty("ProcessingMetadataContract").GetProperty("properties");
        processingSchema.TryGetProperty("ai", out _).Should().BeTrue();
        processingSchema.TryGetProperty("aiTasks", out _).Should().BeTrue();

        schemas.TryGetProperty("AiProcessingMetadataContract", out var aiProcessingSchema).Should().BeTrue();
        var aiProperties = aiProcessingSchema.GetProperty("properties");
        aiProperties.TryGetProperty("requested", out _).Should().BeTrue();
        aiProperties.TryGetProperty("provider", out _).Should().BeTrue();
        aiProperties.TryGetProperty("model", out _).Should().BeTrue();
        aiProperties.TryGetProperty("status", out _).Should().BeTrue();
        aiProperties.TryGetProperty("fallbackUsed", out _).Should().BeTrue();
        aiProperties.TryGetProperty("reason", out _).Should().BeTrue();
        aiProperties.TryGetProperty("feature", out _).Should().BeTrue();

        schemas.TryGetProperty("AnalysisNarrativeContract", out var narrativeSchema).Should().BeTrue();
        var narrativeProperties = narrativeSchema.GetProperty("properties");
        narrativeProperties.TryGetProperty("summary", out _).Should().BeTrue();
        narrativeProperties.TryGetProperty("groupSummaries", out _).Should().BeTrue();
        narrativeProperties.TryGetProperty("recommendedNextSteps", out _).Should().BeTrue();
        narrativeProperties.TryGetProperty("source", out _).Should().BeTrue();
    }

    private static void AssertOperation(JsonElement operation, string requestSchemaName, string successSchemaName)
    {
        var requestSchemaRef = operation
            .GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();

        requestSchemaRef.Should().EndWith("/" + requestSchemaName);

        var responses = operation.GetProperty("responses");
        responses.TryGetProperty("200", out var okResponse).Should().BeTrue();
        responses.TryGetProperty("400", out _).Should().BeTrue();
        responses.TryGetProperty("413", out _).Should().BeTrue();
        responses.TryGetProperty("415", out _).Should().BeTrue();
        responses.TryGetProperty("500", out _).Should().BeTrue();

        var okSchemaRef = okResponse
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();

        okSchemaRef.Should().EndWith("/" + successSchemaName);

        var errorSchemaRef = responses
            .GetProperty("400")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();

        errorSchemaRef.Should().EndWith("/ApiErrorResponse");
    }
}
