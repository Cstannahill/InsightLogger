using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
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
using InsightLogger.Infrastructure.Parsing;
using InsightLogger.Infrastructure.Parsing.Detection;
using InsightLogger.Infrastructure.Parsing.DotNet;
using InsightLogger.Infrastructure.Parsing.JavaScript;
using InsightLogger.Infrastructure.Parsing.Python;
using InsightLogger.Infrastructure.Parsing.TypeScript;
using Xunit;

namespace InsightLogger.ApplicationTests.Analyses;

public sealed class AnalysisServiceTests
{
    private readonly AnalysisService _service;

    public AnalysisServiceTests()
    {
        var detector = new DefaultToolDetector();
        var parserCoordinator = new DiagnosticParserCoordinator(new InsightLogger.Application.Abstractions.Parsing.IDiagnosticParser[] { new DotNetDiagnosticParser(), new TypeScriptDiagnosticParser(), new ViteDiagnosticParser(), new NpmDiagnosticParser(), new PythonTracebackParser() });
        var ruleMatchingService = new RuleMatchingService(new NoRulesRepository(), new NoRulesMatcher());

        _service = new AnalysisService(
            detector,
            parserCoordinator,
            new DiagnosticGroupingService(),
            new AnalysisNarrativeFactory(),
            new RootCauseRankingService(),
            ruleMatchingService);
    }

    [Fact]
    public async Task Analyze_single_diagnostic_returns_group_and_primary_candidate()
    {
        var result = await _service.AnalyzeAsync(new AnalyzeInputCommand(
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            InputType: InputType.SingleDiagnostic,
            CorrelationId: "corr_demo_001"));

        result.ToolDetected.Should().Be(ToolKind.DotNet);
        result.Diagnostics.Should().ContainSingle();
        result.Groups.Should().ContainSingle();
        result.RootCauseCandidates.Should().ContainSingle();
        result.Summary.TotalDiagnostics.Should().Be(1);
        result.Summary.GroupCount.Should().Be(1);
        result.Summary.PrimaryIssueCount.Should().Be(1);
        result.Processing.Parser.Should().Be("dotnet-diagnostic-parser-v1");
        result.Processing.CorrelationId.Should().Be("corr_demo_001");

        var candidate = result.RootCauseCandidates[0];
        candidate.Title.Should().Be("Unknown symbol in current context");
        candidate.Explanation.Should().Contain("cannot resolve");
        candidate.LikelyCauses.Should().Contain("Typo in variable or member name");
        candidate.SuggestedFixes.Should().NotBeEmpty();
        candidate.Signals.Should().Contain("diagnostic-code:CS0103");
    }

    [Fact]
    public async Task Analyze_build_log_groups_repeated_fingerprints()
    {
        const string content = """
Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context
Program.cs(15,9): error CS0103: The name 'servicez' does not exist in the current context
""";

        var result = await _service.AnalyzeAsync(new AnalyzeInputCommand(
            Content: content,
            InputType: InputType.BuildLog));

        result.Diagnostics.Should().HaveCount(2);
        result.Groups.Should().ContainSingle();
        result.Groups[0].Count.Should().Be(2);
        result.RootCauseCandidates.Should().ContainSingle();
        result.RootCauseCandidates[0].Signals.Should().Contain("group-count:2");
        result.Narrative.Should().NotBeNull();
        result.Narrative!.Source.Should().Be("deterministic");
        result.Narrative.GroupSummaries.Should().NotBeEmpty();
    }


    [Fact]
    public async Task Analyze_typescript_diagnostic_returns_typescript_root_cause_candidate()
    {
        var result = await _service.AnalyzeAsync(new AnalyzeInputCommand(
            Content: "src/app.ts:5:13 - error TS2304: Cannot find name 'usre'.",
            InputType: InputType.SingleDiagnostic,
            CorrelationId: "corr_ts_001"));

        result.ToolDetected.Should().Be(ToolKind.TypeScript);
        result.Diagnostics.Should().ContainSingle();
        result.RootCauseCandidates.Should().ContainSingle();
        result.Diagnostics[0].Code.Should().Be("TS2304");
        result.Diagnostics[0].Category.Should().Be(DiagnosticCategory.MissingSymbol);
        result.Processing.Parser.Should().Be("typescript-diagnostic-parser-v1");
        result.RootCauseCandidates[0].Signals.Should().Contain("diagnostic-code:TS2304");
    }



    [Fact]
    public async Task Analyze_python_traceback_returns_python_root_cause_candidate()
    {
        const string content = """
Traceback (most recent call last):
  File "src/main.py", line 8, in <module>
    run()
  File "src/main.py", line 5, in run
    print(usre_name)
NameError: name 'usre_name' is not defined
""";

        var result = await _service.AnalyzeAsync(new AnalyzeInputCommand(
            Content: content,
            InputType: InputType.BuildLog,
            CorrelationId: "corr_py_001"));

        result.ToolDetected.Should().Be(ToolKind.Python);
        result.Diagnostics.Should().ContainSingle();
        result.RootCauseCandidates.Should().ContainSingle();
        result.Diagnostics[0].Code.Should().Be("NameError");
        result.Diagnostics[0].Category.Should().Be(DiagnosticCategory.MissingSymbol);
        result.Processing.Parser.Should().Be("python-traceback-parser-v1");
        result.RootCauseCandidates[0].Signals.Should().Contain("diagnostic-code:NameError");
    }


    [Fact]
    public async Task Analyze_vite_build_error_returns_vite_root_cause_candidate()
    {
        const string content = """
vite v5.4.8 building for production...
transforming...
✓ 3 modules transformed.
x Build failed in 112ms
error during build:
[vite]: Rollup failed to resolve import "axiosx" from "/src/main.ts".
This is most likely unintended because it can break your application at runtime.
""";

        var result = await _service.AnalyzeAsync(new AnalyzeInputCommand(
            Content: content,
            InputType: InputType.BuildLog,
            CorrelationId: "corr_vite_001"));

        result.ToolDetected.Should().Be(ToolKind.Vite);
        result.Diagnostics.Should().ContainSingle();
        result.RootCauseCandidates.Should().ContainSingle();
        result.Diagnostics[0].Code.Should().Be("VITE_RESOLVE_IMPORT");
        result.Diagnostics[0].Category.Should().Be(DiagnosticCategory.Dependency);
        result.Processing.Parser.Should().Be("vite-diagnostic-parser-v1");
        result.RootCauseCandidates[0].Signals.Should().Contain("diagnostic-code:VITE_RESOLVE_IMPORT");
    }

    [Fact]
    public async Task Analyze_npm_error_returns_npm_root_cause_candidate()
    {
        const string content = """
npm ERR! Missing script: "build"
npm ERR!
npm ERR! To see a list of scripts, run:
npm ERR!   npm run
""";

        var result = await _service.AnalyzeAsync(new AnalyzeInputCommand(
            Content: content,
            InputType: InputType.BuildLog,
            CorrelationId: "corr_npm_001"));

        result.ToolDetected.Should().Be(ToolKind.Npm);
        result.Diagnostics.Should().ContainSingle();
        result.RootCauseCandidates.Should().ContainSingle();
        result.Diagnostics[0].Code.Should().Be("NPM_MISSING_SCRIPT");
        result.Diagnostics[0].Category.Should().Be(DiagnosticCategory.Configuration);
        result.Processing.Parser.Should().Be("npm-diagnostic-parser-v1");
        result.RootCauseCandidates[0].Signals.Should().Contain("diagnostic-code:NPM_MISSING_SCRIPT");
    }

    [Fact]
    public async Task Analyze_returns_empty_result_when_no_parser_is_available()
    {
        var detector = new DefaultToolDetector();
        var parserCoordinator = new DiagnosticParserCoordinator(Array.Empty<InsightLogger.Application.Abstractions.Parsing.IDiagnosticParser>());
        var ruleMatchingService = new RuleMatchingService(new NoRulesRepository(), new NoRulesMatcher());

        var service = new AnalysisService(
            detector,
            parserCoordinator,
            new DiagnosticGroupingService(),
            new AnalysisNarrativeFactory(),
            new RootCauseRankingService(),
            ruleMatchingService);

        var result = await service.AnalyzeAsync(new AnalyzeInputCommand(
            Content: "totally unsupported text",
            InputType: InputType.BuildLog));

        result.ToolDetected.Should().Be(ToolKind.Unknown);
        result.Diagnostics.Should().BeEmpty();
        result.Groups.Should().BeEmpty();
        result.RootCauseCandidates.Should().BeEmpty();
        result.Processing.Parser.Should().BeNull();
        result.Processing.Notes.Should().NotBeNullOrWhiteSpace();
    }

    private sealed class NoRulesRepository : IRuleRepository
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
            => Task.FromResult<IReadOnlyList<Rule>>(Array.Empty<Rule>());

        public Task RecordMatchesAsync(IReadOnlyList<string> ruleIds, DateTimeOffset matchedAtUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<RelatedRuleSummaryDto>> GetRelatedRuleSummariesByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RelatedRuleSummaryDto>>(Array.Empty<RelatedRuleSummaryDto>());
    }

    private sealed class NoRulesMatcher : IRuleMatcher
    {
        public Task<IReadOnlyList<RuleApplicationResult>> MatchAsync(
            IReadOnlyList<Rule> rules,
            IReadOnlyList<DiagnosticRecord> diagnostics,
            IReadOnlyList<DiagnosticGroup> groups,
            IReadOnlyDictionary<string, string>? context = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RuleApplicationResult>>(Array.Empty<RuleApplicationResult>());
    }
}
