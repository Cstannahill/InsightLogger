using System.Text.Json;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.Infrastructure.Persistence.Entities;
using InsightLogger.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InsightLogger.InfrastructureTests.Persistence;

public sealed class EfCoreRuleRepositoryTests
{
    [Fact]
    public async Task CreateAndQueryAsync_Persists_And_Returns_Related_Rules_From_Representative_Diagnostic()
    {
        var repository = CreateRepository(out var dbContext);
        await SeedPatternContextAsync(dbContext);

        await repository.CreateAsync(new Rule(
            id: "rule_1",
            name: "Exact fingerprint guidance",
            description: null,
            isEnabled: true,
            priority: 100,
            condition: new RuleCondition(
                ToolKind: ToolKind.DotNet,
                Code: "CS0103",
                Category: DiagnosticCategory.MissingSymbol,
                Fingerprint: "fp_cs0103_name_missing",
                ProjectName: "InsightLogger.Api",
                Repository: "InsightLogger"),
            action: new RuleAction(
                Explanation: "Check symbol scope.",
                SuggestedFixes: new[] { "Check spelling." })));

        await repository.CreateAsync(new Rule(
            id: "rule_2",
            name: "Generic missing symbol fallback",
            description: null,
            isEnabled: true,
            priority: 50,
            condition: new RuleCondition(
                ToolKind: ToolKind.DotNet,
                Category: DiagnosticCategory.MissingSymbol),
            action: new RuleAction(Explanation: "Look for missing declarations.")));

        await repository.CreateAsync(new Rule(
            id: "rule_3",
            name: "Wrong repo scoped rule",
            description: null,
            isEnabled: true,
            priority: 90,
            condition: new RuleCondition(
                ToolKind: ToolKind.DotNet,
                Category: DiagnosticCategory.MissingSymbol,
                Repository: "OtherRepo"),
            action: new RuleAction(Explanation: "Wrong repo.")));

        Assert.True(await repository.ExistsByNameAsync("Exact fingerprint guidance"));

        var enabledRules = await repository.GetEnabledRulesAsync(ToolKind.DotNet);
        Assert.Equal(3, enabledRules.Count);

        var byId = await repository.GetByIdAsync("rule_1");
        Assert.NotNull(byId);
        Assert.Equal("InsightLogger.Api", byId.Condition.ProjectName);
        Assert.Equal("InsightLogger", byId.Condition.Repository);

        var list = await repository.ListAsync(true, ToolKind.DotNet, null, 10, 0);
        Assert.Equal(3, list.Count);

        var count = await repository.CountAsync(true, ToolKind.DotNet, null);
        Assert.Equal(3, count);

        var related = await repository.GetRelatedRuleSummariesByFingerprintAsync("fp_cs0103_name_missing");
        Assert.Equal(2, related.Count);
        Assert.Equal("rule_1", related[0].Id);
        Assert.Contains("fingerprint", related[0].MatchedBy);
        Assert.Contains("code", related[0].MatchedBy);
        Assert.Contains("category", related[0].MatchedBy);
        Assert.Contains("projectName", related[0].MatchedBy);
        Assert.Contains("repository", related[0].MatchedBy);
        Assert.Equal("InsightLogger.Api", related[0].ProjectName);
        Assert.Equal("InsightLogger", related[0].Repository);
        Assert.Equal("rule_2", related[1].Id);
        Assert.Contains("category", related[1].MatchedBy);
    }

    [Fact]
    public async Task RecordMatchesAsync_Increments_Count_And_LastMatchedAt()
    {
        var repository = CreateRepository(out _);

        await repository.CreateAsync(new Rule(
            id: "rule_1",
            name: "DotNet rule",
            description: null,
            isEnabled: true,
            priority: 100,
            condition: new RuleCondition(ToolKind: ToolKind.DotNet, Code: "CS0103"),
            action: new RuleAction(Explanation: "dotnet")));

        var matchedAtUtc = new DateTimeOffset(2026, 03, 24, 1, 10, 0, TimeSpan.Zero);
        await repository.RecordMatchesAsync(new[] { "rule_1", "rule_1" }, matchedAtUtc);

        var reloaded = await repository.GetByIdAsync("rule_1");
        Assert.NotNull(reloaded);
        Assert.Equal(2, reloaded.MatchCount);
        Assert.Equal(matchedAtUtc, reloaded.LastMatchedAtUtc);
    }

    [Fact]
    public async Task ListAsync_Can_Filter_By_Tag_And_UpdateAsync_Persists_Changes()
    {
        var repository = CreateRepository(out _);

        await repository.CreateAsync(new Rule(
            id: "rule_1",
            name: "DotNet rule",
            description: null,
            isEnabled: true,
            priority: 100,
            condition: new RuleCondition(ToolKind: ToolKind.DotNet, Code: "CS0103", ProjectName: "InsightLogger.Api", Repository: "InsightLogger"),
            action: new RuleAction(Explanation: "dotnet"),
            tags: new[] { "compiler", "dotnet" }));

        await repository.CreateAsync(new Rule(
            id: "rule_2",
            name: "Python rule",
            description: null,
            isEnabled: false,
            priority: 50,
            condition: new RuleCondition(ToolKind: ToolKind.Python, Code: "NameError"),
            action: new RuleAction(Explanation: "python"),
            tags: new[] { "runtime", "python" }));

        var filtered = await repository.ListAsync(null, null, "compiler", 10, 0);
        Assert.Single(filtered);
        Assert.Equal("rule_1", filtered[0].Id);
        Assert.Equal("InsightLogger.Api", filtered[0].Condition.ProjectName);
        Assert.Equal("InsightLogger", filtered[0].Condition.Repository);

        var updated = new Rule(
            id: "rule_1",
            name: "DotNet rule",
            description: "updated",
            isEnabled: false,
            priority: 150,
            condition: new RuleCondition(ToolKind: ToolKind.DotNet, Code: "CS8618", ProjectName: "InsightLogger.Domain", Repository: "InsightLogger"),
            action: new RuleAction(Explanation: "updated explanation", MarkAsPrimaryCause: true),
            tags: new[] { "nullable" },
            createdAtUtc: filtered[0].CreatedAtUtc,
            updatedAtUtc: DateTimeOffset.UtcNow,
            matchCount: filtered[0].MatchCount,
            lastMatchedAtUtc: filtered[0].LastMatchedAtUtc);

        await repository.UpdateAsync(updated);

        var reloaded = await repository.GetByIdAsync("rule_1");
        Assert.NotNull(reloaded);
        Assert.False(reloaded.IsEnabled);
        Assert.Equal("CS8618", reloaded.Condition.Code);
        Assert.Equal("InsightLogger.Domain", reloaded.Condition.ProjectName);
        Assert.Equal("InsightLogger", reloaded.Condition.Repository);
        Assert.Contains("nullable", reloaded.Tags);
        Assert.DoesNotContain("compiler", reloaded.Tags);

        var disabledCount = await repository.CountAsync(false, ToolKind.DotNet, null);
        Assert.Equal(1, disabledCount);
    }

    [Fact]
    public async Task ExistsByNameAsync_Can_Exclude_Current_Rule_Id()
    {
        var repository = CreateRepository(out _);

        await repository.CreateAsync(new Rule(
            id: "rule_1",
            name: "Shared name",
            description: null,
            isEnabled: true,
            priority: 100,
            condition: new RuleCondition(ToolKind: ToolKind.DotNet, Code: "CS0103"),
            action: new RuleAction(Explanation: "first")));

        await repository.CreateAsync(new Rule(
            id: "rule_2",
            name: "Other name",
            description: null,
            isEnabled: true,
            priority: 90,
            condition: new RuleCondition(ToolKind: ToolKind.DotNet, Code: "CS8618"),
            action: new RuleAction(Explanation: "second")));

        Assert.False(await repository.ExistsByNameAsync("Shared name", excludingId: "rule_1"));
        Assert.True(await repository.ExistsByNameAsync("Shared name", excludingId: "rule_2"));
    }

    private static EfCoreRuleRepository CreateRepository(out InsightLoggerDbContext dbContext)
    {
        var options = new DbContextOptionsBuilder<InsightLoggerDbContext>()
            .UseInMemoryDatabase(databaseName: $"rules_{Guid.NewGuid():N}")
            .Options;

        dbContext = new InsightLoggerDbContext(options);
        return new EfCoreRuleRepository(dbContext);
    }

    private static async Task SeedPatternContextAsync(InsightLoggerDbContext dbContext)
    {
        dbContext.ErrorPatterns.Add(new ErrorPatternEntity
        {
            Fingerprint = "fp_cs0103_name_missing",
            Title = "Unknown symbol in current context",
            CanonicalMessage = "The name '{identifier}' does not exist in the current context",
            ToolKind = ToolKind.DotNet.ToString(),
            Category = DiagnosticCategory.MissingSymbol.ToString(),
            FirstSeenAtUtc = new DateTimeOffset(2026, 03, 23, 10, 0, 0, TimeSpan.Zero),
            LastSeenAtUtc = new DateTimeOffset(2026, 03, 24, 1, 0, 0, TimeSpan.Zero),
            OccurrenceCount = 4
        });

        dbContext.Analyses.Add(new AnalysisEntity
        {
            Id = "anl_1",
            InputType = "BuildLog",
            ToolDetected = ToolKind.DotNet.ToString(),
            TotalDiagnostics = 1,
            GroupCount = 1,
            PrimaryIssueCount = 1,
            ErrorCount = 1,
            WarningCount = 0,
            UsedAi = false,
            DurationMs = 10,
            RawContentHash = "hash",
            ContextJson = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["projectName"] = "InsightLogger.Api",
                ["repository"] = "InsightLogger"
            }),
            CreatedAtUtc = new DateTimeOffset(2026, 03, 24, 1, 0, 0, TimeSpan.Zero)
        });

        dbContext.Diagnostics.Add(new DiagnosticEntity
        {
            Id = "diag_1",
            AnalysisId = "anl_1",
            ToolKind = ToolKind.DotNet.ToString(),
            Code = "CS0103",
            Severity = Severity.Error.ToString(),
            Message = "The name 'builderz' does not exist in the current context",
            NormalizedMessage = "The name '{identifier}' does not exist in the current context",
            FilePath = "Program.cs",
            RawSnippet = "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            Category = DiagnosticCategory.MissingSymbol.ToString(),
            IsPrimaryCandidate = true,
            Fingerprint = "fp_cs0103_name_missing",
            OrderIndex = 0
        });

        dbContext.PatternOccurrences.Add(new PatternOccurrenceEntity
        {
            Id = "occ_1",
            Fingerprint = "fp_cs0103_name_missing",
            AnalysisId = "anl_1",
            DiagnosticId = "diag_1",
            SeenAtUtc = new DateTimeOffset(2026, 03, 24, 1, 0, 0, TimeSpan.Zero)
        });

        await dbContext.SaveChangesAsync();
    }
}
