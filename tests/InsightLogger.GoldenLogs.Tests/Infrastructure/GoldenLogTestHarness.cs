using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Abstractions.Rules;
using InsightLogger.Application.Analyses.Commands;
using InsightLogger.Application.Analyses.Services;
using InsightLogger.Application.DependencyInjection;
using InsightLogger.Application.Diagnostics.DTOs;
using InsightLogger.Application.Rules.DTOs;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;
using InsightLogger.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace InsightLogger.GoldenLogs.Tests.Infrastructure;

public sealed class GoldenLogTestHarness
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _repoRoot;

    public GoldenLogTestHarness()
    {
        var services = new ServiceCollection();
        services.AddInsightLoggerApplication();
        services.AddInsightLoggerInfrastructureParsing();

        services.AddSingleton<IRuleRepository, NoRulesRepository>();
        services.AddSingleton<IRuleMatcher, NoRulesMatcher>();

        _serviceProvider = services.BuildServiceProvider();
        _repoRoot = RepositoryPathResolver.FindRepositoryRoot();
    }

    public async Task<AnalysisResult> ExecuteAsync(GoldenLogCase testCase)
    {
        var analysisService = _serviceProvider.GetRequiredService<IAnalysisService>();
        var inputType = ParseInputType(testCase.InputType);
        var toolHint = ParseToolHintOrNull(testCase.ToolHint);
        var sampleLogFullPath = Path.Combine(_repoRoot, testCase.SampleLogPath);
        var content = File.ReadAllText(sampleLogFullPath);

        var command = new AnalyzeInputCommand(
            Content: content,
            InputType: inputType,
            ToolHint: toolHint,
            CorrelationId: $"golden_{testCase.Id}",
            Context: null,
            Persist: false,
            UseAiEnrichment: false);

        return await analysisService.AnalyzeAsync(command);
    }

    public void AssertMatches(GoldenLogCase testCase, AnalysisResult result)
    {
        if (!string.IsNullOrWhiteSpace(testCase.Expect.ToolDetected))
        {
            result.ToolDetected.Should().Be(ParseToolHintOrNull(testCase.Expect.ToolDetected) ?? ToolKind.Unknown);
        }

        if (testCase.Expect.Summary is not null)
        {
            AssertSummary(testCase.Expect.Summary, result.Summary);
        }

        var rootCauseFingerprints = result.RootCauseCandidates
            .Select(candidate => candidate.Fingerprint.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var expectedFingerprint in testCase.Expect.PrimaryFingerprints)
        {
            rootCauseFingerprints.Should().Contain(expectedFingerprint, $"{testCase.Id} should keep detecting the same primary fingerprint");
        }

        var actualCategories = result.Diagnostics
            .Select(diagnostic => ToContractCategory(diagnostic.Category))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var expectedCategory in testCase.Expect.RequiredCategories)
        {
            actualCategories.Should().Contain(expectedCategory, $"{testCase.Id} should keep classifying into expected categories");
        }

        var actualCodes = result.Diagnostics
            .Where(diagnostic => !string.IsNullOrWhiteSpace(diagnostic.Code))
            .Select(diagnostic => diagnostic.Code!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var expectedCode in testCase.Expect.RequiredDiagnosticCodes)
        {
            actualCodes.Should().Contain(expectedCode, $"{testCase.Id} should keep extracting expected diagnostic codes");
        }

        var diagnosticMessages = result.Diagnostics
            .Select(diagnostic => diagnostic.Message)
            .Concat(result.RootCauseCandidates.Select(candidate => candidate.Explanation))
            .ToArray();

        foreach (var expectedFragment in testCase.Expect.RequiredMessageFragments)
        {
            diagnosticMessages.Should().Contain(message => message.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(testCase.Expect.ParserName))
        {
            result.Processing.Parser.Should().Be(testCase.Expect.ParserName);
        }

        if (testCase.Expect.MaxUnparsedSegments.HasValue)
        {
            result.Processing.UnparsedSegmentCount.Should().BeLessThanOrEqualTo(testCase.Expect.MaxUnparsedSegments.Value);
        }
    }

    private static void AssertSummary(GoldenLogSummaryExpectations expected, AnalysisSummary actual)
    {
        if (expected.TotalDiagnostics.HasValue)
        {
            actual.TotalDiagnostics.Should().Be(expected.TotalDiagnostics.Value);
        }

        if (expected.GroupCount.HasValue)
        {
            actual.GroupCount.Should().Be(expected.GroupCount.Value);
        }

        if (expected.PrimaryIssueCount.HasValue)
        {
            actual.PrimaryIssueCount.Should().Be(expected.PrimaryIssueCount.Value);
        }

        if (expected.ErrorCount.HasValue)
        {
            actual.ErrorCount.Should().Be(expected.ErrorCount.Value);
        }

        if (expected.WarningCount.HasValue)
        {
            actual.WarningCount.Should().Be(expected.WarningCount.Value);
        }
    }

    private static InputType ParseInputType(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "build-log" => InputType.BuildLog,
            "compiler-error" => InputType.SingleDiagnostic,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown golden-log input type.")
        };

    private static ToolKind? ParseToolHintOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "dotnet" => ToolKind.DotNet,
            "typescript" => ToolKind.TypeScript,
            "npm" => ToolKind.Npm,
            "vite" => ToolKind.Vite,
            "python" => ToolKind.Python,
            "generic" => ToolKind.Generic,
            "unknown" => ToolKind.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown golden-log tool hint.")
        };
    }

    private static string ToContractCategory(DiagnosticCategory category) =>
        category switch
        {
            DiagnosticCategory.MissingSymbol => "missing-symbol",
            DiagnosticCategory.TypeMismatch => "type-mismatch",
            DiagnosticCategory.NullableSafety => "nullable-safety",
            DiagnosticCategory.RuntimeEnvironment => "runtime-environment",
            DiagnosticCategory.BuildSystem => "build-system",
            DiagnosticCategory.TestFailure => "test-failure",
            DiagnosticCategory.Dependency => "dependency",
            _ => category.ToString().ToLowerInvariant()
        };

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
