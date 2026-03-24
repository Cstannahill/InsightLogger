using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using InsightLogger.ApiTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace InsightLogger.ApiTests.OpenApi;

public sealed class OpenApiContractSnapshotTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OpenApiContractSnapshotTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OpenApiContractProjection_ShouldMatchSnapshot()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var repoRoot = RepositoryPathResolver.FindRepositoryRoot();
        var snapshotPath = Path.Combine(repoRoot, "tests", "InsightLogger.ApiTests", "OpenApi", "Snapshots", "openapi-contract.snapshot.json");
        var projected = ProjectContractSnapshot(json);

        File.Exists(snapshotPath).Should().BeTrue($"snapshot file was not found at {snapshotPath}");
        var expected = await File.ReadAllTextAsync(snapshotPath);

        NormalizeNewLines(projected).TrimEnd().Should().Be(NormalizeNewLines(expected).TrimEnd());
    }

    internal static string ProjectContractSnapshot(string openApiJson)
    {
        var root = JsonNode.Parse(openApiJson)!.AsObject();
        var paths = root["paths"]!.AsObject();
        var schemas = root["components"]!["schemas"]!.AsObject();

        var snapshot = new JsonObject
        {
            ["paths"] = new JsonObject
            {
                ["/analyze/build-log"] = BuildOperationSnapshot(paths["/analyze/build-log"]!["post"]!.AsObject()),
                ["/analyze/compiler-error"] = BuildOperationSnapshot(paths["/analyze/compiler-error"]!["post"]!.AsObject())
            },
            ["schemas"] = new JsonArray(
                "AnalyzeBuildLogRequest",
                "AnalyzeBuildLogResponse",
                "AnalyzeCompilerErrorRequest",
                "AnalyzeCompilerErrorResponse",
                "ApiErrorResponse")
        };

        return snapshot.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static JsonObject BuildOperationSnapshot(JsonObject operation)
    {
        var requestSchema = operation["requestBody"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>();
        var responses = operation["responses"]!.AsObject();

        var responseCodes = new JsonArray();
        foreach (var code in new[] { "200", "400", "413", "415", "500" })
        {
            if (responses.ContainsKey(code))
            {
                responseCodes.Add(code);
            }
        }

        var successSchema = responses["200"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>();
        var errorSchema = responses["400"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>();

        return new JsonObject
        {
            ["requestSchema"] = LastRefSegment(requestSchema),
            ["successSchema"] = LastRefSegment(successSchema),
            ["errorSchema"] = LastRefSegment(errorSchema),
            ["responseCodes"] = responseCodes
        };
    }

    private static string NormalizeNewLines(string value)
    {
        return value.Replace("\r\n", "\n");
    }
    private static string LastRefSegment(string reference)
    {
        var lastSlashIndex = reference.LastIndexOf('/');
        return lastSlashIndex < 0 ? reference : reference[(lastSlashIndex + 1)..];
    }
}



