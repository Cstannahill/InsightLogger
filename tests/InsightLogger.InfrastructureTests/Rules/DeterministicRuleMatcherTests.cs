using System.Collections.Generic;
using System.Threading.Tasks;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;
using InsightLogger.Infrastructure.Rules;

namespace InsightLogger.InfrastructureTests.Rules;

public sealed class DeterministicRuleMatcherTests
{
    [Fact]
    public async Task MatchAsync_Matches_ExactFingerprint_And_Code_Rule()
    {
        var rule = new Rule(
            id: "rule_1",
            name: "Common missing symbol guidance",
            description: null,
            isEnabled: true,
            priority: 100,
            condition: new RuleCondition(
                ToolKind: ToolKind.DotNet,
                Code: "CS0103",
                Fingerprint: "fp_cs0103_name_missing"),
            action: new RuleAction(
                Title: "Unknown symbol in current context",
                Explanation: "This usually means the identifier is out of scope or not declared.",
                SuggestedFixes: new[] { "Check spelling.", "Ensure the symbol is declared and in scope." }));

        var diagnostic = BuildDiagnostic();

        var matcher = new DeterministicRuleMatcher();
        var matches = await matcher.MatchAsync(new[] { rule }, new[] { diagnostic }, Array.Empty<DiagnosticGroup>());

        Assert.Single(matches);
        Assert.Equal("diagnostic", matches[0].TargetType);
        Assert.Equal("diag_1", matches[0].TargetId);
        Assert.Contains("code", matches[0].MatchedConditions);
        Assert.Contains("fingerprint", matches[0].MatchedConditions);
    }

    [Fact]
    public async Task MatchAsync_Respects_Project_And_Repository_Scope()
    {
        var rule = new Rule(
            id: "rule_scoped",
            name: "Scoped missing symbol guidance",
            description: null,
            isEnabled: true,
            priority: 100,
            condition: new RuleCondition(
                ToolKind: ToolKind.DotNet,
                Code: "CS0103",
                Fingerprint: "fp_cs0103_name_missing",
                ProjectName: "InsightLogger.Api",
                Repository: "InsightLogger"),
            action: new RuleAction(Explanation: "Scoped guidance."));

        var diagnostic = BuildDiagnostic();
        var matcher = new DeterministicRuleMatcher();

        var noMatch = await matcher.MatchAsync(
            new[] { rule },
            new[] { diagnostic },
            Array.Empty<DiagnosticGroup>(),
            new Dictionary<string, string> { ["projectName"] = "Other.Api", ["repository"] = "InsightLogger" });

        Assert.Empty(noMatch);

        var matched = await matcher.MatchAsync(
            new[] { rule },
            new[] { diagnostic },
            Array.Empty<DiagnosticGroup>(),
            new Dictionary<string, string> { ["projectName"] = "InsightLogger.Api", ["repository"] = "InsightLogger" });

        Assert.Single(matched);
        Assert.Contains("projectName", matched[0].MatchedConditions);
        Assert.Contains("repository", matched[0].MatchedConditions);
    }

    private static DiagnosticRecord BuildDiagnostic() =>
        new(
            id: "diag_1",
            toolKind: ToolKind.DotNet,
            source: "dotnet build",
            code: "CS0103",
            severity: Severity.Error,
            message: "The name 'foo' does not exist in the current context",
            normalizedMessage: "The name '{identifier}' does not exist in the current context",
            location: new DiagnosticLocation("Program.cs", 10, 5, null, null),
            rawSnippet: string.Empty,
            category: DiagnosticCategory.MissingSymbol,
            subcategory: null,
            isPrimaryCandidate: true,
            fingerprint: new DiagnosticFingerprint("fp_cs0103_name_missing"),
            metadata: new Dictionary<string, string>());
}
