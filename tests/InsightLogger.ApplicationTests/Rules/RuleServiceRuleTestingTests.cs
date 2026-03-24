using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Abstractions.Rules;
using InsightLogger.Application.Analyses.Services;
using InsightLogger.Application.Diagnostics.DTOs;
using InsightLogger.Application.Rules.Commands;
using InsightLogger.Application.Rules.DTOs;
using InsightLogger.Application.Rules.Services;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;
using InsightLogger.Infrastructure.Rules;

namespace InsightLogger.ApplicationTests.Rules;

public sealed class RuleServiceRuleTestingTests
{
    [Fact]
    public async Task TestAsync_WithInlineRule_Returns_Match_And_Updated_Candidates()
    {
        var ruleRepository = new FakeRuleRepository();
        var groupingService = new DiagnosticGroupingService();
        var rankingService = new RootCauseRankingService();
        var ruleMatchingService = new RuleMatchingService(ruleRepository, new FakeRuleMatcher());
        var service = new RuleService(
            ruleRepository,
            new FakeToolDetector(),
            new FakeParserCoordinator(),
            groupingService,
            rankingService,
            ruleMatchingService);

        var command = new TestRuleCommand(
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            InputType: InputType.SingleDiagnostic,
            ToolHint: ToolKind.DotNet,
            RuleId: null,
            DraftRule: new CreateRuleCommand(
                Name: "Inline missing symbol guidance",
                Description: null,
                IsEnabled: true,
                Priority: 100,
                ToolKind: ToolKind.DotNet,
                Code: "CS0103",
                Severity: null,
                Category: DiagnosticCategory.MissingSymbol,
                MessageRegex: null,
                FilePathRegex: null,
                Fingerprint: "fp_cs0103_name_missing",
                Title: "Unknown symbol in current context",
                Explanation: "This usually means the identifier is missing or out of scope.",
                SuggestedFixes: new[] { "Check spelling." },
                ConfidenceAdjustment: 0.20d,
                MarkAsPrimaryCause: false,
                Tags: new[] { "compiler" }));

        var result = await service.TestAsync(command);

        Assert.NotNull(result);
        Assert.False(result.IsPersisted);
        Assert.Equal(ToolKind.DotNet, result.ToolDetected);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Single(result.Matches);
        Assert.True(result.RootCauseCandidatesAfter[0].Confidence >= result.RootCauseCandidatesBefore[0].Confidence);
    }

    [Fact]
    public async Task TestAsync_WithScopedInlineRule_Requires_Matching_Context()
    {
        var ruleRepository = new FakeRuleRepository();
        var groupingService = new DiagnosticGroupingService();
        var rankingService = new RootCauseRankingService();
        var ruleMatchingService = new RuleMatchingService(ruleRepository, new DeterministicRuleMatcher());
        var service = new RuleService(
            ruleRepository,
            new FakeToolDetector(),
            new FakeParserCoordinator(),
            groupingService,
            rankingService,
            ruleMatchingService);

        var draftRule = new CreateRuleCommand(
            Name: "Scoped missing symbol guidance",
            Description: null,
            IsEnabled: true,
            Priority: 100,
            ToolKind: ToolKind.DotNet,
            Code: "CS0103",
            Severity: null,
            Category: DiagnosticCategory.MissingSymbol,
            MessageRegex: null,
            FilePathRegex: null,
            Fingerprint: "fp_cs0103_name_missing",
            Title: "Scoped symbol guidance",
            Explanation: "Only applies to InsightLogger.Api in InsightLogger.",
            SuggestedFixes: new[] { "Check spelling." },
            ConfidenceAdjustment: 0.20d,
            MarkAsPrimaryCause: false,
            Tags: new[] { "compiler" },
            ProjectName: "InsightLogger.Api",
            Repository: "InsightLogger");

        var noMatch = await service.TestAsync(new TestRuleCommand(
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            InputType: InputType.SingleDiagnostic,
            ToolHint: ToolKind.DotNet,
            RuleId: null,
            DraftRule: draftRule,
            Context: new Dictionary<string, string> { ["projectName"] = "Other.Api", ["repository"] = "InsightLogger" }));

        Assert.NotNull(noMatch);
        Assert.Empty(noMatch.Matches);

        var matched = await service.TestAsync(new TestRuleCommand(
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            InputType: InputType.SingleDiagnostic,
            ToolHint: ToolKind.DotNet,
            RuleId: null,
            DraftRule: draftRule,
            Context: new Dictionary<string, string> { ["projectName"] = "InsightLogger.Api", ["repository"] = "InsightLogger" }));

        Assert.NotNull(matched);
        Assert.NotEmpty(matched.Matches);
        Assert.Contains(matched.Matches[0].MatchedConditions, condition => condition == "projectName");
        Assert.Contains(matched.Matches[0].MatchedConditions, condition => condition == "repository");
    }

    [Fact]
    public async Task TestAsync_WithMissingPersistedRule_Returns_Null()
    {
        var ruleRepository = new FakeRuleRepository();
        var groupingService = new DiagnosticGroupingService();
        var rankingService = new RootCauseRankingService();
        var ruleMatchingService = new RuleMatchingService(ruleRepository, new FakeRuleMatcher());
        var service = new RuleService(
            ruleRepository,
            new FakeToolDetector(),
            new FakeParserCoordinator(),
            groupingService,
            rankingService,
            ruleMatchingService);

        var result = await service.TestAsync(new TestRuleCommand(
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            InputType: InputType.SingleDiagnostic,
            ToolHint: ToolKind.DotNet,
            RuleId: "rule_missing",
            DraftRule: null));

        Assert.Null(result);
    }

    private sealed class FakeToolDetector : IToolDetector
    {
        public Task<ToolDetectionResult> DetectAsync(string content, ToolKind? hint, CancellationToken cancellationToken = default)
            => Task.FromResult(new ToolDetectionResult(ToolKind.DotNet, 1.0d, "hint"));
    }

    private sealed class FakeParserCoordinator : IDiagnosticParserCoordinator
    {
        public Task<DiagnosticParserCoordinatorResult> ParseAsync(
            string content,
            InputType inputType,
            ToolKind detectedTool,
            string? correlationId,
            CancellationToken cancellationToken = default)
        {
            var diagnostic = new DiagnosticRecord(
                id: "diag_1",
                toolKind: ToolKind.DotNet,
                source: "dotnet build",
                code: "CS0103",
                severity: Severity.Error,
                message: "The name 'builderz' does not exist in the current context",
                normalizedMessage: "The name '{identifier}' does not exist in the current context",
                location: new DiagnosticLocation("Program.cs", 14, 9, null, null),
                rawSnippet: content,
                category: DiagnosticCategory.MissingSymbol,
                subcategory: null,
                isPrimaryCandidate: true,
                fingerprint: new DiagnosticFingerprint("fp_cs0103_name_missing"),
                metadata: new Dictionary<string, string>());

            var parseResult = new ParseDiagnosticsResult(
                ToolKind: ToolKind.DotNet,
                ParserName: "FakeDotNetParser",
                ParseConfidence: 0.95d,
                Diagnostics: new[] { diagnostic },
                TotalSegments: 1,
                ParsedSegments: 1,
                UnparsedSegments: Array.Empty<string>());

            return Task.FromResult(new DiagnosticParserCoordinatorResult(
                ToolKind: ToolKind.DotNet,
                SelectedParserName: "FakeDotNetParser",
                ParseResult: parseResult,
                FailureReason: null));
        }
    }

    private sealed class FakeRuleRepository : IRuleRepository
    {
        public Task<Rule> CreateAsync(Rule rule, CancellationToken cancellationToken = default) => Task.FromResult(rule);

        public Task<bool> ExistsByNameAsync(string name, string? excludingId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<Rule?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(id, "rule_saved", StringComparison.Ordinal))
            {
                return Task.FromResult<Rule?>(null);
            }

            return Task.FromResult<Rule?>(new Rule(
                id: "rule_saved",
                name: "Persisted missing symbol guidance",
                description: null,
                isEnabled: false,
                priority: 100,
                condition: new RuleCondition(ToolKind.DotNet, "CS0103", null, DiagnosticCategory.MissingSymbol, null, null, "fp_cs0103_name_missing"),
                action: new RuleAction(
                    Title: "Unknown symbol in current context",
                    Explanation: "This usually means the identifier is missing or out of scope.",
                    SuggestedFixes: new[] { "Check spelling." },
                    ConfidenceAdjustment: 0.20d)));
        }

        public Task<IReadOnlyList<Rule>> ListAsync(bool? isEnabled, ToolKind? toolKind, string? tag, int limit, int offset, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Rule>>(Array.Empty<Rule>());

        public Task<int> CountAsync(bool? isEnabled, ToolKind? toolKind, string? tag, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<Rule> UpdateAsync(Rule rule, CancellationToken cancellationToken = default) => Task.FromResult(rule);

        public Task<IReadOnlyList<Rule>> GetEnabledRulesAsync(ToolKind? toolKind, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Rule>>(Array.Empty<Rule>());

        public Task RecordMatchesAsync(IReadOnlyList<string> ruleIds, DateTimeOffset matchedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<RelatedRuleSummaryDto>> GetRelatedRuleSummariesByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RelatedRuleSummaryDto>>(Array.Empty<RelatedRuleSummaryDto>());
    }

    private sealed class FakeRuleMatcher : IRuleMatcher
    {
        public Task<IReadOnlyList<RuleApplicationResult>> MatchAsync(
            IReadOnlyList<Rule> rules,
            IReadOnlyList<DiagnosticRecord> diagnostics,
            IReadOnlyList<DiagnosticGroup> groups,
            IReadOnlyDictionary<string, string>? context = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<RuleApplicationResult> matches =
            [
                new RuleApplicationResult(
                    Rule: rules[0],
                    TargetType: "diagnostic",
                    TargetId: diagnostics[0].Id,
                    MatchedFingerprint: diagnostics[0].Fingerprint?.Value,
                    MatchedConditions: new[] { "tool", "code", "fingerprint" },
                    AppliedActions: new[] { "title", "explanation", "suggestedFixes", "confidenceAdjustment" })
            ];

            return Task.FromResult(matches);
        }
    }
}
