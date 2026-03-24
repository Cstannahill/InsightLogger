using System.Linq;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Contracts.Common;
using InsightLogger.ApiTests.Infrastructure;
using Xunit;

namespace InsightLogger.ApiTests.Endpoints;

public sealed class AnalysisNarrativeEndpointsTests : IClassFixture<ApiTestWebApplicationFactory>
{
    private readonly ApiTestWebApplicationFactory _factory;

    public AnalysisNarrativeEndpointsTests(ApiTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetNarratives_And_GetNarrativeById_ShouldReturnPersistedNarrativeHistory()
    {
        using var client = _factory.CreateClient();

        var analyzeRequest = new AnalyzeBuildLogRequest(
            Tool: "dotnet",
            Content: """
Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context
Models/User.cs(6,19): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
Build FAILED.
""",
            ProjectName: "InsightLogger.Api",
            Repository: "InsightLogger",
            Options: new AnalyzeRequestOptionsContract(
                Persist: true,
                UseAiEnrichment: false,
                IncludeRawDiagnostics: true,
                IncludeGroups: true,
                IncludeProcessingMetadata: true,
                UseAiRootCauseNarrative: false));

        using var analyzeResponse = await client.PostAsJsonAsync("/analyze/build-log", analyzeRequest);
        var analyzePayload = await analyzeResponse.Content.ReadFromJsonAsync<AnalyzeBuildLogResponse>();

        analyzeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        analyzePayload.Should().NotBeNull();
        analyzePayload!.Narrative.Should().NotBeNull();

        var history = await client.GetFromJsonAsync<GetAnalysisNarrativesResponse>("/analyses/narratives?repository=InsightLogger&limit=10");
        history.Should().NotBeNull();
        history!.Items.Should().Contain(item => item.AnalysisId == analyzePayload.AnalysisId);

        var historyItem = history.Items.Single(item => item.AnalysisId == analyzePayload.AnalysisId);
        historyItem.Repository.Should().Be("InsightLogger");
        historyItem.ProjectName.Should().Be("InsightLogger.Api");
        historyItem.Summary.TotalDiagnostics.Should().BeGreaterThan(1);
        historyItem.SummaryText.Should().NotBeNullOrWhiteSpace();
        historyItem.MatchedFields.Should().BeEmpty();
        historyItem.MatchSnippet.Should().BeNull();

        var detail = await client.GetFromJsonAsync<GetAnalysisNarrativeResponse>($"/analyses/{analyzePayload.AnalysisId}/narrative");
        detail.Should().NotBeNull();
        detail!.AnalysisId.Should().Be(analyzePayload.AnalysisId);
        detail.ProjectName.Should().Be("InsightLogger.Api");
        detail.Repository.Should().Be("InsightLogger");
        detail.Narrative.Summary.Should().NotBeNullOrWhiteSpace();
        detail.Narrative.GroupSummaries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetNarratives_ShouldSupportTextSearch_AndReturnMatchMetadata()
    {
        using var client = _factory.CreateClient();

        var analyzeRequest = new AnalyzeBuildLogRequest(
            Tool: "dotnet",
            Content: """
Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context
Models/User.cs(6,19): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider declaring the property as nullable.
Build FAILED.
""",
            ProjectName: "InsightLogger.Api",
            Repository: "InsightLogger",
            Options: new AnalyzeRequestOptionsContract(
                Persist: true,
                UseAiEnrichment: false,
                IncludeRawDiagnostics: true,
                IncludeGroups: true,
                IncludeProcessingMetadata: true,
                UseAiRootCauseNarrative: false));

        using var analyzeResponse = await client.PostAsJsonAsync("/analyze/build-log", analyzeRequest);
        var analyzePayload = await analyzeResponse.Content.ReadFromJsonAsync<AnalyzeBuildLogResponse>();

        analyzeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        analyzePayload.Should().NotBeNull();

        var history = await client.GetFromJsonAsync<GetAnalysisNarrativesResponse>("/analyses/narratives?text=nullable&limit=10");
        history.Should().NotBeNull();
        history!.Items.Should().Contain(item => item.AnalysisId == analyzePayload!.AnalysisId);

        var match = history.Items.Single(item => item.AnalysisId == analyzePayload!.AnalysisId);
        match.MatchedFields.Should().Contain(field => field == "summary" || field == "groupSummaries" || field == "recommendedNextSteps");
        match.MatchSnippet.Should().NotBeNullOrWhiteSpace();
        match.MatchSnippet.Should().ContainEquivalentOf("nullable");
    }

    [Fact]
    public async Task GetAnalysisById_ShouldReturnPersistedAnalysisDetail()
    {
        using var client = _factory.CreateClient();

        var analyzeRequest = new AnalyzeBuildLogRequest(
            Tool: "dotnet",
            Content: """
Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context
Program.cs(15,9): error CS0103: The name 'servicez' does not exist in the current context
Build FAILED.
""",
            ProjectName: "InsightLogger.Api",
            Repository: "InsightLogger",
            Options: new AnalyzeRequestOptionsContract(
                Persist: true,
                UseAiEnrichment: false,
                IncludeRawDiagnostics: true,
                IncludeGroups: true,
                IncludeProcessingMetadata: true,
                UseAiRootCauseNarrative: false));

        using var analyzeResponse = await client.PostAsJsonAsync("/analyze/build-log", analyzeRequest);
        var analyzePayload = await analyzeResponse.Content.ReadFromJsonAsync<AnalyzeBuildLogResponse>();

        analyzeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        analyzePayload.Should().NotBeNull();

        var detail = await client.GetFromJsonAsync<GetAnalysisResponse>($"/analyses/{analyzePayload!.AnalysisId}");
        detail.Should().NotBeNull();
        detail!.AnalysisId.Should().Be(analyzePayload.AnalysisId);
        detail.ProjectName.Should().Be("InsightLogger.Api");
        detail.Repository.Should().Be("InsightLogger");
        detail.Summary.TotalDiagnostics.Should().Be(2);
        detail.Diagnostics.Should().HaveCount(2);
        detail.Groups.Should().NotBeEmpty();
        detail.RootCauseCandidates.Should().NotBeEmpty();
        detail.Processing.Should().NotBeNull();
        detail.Context.Should().ContainKey("projectName");
        detail.RawContentStored.Should().BeFalse();
    }

    [Fact]
    public async Task GetNarrativeById_ShouldReturnNotFound_WhenNoNarrativeExists()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/analyses/anl_missing/narrative");
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        payload.Should().NotBeNull();
        payload!.Error.Code.Should().Be("not_found");
    }
}


