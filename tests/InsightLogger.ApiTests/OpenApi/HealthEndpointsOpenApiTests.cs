using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InsightLogger.ApiTests.OpenApi;

public sealed class HealthEndpointsOpenApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointsOpenApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpenApi_Contains_Health_Telemetry_And_Ai_Metadata_Operations()
    {
        using var client = _factory.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");

        using var document = JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        Assert.True(paths.TryGetProperty("/health", out var healthPath));
        Assert.True(healthPath.TryGetProperty("get", out var healthGet));
        Assert.True(healthGet.GetProperty("responses").TryGetProperty("200", out _));

        Assert.True(paths.TryGetProperty("/health/telemetry", out var telemetryPath));
        Assert.True(telemetryPath.TryGetProperty("get", out var telemetryGet));
        Assert.True(telemetryGet.GetProperty("responses").TryGetProperty("200", out _));

        Assert.True(paths.TryGetProperty("/health/ai", out var aiHealthPath));
        Assert.True(aiHealthPath.TryGetProperty("get", out var aiHealthGet));
        Assert.True(aiHealthGet.GetProperty("responses").TryGetProperty("200", out _));

        Assert.True(paths.TryGetProperty("/providers/ai", out var providersPath));
        Assert.True(providersPath.TryGetProperty("get", out var providersGet));
        Assert.True(providersGet.GetProperty("responses").TryGetProperty("200", out _));

        Assert.True(schemas.TryGetProperty("GetHealthResponse", out _));
        Assert.True(schemas.TryGetProperty("GetTelemetryResponse", out _));
        Assert.True(schemas.TryGetProperty("AnalysisTelemetrySummaryContract", out _));
        Assert.True(schemas.TryGetProperty("HttpTelemetrySummaryContract", out _));
        Assert.True(schemas.TryGetProperty("GetAiHealthResponse", out _));
        Assert.True(schemas.TryGetProperty("GetAiProvidersResponse", out _));
        Assert.True(schemas.TryGetProperty("AiProviderItemContract", out _));
        Assert.True(schemas.TryGetProperty("AiProviderHealthItemContract", out _));
    }
}
