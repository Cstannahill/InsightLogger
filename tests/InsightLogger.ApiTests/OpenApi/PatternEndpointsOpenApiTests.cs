using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace InsightLogger.ApiTests.OpenApi;

public sealed class PatternEndpointsOpenApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PatternEndpointsOpenApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpenApiDocument_ShouldExposePatternAndFingerprintLookupContracts()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var paths = root.GetProperty("paths");
        var schemas = root.GetProperty("components").GetProperty("schemas");

        paths.TryGetProperty("/errors/{fingerprint}", out var errorLookupPath).Should().BeTrue();
        paths.TryGetProperty("/patterns/top", out var topPatternsPath).Should().BeTrue();

        schemas.TryGetProperty("GetErrorByFingerprintResponse", out var errorLookupSchema).Should().BeTrue();
        schemas.TryGetProperty("GetTopPatternsResponse", out _).Should().BeTrue();
        schemas.TryGetProperty("TopPatternItemContract", out _).Should().BeTrue();
        schemas.TryGetProperty("RelatedRuleSummaryContract", out var relatedRuleSchema).Should().BeTrue();
        schemas.TryGetProperty("KnowledgeReferenceContract", out _).Should().BeTrue();
        schemas.TryGetProperty("ApiErrorResponse", out _).Should().BeTrue();

        var relatedRuleProperties = relatedRuleSchema.GetProperty("properties");
        relatedRuleProperties.TryGetProperty("matchedBy", out _).Should().BeTrue();
        relatedRuleProperties.TryGetProperty("matchCount", out _).Should().BeTrue();
        relatedRuleProperties.TryGetProperty("lastMatchedAt", out _).Should().BeTrue();
        relatedRuleProperties.TryGetProperty("projectName", out _).Should().BeTrue();
        relatedRuleProperties.TryGetProperty("repository", out _).Should().BeTrue();

        var errorLookupProperties = errorLookupSchema.GetProperty("properties");
        errorLookupProperties.TryGetProperty("diagnosticCode", out _).Should().BeTrue();
        errorLookupProperties.TryGetProperty("knowledgeReferences", out _).Should().BeTrue();

        var errorLookupResponses = errorLookupPath.GetProperty("get").GetProperty("responses");
        errorLookupResponses.TryGetProperty("200", out _).Should().BeTrue();
        errorLookupResponses.TryGetProperty("404", out _).Should().BeTrue();

        var topPatternsResponses = topPatternsPath.GetProperty("get").GetProperty("responses");
        topPatternsResponses.TryGetProperty("200", out _).Should().BeTrue();
        topPatternsResponses.TryGetProperty("400", out _).Should().BeTrue();
    }
}
