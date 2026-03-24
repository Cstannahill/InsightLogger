using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Application.Abstractions.Ai;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Abstractions.Rules;
using InsightLogger.Application.Analyses.Commands;
using InsightLogger.Application.Analyses.Services;
using InsightLogger.Application.Diagnostics.DTOs;
using InsightLogger.Application.Rules.DTOs;
using InsightLogger.Application.Rules.Services;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;
using Xunit;

namespace InsightLogger.ApplicationTests.Analyses;

public sealed class AnalysisServiceAiEnrichmentTests
{
    [Fact]
    public async Task AnalyzeAsync_Should_Apply_Ai_Enrichment_To_Primary_Candidate_When_Enrichment_Succeeds()
    {
        var diagnostic = CreateDiagnostic();
        var service = CreateService(
            new[] { diagnostic },
            new SuccessfulEnricher(),
            narrativeGenerator: null
        );

        var result = await service.AnalyzeAsync(
            new AnalyzeInputCommand(
                Content: diagnostic.RawSnippet,
                InputType: InputType.SingleDiagnostic,
                ToolHint: ToolKind.DotNet,
                UseAiEnrichment: true
            )
        );

        result.Processing.UsedAi.Should().BeTrue();
        result.Processing.Ai.Should().NotBeNull();
        result.Processing.Ai!.Provider.Should().Be("ollama");
        result.Processing.Ai.Model.Should().Be("qwen3.5:latest");
        result.Processing.Ai.Status.Should().Be("completed");
        result.Processing.Ai.Feature.Should().Be("explanation-enrichment");
        result.Processing.AiTasks.Should().ContainSingle();
        result.RootCauseCandidates.Should().ContainSingle();
        result
            .RootCauseCandidates[0]
            .Explanation.Should()
            .Be("AI-enriched explanation for the missing symbol.");
        result.RootCauseCandidates[0].LikelyCauses.Should().Contain("Typo in local variable name");
        result
            .RootCauseCandidates[0]
            .SuggestedFixes.Should()
            .Contain("Rename 'builderz' to the intended identifier.");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_Should_Return_Deterministic_Result_With_Warning_When_Enrichment_Fails()
    {
        var diagnostic = CreateDiagnostic();
        var service = CreateService(
            new[] { diagnostic },
            new FailedEnricher(),
            narrativeGenerator: null
        );

        var result = await service.AnalyzeAsync(
            new AnalyzeInputCommand(
                Content: diagnostic.RawSnippet,
                InputType: InputType.SingleDiagnostic,
                ToolHint: ToolKind.DotNet,
                UseAiEnrichment: true
            )
        );

        result.Processing.UsedAi.Should().BeFalse();
        result.Processing.Ai.Should().NotBeNull();
        result.Processing.Ai!.Provider.Should().Be("openrouter");
        result.Processing.Ai.Status.Should().Be("degraded");
        result.Processing.Ai.Feature.Should().Be("explanation-enrichment");
        result.Processing.AiTasks.Should().ContainSingle();
        result.RootCauseCandidates[0].Explanation.Should().Contain("cannot resolve");
        result
            .RootCauseCandidates[0]
            .LikelyCauses.Should()
            .Contain("Typo in variable or member name");
        result
            .RootCauseCandidates[0]
            .SuggestedFixes.Should()
            .Contain("Check the symbol name for a typo.");
        result.Warnings.Should().ContainSingle();
        result.Warnings[0].Should().Contain("AI explanation enrichment was requested");
    }

    [Fact]
    public async Task AnalyzeAsync_Should_Apply_Ai_Narrative_For_MultiDiagnostic_Build_Log_When_Generation_Succeeds()
    {
        var diagnostics = new[]
        {
            CreateDiagnostic(
                id: "diag_ai_1",
                message: "The name 'builderz' does not exist in the current context",
                rawSnippet: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context"
            ),
            CreateDiagnostic(
                id: "diag_ai_2",
                message: "The name 'servicez' does not exist in the current context",
                rawSnippet: "Program.cs(15,9): error CS0103: The name 'servicez' does not exist in the current context"
            ),
        };

        var service = CreateService(
            diagnostics,
            new SuccessfulEnricher(),
            new SuccessfulNarrativeGenerator()
        );

        var result = await service.AnalyzeAsync(
            new AnalyzeInputCommand(
                Content: string.Join("\n", diagnostics[0].RawSnippet, diagnostics[1].RawSnippet),
                InputType: InputType.BuildLog,
                ToolHint: ToolKind.DotNet,
                UseAiRootCauseNarrative: true
            )
        );

        result.Processing.UsedAi.Should().BeTrue();
        result.Processing.Ai.Should().NotBeNull();
        result.Processing.Ai!.Feature.Should().Be("root-cause-narrative");
        result.Processing.AiTasks.Should().ContainSingle();
        result.Narrative.Should().NotBeNull();
        result.Narrative!.Source.Should().Be("ai");
        result.Narrative.Provider.Should().Be("ollama");
        result
            .Narrative.Summary.Should()
            .Be(
                "The build is failing because repeated missing-symbol errors point to one unresolved identifier pattern."
            );
        result
            .Narrative.GroupSummaries.Should()
            .Contain("Missing symbol issues are clustered into one repeated fingerprint group.");
        result.RootCauseCandidates[0].Explanation.Should().Contain("cannot resolve");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeAsync_Should_Record_Separate_Ai_Tasks_When_Both_Toggles_Are_Enabled()
    {
        var diagnostics = new[]
        {
            CreateDiagnostic(
                id: "diag_ai_1",
                message: "The name 'builderz' does not exist in the current context",
                rawSnippet: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context"
            ),
            CreateDiagnostic(
                id: "diag_ai_2",
                message: "The name 'servicez' does not exist in the current context",
                rawSnippet: "Program.cs(15,9): error CS0103: The name 'servicez' does not exist in the current context"
            ),
        };

        var service = CreateService(
            diagnostics,
            new SuccessfulEnricher(),
            new SuccessfulNarrativeGenerator()
        );

        var result = await service.AnalyzeAsync(
            new AnalyzeInputCommand(
                Content: string.Join("\n", diagnostics[0].RawSnippet, diagnostics[1].RawSnippet),
                InputType: InputType.BuildLog,
                ToolHint: ToolKind.DotNet,
                UseAiEnrichment: true,
                UseAiRootCauseNarrative: true
            )
        );

        result.Processing.UsedAi.Should().BeTrue();
        result.Processing.Ai.Should().BeNull();
        result.Processing.AiTasks.Should().HaveCount(2);
        result
            .Processing.AiTasks.Should()
            .Contain(task =>
                task.Feature == "explanation-enrichment" && task.Status == "completed"
            );
        result
            .Processing.AiTasks.Should()
            .Contain(task => task.Feature == "root-cause-narrative" && task.Status == "completed");
        result.Narrative.Should().NotBeNull();
        result.Narrative!.Source.Should().Be("ai");
        result
            .RootCauseCandidates[0]
            .Explanation.Should()
            .Be("AI-enriched explanation for the missing symbol.");
    }

    [Fact]
    public async Task AnalyzeAsync_Should_Keep_Deterministic_Narrative_When_Ai_Narrative_Fails()
    {
        var diagnostics = new[]
        {
            CreateDiagnostic(
                id: "diag_ai_1",
                message: "The name 'builderz' does not exist in the current context",
                rawSnippet: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context"
            ),
            CreateDiagnostic(
                id: "diag_ai_2",
                message: "The name 'servicez' does not exist in the current context",
                rawSnippet: "Program.cs(15,9): error CS0103: The name 'servicez' does not exist in the current context"
            ),
        };

        var service = CreateService(
            diagnostics,
            new SuccessfulEnricher(),
            new FailedNarrativeGenerator()
        );

        var result = await service.AnalyzeAsync(
            new AnalyzeInputCommand(
                Content: string.Join("\n", diagnostics[0].RawSnippet, diagnostics[1].RawSnippet),
                InputType: InputType.BuildLog,
                ToolHint: ToolKind.DotNet,
                UseAiRootCauseNarrative: true
            )
        );

        result.Processing.UsedAi.Should().BeFalse();
        result.Processing.Ai.Should().NotBeNull();
        result.Processing.Ai!.Feature.Should().Be("root-cause-narrative");
        result.Processing.Ai.Status.Should().Be("degraded");
        result.Processing.AiTasks.Should().ContainSingle();
        result.Narrative.Should().NotBeNull();
        result.Narrative!.Source.Should().Be("deterministic");
        result.Narrative.Summary.Should().Contain("The .NET log contains 2 diagnostics");
        result.Warnings.Should().ContainSingle();
        result.Warnings[0].Should().Contain("AI root-cause narrative generation was requested");
    }

    private static AnalysisService CreateService(
        IReadOnlyList<DiagnosticRecord> diagnostics,
        IAiExplanationEnricher? enricher,
        IAiRootCauseNarrativeGenerator? narrativeGenerator
    )
    {
        var toolDetector = new FakeToolDetector();
        var parserCoordinator = new FakeParserCoordinator(diagnostics);
        var groupingService = new DiagnosticGroupingService();
        var rankingService = new RootCauseRankingService();
        var ruleMatchingService = new RuleMatchingService(
            new NoRulesRepository(),
            new NoRulesMatcher()
        );

        return new AnalysisService(
            toolDetector,
            parserCoordinator,
            groupingService,
            new AnalysisNarrativeFactory(),
            rankingService,
            ruleMatchingService,
            analysisPersistenceService: null,
            aiExplanationEnricher: enricher,
            aiRootCauseNarrativeGenerator: narrativeGenerator
        );
    }

    private static DiagnosticRecord CreateDiagnostic(
        string id = "diag_ai_1",
        string? message = null,
        string? rawSnippet = null
    ) =>
        new(
            id: id,
            toolKind: ToolKind.DotNet,
            source: "dotnet build",
            code: "CS0103",
            severity: Severity.Error,
            message: message ?? "The name 'builderz' does not exist in the current context",
            normalizedMessage: "The name '{identifier}' does not exist in the current context",
            location: new DiagnosticLocation("Program.cs", 14, 9, null, null),
            rawSnippet: rawSnippet
                ?? "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            category: DiagnosticCategory.MissingSymbol,
            subcategory: null,
            isPrimaryCandidate: true,
            fingerprint: new DiagnosticFingerprint("fp_cs0103_name_missing"),
            metadata: new Dictionary<string, string>()
        );

    private sealed class SuccessfulEnricher : IAiExplanationEnricher
    {
        public Task<AiExplanationEnrichmentResult> EnrichAsync(
            ExplanationEnrichmentRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                AiExplanationEnrichmentResult.Successful(
                    explanation: "AI-enriched explanation for the missing symbol.",
                    likelyCauses: new[]
                    {
                        "Typo in local variable name",
                        "Reference made before declaration",
                    },
                    suggestedFixes: new[]
                    {
                        "Rename 'builderz' to the intended identifier.",
                        "Declare the symbol before it is first used.",
                    },
                    provider: "ollama",
                    model: "qwen3.5:latest"
                )
            );
    }

    private sealed class FailedEnricher : IAiExplanationEnricher
    {
        public Task<AiExplanationEnrichmentResult> EnrichAsync(
            ExplanationEnrichmentRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                AiExplanationEnrichmentResult.Failure(
                    status: "degraded",
                    reason: "timeout",
                    provider: "openrouter",
                    model: "stepfun/step-3.5-flash:free"
                )
            );
    }

    private sealed class SuccessfulNarrativeGenerator : IAiRootCauseNarrativeGenerator
    {
        public Task<AiRootCauseNarrativeResult> GenerateAsync(
            RootCauseNarrativeRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                AiRootCauseNarrativeResult.Successful(
                    summary: "The build is failing because repeated missing-symbol errors point to one unresolved identifier pattern.",
                    groupSummaries: new[]
                    {
                        "Missing symbol issues are clustered into one repeated fingerprint group.",
                        "The first group should be fixed before chasing downstream repeats.",
                    },
                    recommendedNextSteps: new[]
                    {
                        "Fix the first unresolved identifier and rebuild.",
                        "Check for a typo or missing declaration in the earliest failing file.",
                    },
                    provider: "ollama",
                    model: "qwen3.5:latest"
                )
            );
    }

    private sealed class FailedNarrativeGenerator : IAiRootCauseNarrativeGenerator
    {
        public Task<AiRootCauseNarrativeResult> GenerateAsync(
            RootCauseNarrativeRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                AiRootCauseNarrativeResult.Failure(
                    status: "degraded",
                    reason: "timeout",
                    provider: "openrouter",
                    model: "stepfun/step-3.5-flash:free"
                )
            );
    }

    private sealed class FakeToolDetector : IToolDetector
    {
        public Task<ToolDetectionResult> DetectAsync(
            string content,
            ToolKind? hint,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new ToolDetectionResult(ToolKind.DotNet, 1.0d, "hint"));
    }

    private sealed class FakeParserCoordinator : IDiagnosticParserCoordinator
    {
        private readonly IReadOnlyList<DiagnosticRecord> _diagnostics;

        public FakeParserCoordinator(IReadOnlyList<DiagnosticRecord> diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public Task<DiagnosticParserCoordinatorResult> ParseAsync(
            string content,
            InputType inputType,
            ToolKind detectedTool,
            string? correlationId,
            CancellationToken cancellationToken = default
        )
        {
            var parseResult = new ParseDiagnosticsResult(
                ToolKind: ToolKind.DotNet,
                ParserName: "FakeDotNetParser",
                ParseConfidence: 0.97d,
                Diagnostics: _diagnostics,
                TotalSegments: _diagnostics.Count,
                ParsedSegments: _diagnostics.Count,
                UnparsedSegments: Array.Empty<string>()
            );

            return Task.FromResult(
                new DiagnosticParserCoordinatorResult(
                    ToolKind: ToolKind.DotNet,
                    SelectedParserName: "FakeDotNetParser",
                    ParseResult: parseResult,
                    FailureReason: null
                )
            );
        }
    }

    private sealed class NoRulesRepository : IRuleRepository
    {
        public Task<Rule> CreateAsync(Rule rule, CancellationToken cancellationToken = default) =>
            Task.FromResult(rule);

        public Task<bool> ExistsByNameAsync(
            string name,
            string? excludingId = null,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(false);

        public Task<Rule?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Rule?>(null);

        public Task<IReadOnlyList<Rule>> ListAsync(
            bool? isEnabled,
            ToolKind? toolKind,
            string? tag,
            int limit,
            int offset,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<IReadOnlyList<Rule>>(Array.Empty<Rule>());

        public Task<int> CountAsync(
            bool? isEnabled,
            ToolKind? toolKind,
            string? tag,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(0);

        public Task<Rule> UpdateAsync(Rule rule, CancellationToken cancellationToken = default) =>
            Task.FromResult(rule);

        public Task<IReadOnlyList<Rule>> GetEnabledRulesAsync(
            ToolKind? toolKind,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<IReadOnlyList<Rule>>(Array.Empty<Rule>());

        public Task RecordMatchesAsync(
            IReadOnlyList<string> ruleIds,
            DateTimeOffset matchedAtUtc,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task<IReadOnlyList<RelatedRuleSummaryDto>> GetRelatedRuleSummariesByFingerprintAsync(
            string fingerprint,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult<IReadOnlyList<RelatedRuleSummaryDto>>(
                Array.Empty<RelatedRuleSummaryDto>()
            );
    }

    private sealed class NoRulesMatcher : IRuleMatcher
    {
        public Task<IReadOnlyList<RuleApplicationResult>> MatchAsync(
            IReadOnlyList<Rule> rules,
            IReadOnlyList<DiagnosticRecord> diagnostics,
            IReadOnlyList<DiagnosticGroup> groups,
            IReadOnlyDictionary<string, string>? context = null,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult<IReadOnlyList<RuleApplicationResult>>(
                Array.Empty<RuleApplicationResult>()
            );
    }
}
