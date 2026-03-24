using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InsightLogger.ApiTests.OpenApi;

public sealed class RuleEndpointsOpenApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RuleEndpointsOpenApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpenApi_Contains_Rule_Collection_Operations()
    {
        using var client = _factory.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");

        using var document = JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");

        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.TryGetProperty("GetRuleResponse", out var getRuleSchema));
        Assert.True(schemas.TryGetProperty("RuleListItemContract", out var listItemSchema));

        var getRuleProperties = getRuleSchema.GetProperty("properties");
        Assert.True(getRuleProperties.TryGetProperty("matchCount", out _));
        Assert.True(getRuleProperties.TryGetProperty("lastMatchedAt", out _));

        Assert.True(schemas.TryGetProperty("RuleConditionContract", out var conditionSchema));
        var conditionProperties = conditionSchema.GetProperty("properties");
        Assert.True(conditionProperties.TryGetProperty("projectName", out _));
        Assert.True(conditionProperties.TryGetProperty("repository", out _));

        var listItemProperties = listItemSchema.GetProperty("properties");
        Assert.True(listItemProperties.TryGetProperty("matchCount", out _));
        Assert.True(listItemProperties.TryGetProperty("lastMatchedAt", out _));
        Assert.True(listItemProperties.TryGetProperty("projectName", out _));
        Assert.True(listItemProperties.TryGetProperty("repository", out _));

        Assert.True(paths.TryGetProperty("/rules", out var rulesPath));
        Assert.True(rulesPath.TryGetProperty("post", out var postOperation));
        Assert.True(rulesPath.TryGetProperty("get", out var getOperation));

        var postResponses = postOperation.GetProperty("responses");
        Assert.True(postResponses.TryGetProperty("201", out _));
        Assert.True(postResponses.TryGetProperty("400", out _));
        Assert.True(postResponses.TryGetProperty("409", out _));

        var getResponses = getOperation.GetProperty("responses");
        Assert.True(getResponses.TryGetProperty("200", out _));
        Assert.True(getResponses.TryGetProperty("400", out _));
    }

    [Fact]
    public async Task OpenApi_Contains_Rule_Item_Operations()
    {
        using var client = _factory.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");

        using var document = JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/rules/{id}", out var ruleByIdPath));
        Assert.True(ruleByIdPath.TryGetProperty("get", out var getOperation));
        Assert.True(ruleByIdPath.TryGetProperty("put", out var putOperation));

        var getResponses = getOperation.GetProperty("responses");
        Assert.True(getResponses.TryGetProperty("200", out _));
        Assert.True(getResponses.TryGetProperty("404", out _));

        var putResponses = putOperation.GetProperty("responses");
        Assert.True(putResponses.TryGetProperty("200", out _));
        Assert.True(putResponses.TryGetProperty("400", out _));
        Assert.True(putResponses.TryGetProperty("404", out _));
        Assert.True(putResponses.TryGetProperty("409", out _));

        Assert.True(paths.TryGetProperty("/rules/{id}/enabled", out var enabledPath));
        Assert.True(enabledPath.TryGetProperty("patch", out var patchOperation));

        var patchResponses = patchOperation.GetProperty("responses");
        Assert.True(patchResponses.TryGetProperty("200", out _));
        Assert.True(patchResponses.TryGetProperty("404", out _));

        Assert.True(paths.TryGetProperty("/rules/test", out var testPath));
        Assert.True(testPath.TryGetProperty("post", out var testOperation));

        var testResponses = testOperation.GetProperty("responses");
        Assert.True(testResponses.TryGetProperty("200", out _));
        Assert.True(testResponses.TryGetProperty("400", out _));
        Assert.True(testResponses.TryGetProperty("404", out _));
    }
}
