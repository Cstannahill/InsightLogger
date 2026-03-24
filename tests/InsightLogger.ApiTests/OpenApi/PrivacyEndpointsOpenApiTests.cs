using System.Linq;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace InsightLogger.ApiTests.OpenApi;

public sealed class PrivacyEndpointsOpenApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PrivacyEndpointsOpenApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpenApiDocument_ShouldExposePrivacyEndpointsAndContracts()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        paths.TryGetProperty("/privacy/settings", out var settingsPath).Should().BeTrue();
        paths.TryGetProperty("/privacy/retention/apply", out var retentionPath).Should().BeTrue();
        paths.TryGetProperty("/analyses/{analysisId}/raw-content", out var rawDeletePath).Should().BeTrue();

        schemas.TryGetProperty("GetPrivacySettingsResponse", out _).Should().BeTrue();
        schemas.TryGetProperty("ApplyRetentionPoliciesResponse", out _).Should().BeTrue();

        var requestOptionsSchema = schemas.GetProperty("AnalyzeRequestOptionsContract").GetProperty("properties");
        requestOptionsSchema.TryGetProperty("persistRawContent", out _).Should().BeTrue();

        settingsPath.GetProperty("get").GetProperty("responses").TryGetProperty("200", out _).Should().BeTrue();
        retentionPath.GetProperty("post").GetProperty("responses").TryGetProperty("200", out _).Should().BeTrue();
        rawDeletePath.GetProperty("delete").GetProperty("responses").TryGetProperty("204", out _).Should().BeTrue();

        var getAnalysisProperties = schemas.GetProperty("GetAnalysisResponse").GetProperty("properties");
        getAnalysisProperties.TryGetProperty("rawContentRedacted", out _).Should().BeTrue();
    }
}
