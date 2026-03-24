using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace InsightLogger.ApiTests.OpenApi;

public sealed class OpenApiDocumentTests
{
    [Fact]
    public async Task SwaggerDocument_ShouldExposeAnalysisEndpointsAndErrorResponses()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var paths = root.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/analyze/build-log", out var buildLogPath));
        Assert.True(paths.TryGetProperty("/analyze/compiler-error", out var compilerErrorPath));

        AssertOperation(buildLogPath.GetProperty("post"), "AnalyzeBuildLogRequest", "AnalyzeBuildLogResponse");
        AssertOperation(compilerErrorPath.GetProperty("post"), "AnalyzeCompilerErrorRequest", "AnalyzeCompilerErrorResponse");
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

        Assert.EndsWith("/" + requestSchemaName, requestSchemaRef);

        var responses = operation.GetProperty("responses");
        Assert.True(responses.TryGetProperty("200", out var okResponse));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("413", out _));
        Assert.True(responses.TryGetProperty("415", out _));
        Assert.True(responses.TryGetProperty("500", out _));

        var okSchemaRef = okResponse
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();

        Assert.EndsWith("/" + successSchemaName, okSchemaRef);

        var errorSchemaRef = responses
            .GetProperty("400")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();

        Assert.EndsWith("/ApiErrorResponse", errorSchemaRef);
    }
}

