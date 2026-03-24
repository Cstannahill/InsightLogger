using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Abstractions.Rules;
using InsightLogger.Application.Analyses.Commands;
using InsightLogger.Application.Analyses.Services;
using InsightLogger.Application.Diagnostics.DTOs;
using InsightLogger.Application.Rules.DTOs;
using InsightLogger.Application.Rules.Services;
using InsightLogger.Infrastructure.Rules;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;

namespace InsightLogger.ApplicationTests.Analyses;

public sealed class AnalysisServiceRuleMatchingTests
{
    [Fact]
    public async Task AnalyzeAsync_Attaches_Matched_Rules_And_Enriches_Candidate()
    {
        var diagnostic = new DiagnosticRecord(
            id: "diag_1",
            toolKind: ToolKind.DotNet,
            source: "dotnet build",
            code: "CS0103",
            severity: Severity.Error,
            message: "The name 'foo' does not exist in the current context",
            normalizedMessage: "The name '{identifier}' does not exist in the current context",
            location: new DiagnosticLocation("Program.cs", 5, 9, null, null),
            rawSnippet: string.Empty,
            category: DiagnosticCategory.MissingSymbol,
            subcategory: null,
            isPrimaryCandidate: true,
            fingerprint: new DiagnosticFingerprint("fp_cs0103_name_missing"),
            metadata: new Dictionary<string, string>());

        var toolDetector = new FakeToolDetector();
        var parserCoordinator = new FakeParserCoordinator(diagnostic);
        var groupingService = new DiagnosticGroupingService();
        var rankingService = new RootCauseRankingService();
        var ruleRepository = new FakeRuleRepository();
        var ruleMatcher = new FakeRuleMatcher();
        var ruleMatchingService = new RuleMatchingService(ruleRepository, ruleMatcher);

        var service = new AnalysisService(
            toolDetector,
            parserCoordinator,
            groupingService,
            new AnalysisNarrativeFactory(),
            rankingService,
            ruleMatchingService,
            analysisPersistenceService: null);

        var result = await service.AnalyzeAsync(new AnalyzeInputCommand(
            Content: "Program.cs(5,9): error CS0103: The name 'foo' does not exist in the current context",
            InputType: InputType.BuildLog,
            ToolHint: ToolKind.DotNet));

        Assert.Single(result.MatchedRules);
        Assert.Contains(result.RootCauseCandidates, candidate => candidate.Explanation.Contains("identifier", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.RootCauseCandidates, candidate => candidate.SuggestedFixes.Contains("Check spelling."));
    }


    [Fact]
    public async Task AnalyzeAsync_Respects_Project_And_Repository_Scope_From_Context()
    {
        var diagnostic = new DiagnosticRecord(
            id: "diag_1",
            toolKind: ToolKind.DotNet,
            source: "dotnet build",
            code: "CS0103",
            severity: Severity.Error,
            message: "The name 'foo' does not exist in the current context",
            normalizedMessage: "The name '{identifier}' does not exist in the current context",
            location: new DiagnosticLocation("Program.cs", 5, 9, null, null),
            rawSnippet: string.Empty,
            category: DiagnosticCategory.MissingSymbol,
            subcategory: null,
            isPrimaryCandidate: true,
            fingerprint: new DiagnosticFingerprint("fp_cs0103_name_missing"),
            metadata: new Dictionary<string, string>());

        var toolDetector = new FakeToolDetector();
        var parserCoordinator = new FakeParserCoordinator(diagnostic);
        var groupingService = new DiagnosticGroupingService();
        var rankingService = new RootCauseRankingService();
        var ruleRepository = new ScopedRuleRepository();
        var ruleMatchingService = new RuleMatchingService(ruleRepository, new DeterministicRuleMatcher());

        var service = new AnalysisService(
            toolDetector,
            parserCoordinator,
            groupingService,
            new AnalysisNarrativeFactory(),
            rankingService,
            ruleMatchingService,
            analysisPersistenceService: null);

        var unmatched = await service.AnalyzeAsync(new AnalyzeInputCommand(
            Content: "Program.cs(5,9): error CS0103: The name 'foo' does not exist in the current context",
            InputType: InputType.BuildLog,
            ToolHint: ToolKind.DotNet,
            Context: new Dictionary<string, string>
            {
                ["projectName"] = "Other.Api",
                ["repository"] = "InsightLogger"
            }));

        Assert.Empty(unmatched.MatchedRules);

        var matched = await service.AnalyzeAsync(new AnalyzeInputCommand(
            Content: "Program.cs(5,9): error CS0103: The name 'foo' does not exist in the current context",
            InputType: InputType.BuildLog,
            ToolHint: ToolKind.DotNet,
            Context: new Dictionary<string, string>
            {
                ["projectName"] = "InsightLogger.Api",
                ["repository"] = "InsightLogger"
            }));

        Assert.NotEmpty(matched.MatchedRules);
        Assert.Contains(matched.MatchedRules, rule => rule.TargetType == "diagnostic");
        Assert.Contains(matched.MatchedRules, rule => rule.TargetType == "group");
        Assert.All(
            matched.MatchedRules,
            rule =>
            {
                Assert.Contains("projectName", rule.MatchedConditions);
                Assert.Contains("repository", rule.MatchedConditions);
            });
    }

    private sealed class FakeToolDetector : IToolDetector
    {
        public Task<ToolDetectionResult> DetectAsync(string content, ToolKind? hint, CancellationToken cancellationToken = default)
            => Task.FromResult(new ToolDetectionResult(ToolKind.DotNet, 1.0d, "hint"));
    }

    private sealed class FakeParserCoordinator : IDiagnosticParserCoordinator
    {
        private readonly DiagnosticRecord _diagnostic;

        public FakeParserCoordinator(DiagnosticRecord diagnostic)
        {
            _diagnostic = diagnostic;
        }

        public Task<DiagnosticParserCoordinatorResult> ParseAsync(
            string content,
            InputType inputType,
            ToolKind detectedTool,
            string? correlationId,
            CancellationToken cancellationToken = default)
        {
            var parseResult = new ParseDiagnosticsResult(
                ToolKind: ToolKind.DotNet,
                ParserName: "FakeDotNetParser",
                ParseConfidence: 0.95d,
                Diagnostics: new[] { _diagnostic },
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

        public Task<bool> ExistsByNameAsync(string name, string? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);

        
        public Task<Rule?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<Rule?>(null);

        public Task<IReadOnlyList<Rule>> ListAsync(bool? isEnabled, ToolKind? toolKind, string? tag, int limit, int offset, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Rule>>(Array.Empty<Rule>());

        public Task<int> CountAsync(bool? isEnabled, ToolKind? toolKind, string? tag, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<Rule> UpdateAsync(Rule rule, CancellationToken cancellationToken = default) => Task.FromResult(rule);

        public Task<IReadOnlyList<Rule>> GetEnabledRulesAsync(ToolKind? toolKind, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Rule> rules =
            [
                new Rule(
                    id: "rule_1",
                    name: "Common missing symbol guidance",
                    description: null,
                    isEnabled: true,
                    priority: 100,
                    condition: new RuleCondition(ToolKind.DotNet, "CS0103", null, null, null, null, "fp_cs0103_name_missing"),
                    action: new RuleAction(
                        Title: "Unknown symbol in current context",
                        Explanation: "This usually means the identifier is missing or out of scope.",
                        SuggestedFixes: new[] { "Check spelling." },
                        ConfidenceAdjustment: 0.20d))
            ];

            return Task.FromResult(rules);
        }

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
                    MatchedConditions: new[] { "code", "fingerprint" },
                    AppliedActions: new[] { "title", "explanation", "suggestedFixes", "confidenceAdjustment" })
            ];

            return Task.FromResult(matches);
        }
    }

    private sealed class ScopedRuleRepository : IRuleRepository
    {
        public Task<Rule> CreateAsync(Rule rule, CancellationToken cancellationToken = default) => Task.FromResult(rule);

        public Task<bool> ExistsByNameAsync(string name, string? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<Rule?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult<Rule?>(null);

        public Task<IReadOnlyList<Rule>> ListAsync(bool? isEnabled, ToolKind? toolKind, string? tag, int limit, int offset, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Rule>>(Array.Empty<Rule>());

        public Task<int> CountAsync(bool? isEnabled, ToolKind? toolKind, string? tag, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<Rule> UpdateAsync(Rule rule, CancellationToken cancellationToken = default) => Task.FromResult(rule);

        public Task<IReadOnlyList<Rule>> GetEnabledRulesAsync(ToolKind? toolKind, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Rule> rules =
            [
                new Rule(
                    id: "rule_scoped_1",
                    name: "Scoped missing symbol guidance",
                    description: null,
                    isEnabled: true,
                    priority: 150,
                    condition: new RuleCondition(
                        ToolKind: ToolKind.DotNet,
                        Code: "CS0103",
                        Category: DiagnosticCategory.MissingSymbol,
                        Fingerprint: "fp_cs0103_name_missing",
                        ProjectName: "InsightLogger.Api",
                        Repository: "InsightLogger"),
                    action: new RuleAction(
                        Title: "Scoped missing symbol",
                        Explanation: "Applies only to the InsightLogger.Api project.",
                        SuggestedFixes: new[] { "Check API project symbol scope." },
                        ConfidenceAdjustment: 0.15d,
                        MarkAsPrimaryCause: true))
            ];

            return Task.FromResult(rules);
        }

        public Task RecordMatchesAsync(IReadOnlyList<string> ruleIds, DateTimeOffset matchedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<RelatedRuleSummaryDto>> GetRelatedRuleSummariesByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RelatedRuleSummaryDto>>(Array.Empty<RelatedRuleSummaryDto>());
    }

}

