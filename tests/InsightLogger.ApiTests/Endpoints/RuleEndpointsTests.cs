using System.Net;
using System.Net.Http.Json;
using System.IO;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Contracts.Rules;
using InsightLogger.Infrastructure.Persistence.Db;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace InsightLogger.ApiTests.Endpoints;

public sealed class RuleEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _connectionString;
    private readonly string _databasePath;

    public RuleEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"insightlogger-api-rule-tests-{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_databasePath};Pooling=False";

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Persistence:Enabled"] = "true",
                    ["Persistence:AutoMigrate"] = "false",
                    ["Persistence:ConnectionString"] = _connectionString
                });
            });
        });
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<InsightLoggerDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        await using var dbContext = new InsightLoggerDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task PostRules_Returns_Created_For_Valid_Request()
    {
        using var client = _factory.CreateClient();

        var request = BuildCreateRequest();
        var response = await client.PostAsJsonAsync("/rules", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostRules_Returns_BadRequest_For_Invalid_Request()
    {
        using var client = _factory.CreateClient();

        var request = new CreateRuleRequest(
            Name: "",
            Description: null,
            Priority: 10,
            IsEnabled: true,
            Conditions: new RuleConditionContract(null, null, null, null, null, null, null),
            Actions: new RuleActionContract(null, null, Array.Empty<string>(), 0d, false),
            Tags: Array.Empty<string>());

        var response = await client.PostAsJsonAsync("/rules", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRules_Returns_Filtered_Items_With_Analytics_Fields()
    {
        using var client = _factory.CreateClient();

        var created = await CreateRuleAsync(client, BuildCreateRequest(tool: "dotnet", tag: "compiler"));
        await CreateRuleAsync(client, BuildCreateRequest(tool: "python", tag: "runtime"));

        var response = await client.GetAsync("/rules?tool=dotnet&tag=compiler&enabled=true&limit=10&offset=0");
        var payload = await response.Content.ReadFromJsonAsync<GetRulesResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Total >= 1);

        var item = Assert.Single(payload.Items.Where(item => item.Id == created.Id));
        Assert.Contains("compiler", item.Tags, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(0, item.MatchCount);
        Assert.Null(item.LastMatchedAt);
    }

    [Fact]
    public async Task GetRuleById_Returns_NotFound_For_Unknown_Id()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/rules/rule_missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutRule_Updates_Existing_Rule()
    {
        using var client = _factory.CreateClient();

        var created = await CreateRuleAsync(client, BuildCreateRequest());
        var update = new UpdateRuleRequest(
            Name: created.Name + " Updated",
            Description: "Updated description",
            Priority: 250,
            IsEnabled: false,
            Conditions: new RuleConditionContract(
                Tool: "dotnet",
                Code: "CS8618",
                Severity: "warning",
                Category: "nullable-safety",
                MessageRegex: null,
                FilePathRegex: "Entity\\.cs$",
                Fingerprint: null),
            Actions: new RuleActionContract(
                Title: "Updated rule",
                Explanation: "Updated explanation",
                SuggestedFixes: new[] { "Use required members." },
                ConfidenceAdjustment: 0.25d,
                MarkAsPrimaryCause: true),
            Tags: new[] { "nullable", "efcore" });

        var response = await client.PutAsJsonAsync($"/rules/{created.Id}", update);
        var payload = await response.Content.ReadFromJsonAsync<GetRuleResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(update.Name, payload.Name);
        Assert.False(payload.IsEnabled);
        Assert.Equal("CS8618", payload.Conditions.Code);
        Assert.Equal("nullable-safety", payload.Conditions.Category);
        Assert.Equal("Updated rule", payload.Actions.Title);
        Assert.Contains("efcore", payload.Tags);
        Assert.Equal(0, payload.MatchCount);
        Assert.Null(payload.LastMatchedAt);
    }

    [Fact]
    public async Task PatchRuleEnabled_Updates_Enabled_State()
    {
        using var client = _factory.CreateClient();

        var created = await CreateRuleAsync(client, BuildCreateRequest());
        var response = await client.PatchAsJsonAsync($"/rules/{created.Id}/enabled", new SetRuleEnabledRequest(false));
        var payload = await response.Content.ReadFromJsonAsync<SetRuleEnabledResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(created.Id, payload.Id);
        Assert.False(payload.IsEnabled);
    }

    [Fact]
    public async Task PostRuleTest_WithInlineRule_Returns_Match_Details()
    {
        using var client = _factory.CreateClient();

        var request = new RuleTestRequest(
            RuleId: null,
            Rule: BuildCreateRequest(),
            Tool: "dotnet",
            InputType: "compiler-error",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context");

        var response = await client.PostAsJsonAsync("/rules/test", request);
        var payload = await response.Content.ReadFromJsonAsync<RuleTestResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Matched);
        Assert.False(payload.Rule.IsPersisted);
        Assert.Equal("dotnet", payload.ToolDetected);
        Assert.NotEmpty(payload.Diagnostics);
        Assert.NotEmpty(payload.Matches);
        Assert.Contains(payload.Matches, match => match.TargetType == "diagnostic");
        Assert.True(payload.RootCauseCandidatesAfter.First().Confidence >= payload.RootCauseCandidatesBefore.First().Confidence);
    }

    [Fact]
    public async Task PostRuleTest_WithPersistedRuleId_Returns_Persisted_Rule_Details()
    {
        using var client = _factory.CreateClient();

        var created = await CreateRuleAsync(client, BuildCreateRequest());
        var request = new RuleTestRequest(
            RuleId: created.Id,
            Rule: null,
            Tool: "dotnet",
            InputType: "compiler-error",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context");

        var response = await client.PostAsJsonAsync("/rules/test", request);
        var payload = await response.Content.ReadFromJsonAsync<RuleTestResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Rule.IsPersisted);
        Assert.Equal(created.Id, payload.Rule.Id);
        Assert.Contains(payload.Matches, match => match.RuleId == created.Id);
    }

    [Fact]
    public async Task PostRuleTest_Returns_NotFound_For_Missing_RuleId()
    {
        using var client = _factory.CreateClient();

        var request = new RuleTestRequest(
            RuleId: "rule_missing",
            Rule: null,
            Tool: "dotnet",
            InputType: "compiler-error",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context");

        var response = await client.PostAsJsonAsync("/rules/test", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeCompilerError_WithPersistedRule_ShouldIncrement_Rule_Match_Stats()
    {
        using var client = _factory.CreateClient();

        var created = await CreateRuleAsync(client, BuildCreateRequest());
        var analyzeRequest = new AnalyzeCompilerErrorRequest(
            Tool: "dotnet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            Context: null,
            Options: new AnalyzeRequestOptionsContract(Persist: true));

        var analyzeResponse = await client.PostAsJsonAsync("/analyze/compiler-error", analyzeRequest);
        analyzeResponse.EnsureSuccessStatusCode();

        var ruleResponse = await client.GetAsync($"/rules/{created.Id}");
        var payload = await ruleResponse.Content.ReadFromJsonAsync<GetRuleResponse>();

        Assert.Equal(HttpStatusCode.OK, ruleResponse.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.MatchCount >= 1);
        Assert.NotNull(payload.LastMatchedAt);
    }

    [Fact]
    public async Task PatchRuleEnabled_ShouldPreserve_Existing_Match_Stats()
    {
        using var client = _factory.CreateClient();

        var created = await CreateRuleAsync(client, BuildCreateRequest());
        var analyzeRequest = new AnalyzeCompilerErrorRequest(
            Tool: "dotnet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            Context: null,
            Options: new AnalyzeRequestOptionsContract(Persist: true));

        var analyzeResponse = await client.PostAsJsonAsync("/analyze/compiler-error", analyzeRequest);
        analyzeResponse.EnsureSuccessStatusCode();

        var beforeToggleResponse = await client.GetAsync($"/rules/{created.Id}");
        var beforeToggle = await beforeToggleResponse.Content.ReadFromJsonAsync<GetRuleResponse>();
        Assert.NotNull(beforeToggle);
        Assert.True(beforeToggle.MatchCount >= 1);

        var toggleResponse = await client.PatchAsJsonAsync($"/rules/{created.Id}/enabled", new SetRuleEnabledRequest(false));
        toggleResponse.EnsureSuccessStatusCode();

        var afterToggleResponse = await client.GetAsync($"/rules/{created.Id}");
        var afterToggle = await afterToggleResponse.Content.ReadFromJsonAsync<GetRuleResponse>();

        Assert.NotNull(afterToggle);
        Assert.Equal(beforeToggle.MatchCount, afterToggle.MatchCount);
        Assert.Equal(beforeToggle.LastMatchedAt, afterToggle.LastMatchedAt);
        Assert.False(afterToggle.IsEnabled);
    }


    [Fact]
    public async Task PostRules_And_GetRuleById_Should_Preserve_Project_And_Repository_Scope()
    {
        using var client = _factory.CreateClient();

        var created = await CreateRuleAsync(client, BuildCreateRequest(projectName: "InsightLogger.Api", repository: "InsightLogger"));
        var response = await client.GetAsync($"/rules/{created.Id}");
        var payload = await response.Content.ReadFromJsonAsync<GetRuleResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("InsightLogger.Api", payload.Conditions.ProjectName);
        Assert.Equal("InsightLogger", payload.Conditions.Repository);
    }

    [Fact]
    public async Task PostRuleTest_WithScopedRule_Requires_Matching_Project_And_Repository()
    {
        using var client = _factory.CreateClient();

        var request = new RuleTestRequest(
            RuleId: null,
            Rule: BuildCreateRequest(projectName: "InsightLogger.Api", repository: "InsightLogger"),
            Tool: "dotnet",
            InputType: "compiler-error",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            ProjectName: "Other.Api",
            Repository: "InsightLogger");

        var noMatchResponse = await client.PostAsJsonAsync("/rules/test", request);
        var noMatchPayload = await noMatchResponse.Content.ReadFromJsonAsync<RuleTestResponse>();

        Assert.Equal(HttpStatusCode.OK, noMatchResponse.StatusCode);
        Assert.NotNull(noMatchPayload);
        Assert.False(noMatchPayload.Matched);
        Assert.Empty(noMatchPayload.Matches);

        var matchingRequest = request with { ProjectName = "InsightLogger.Api" };
        var matchResponse = await client.PostAsJsonAsync("/rules/test", matchingRequest);
        var matchPayload = await matchResponse.Content.ReadFromJsonAsync<RuleTestResponse>();

        Assert.Equal(HttpStatusCode.OK, matchResponse.StatusCode);
        Assert.NotNull(matchPayload);
        Assert.True(matchPayload.Matched);
        Assert.Contains(matchPayload.Matches[0].MatchedConditions, condition => condition == "projectName");
        Assert.Contains(matchPayload.Matches[0].MatchedConditions, condition => condition == "repository");
    }

    private static async Task<CreateRuleResponse> CreateRuleAsync(HttpClient client, CreateRuleRequest request)
    {
        var response = await client.PostAsJsonAsync("/rules", request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CreateRuleResponse>();
        Assert.NotNull(payload);
        return payload;
    }

    private static CreateRuleRequest BuildCreateRequest(string tool = "dotnet", string tag = "compiler", string? projectName = null, string? repository = null)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];

        return new CreateRuleRequest(
            Name: $"Common missing symbol guidance {unique}",
            Description: "Explain common missing symbol failures.",
            Priority: 100,
            IsEnabled: true,
            Conditions: new RuleConditionContract(
                Tool: tool,
                Code: tool == "python" ? "NameError" : "CS0103",
                Severity: null,
                Category: "missing-symbol",
                MessageRegex: null,
                FilePathRegex: null,
                Fingerprint: tool == "python" ? "fp_python_nameerror_not_defined" : "fp_cs0103_name_missing",
                ProjectName: projectName,
                Repository: repository),
            Actions: new RuleActionContract(
                Title: "Unknown symbol in current context",
                Explanation: "The identifier is missing or out of scope.",
                SuggestedFixes: new[] { "Check spelling." },
                ConfidenceAdjustment: 0.15d,
                MarkAsPrimaryCause: false),
            Tags: new[] { tag, unique });
    }
}
