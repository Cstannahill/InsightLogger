using System;
using System.Collections.Generic;
using System.Linq;
using InsightLogger.Application.Analyses.Commands;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;

namespace InsightLogger.Api.Mapping;

public static class AnalysisContractMapper
{
    public static AnalyzeInputCommand ToCommand(AnalyzeBuildLogRequest request, string? correlationId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AnalyzeInputCommand(
            Content: request.Content?.Trim() ?? string.Empty,
            InputType: InputType.BuildLog,
            ToolHint: ParseToolOrNull(request.Tool),
            CorrelationId: correlationId,
            Context: BuildBuildLogContext(request),
            Persist: request.Options?.Persist ?? false,
            UseAiEnrichment: request.Options?.UseAiEnrichment ?? false,
            UseAiRootCauseNarrative: request.Options?.UseAiRootCauseNarrative ?? false);
    }

    public static AnalyzeInputCommand ToCommand(AnalyzeCompilerErrorRequest request, string? correlationId)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AnalyzeInputCommand(
            Content: request.Content?.Trim() ?? string.Empty,
            InputType: InputType.SingleDiagnostic,
            ToolHint: ParseToolOrNull(request.Tool),
            CorrelationId: correlationId,
            Context: BuildCompilerErrorContext(request),
            Persist: request.Options?.Persist ?? false,
            UseAiEnrichment: request.Options?.UseAiEnrichment ?? false,
            UseAiRootCauseNarrative: false);
    }

    public static AnalyzeBuildLogResponse ToBuildLogResponse(AnalysisResult result, AnalyzeRequestOptionsContract? options = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        var includeDiagnostics = options?.IncludeRawDiagnostics ?? true;
        var includeGroups = options?.IncludeGroups ?? true;
        var includeProcessing = options?.IncludeProcessingMetadata ?? true;

        return new AnalyzeBuildLogResponse(
            AnalysisId: result.AnalysisId,
            ToolDetected: ToContractTool(result.ToolDetected),
            Summary: new AnalysisSummaryContract(
                TotalDiagnostics: result.Summary.TotalDiagnostics,
                GroupCount: result.Summary.GroupCount,
                PrimaryIssueCount: result.Summary.PrimaryIssueCount,
                ErrorCount: result.Summary.ErrorCount,
                WarningCount: result.Summary.WarningCount),
            RootCauseCandidates: result.RootCauseCandidates.Select(ToContract).ToList(),
            Groups: includeGroups ? result.Groups.Select(ToContract).ToList() : Array.Empty<DiagnosticGroupContract>(),
            Diagnostics: includeDiagnostics ? result.Diagnostics.Select(ToContract).ToList() : Array.Empty<DiagnosticContract>(),
            MatchedRules: result.MatchedRules.Select(ToContract).ToList(),
            Narrative: result.Narrative is null ? null : ToContract(result.Narrative),
            Processing: includeProcessing ? ToContract(result.Processing) : null,
            Warnings: result.Warnings);
    }

    public static AnalyzeCompilerErrorResponse ToCompilerErrorResponse(AnalysisResult result, AnalyzeRequestOptionsContract? options = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        var includeProcessing = options?.IncludeProcessingMetadata ?? true;
        var primaryDiagnostic = SelectPrimaryDiagnostic(result);
        var primaryCandidate = SelectPrimaryCandidate(result, primaryDiagnostic);
        var fingerprint = primaryCandidate?.Fingerprint.Value
            ?? primaryDiagnostic?.Fingerprint?.Value
            ?? "fp_unknown";

        return new AnalyzeCompilerErrorResponse(
            Fingerprint: fingerprint,
            ToolDetected: ToContractTool(result.ToolDetected),
            Diagnostic: primaryDiagnostic is null ? null : ToContract(primaryDiagnostic),
            Explanation: primaryCandidate?.Explanation ?? "A structured diagnostic was detected, but no specialized explanation is available yet.",
            LikelyCauses: primaryCandidate?.LikelyCauses ?? BuildLikelyCauses(primaryDiagnostic),
            SuggestedFixes: primaryCandidate?.SuggestedFixes ?? Array.Empty<string>(),
            Confidence: primaryCandidate?.Confidence ?? 0d,
            MatchedRules: result.MatchedRules.Select(ToContract).ToList(),
            Processing: includeProcessing ? ToContract(result.Processing) : null,
            Warnings: result.Warnings);
    }

    public static bool TryParseTool(string? value, out ToolKind toolKind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            toolKind = ToolKind.Unknown;
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "dotnet":
            case ".net":
            case "roslyn":
                toolKind = ToolKind.DotNet;
                return true;
            case "typescript":
            case "ts":
            case "tsc":
                toolKind = ToolKind.TypeScript;
                return true;
            case "npm":
                toolKind = ToolKind.Npm;
                return true;
            case "vite":
                toolKind = ToolKind.Vite;
                return true;
            case "python":
            case "py":
                toolKind = ToolKind.Python;
                return true;
            case "generic":
                toolKind = ToolKind.Generic;
                return true;
            default:
                toolKind = ToolKind.Unknown;
                return false;
        }
    }

    private static ToolKind? ParseToolOrNull(string? value) => TryParseTool(value, out var tool) ? tool : null;

    private static IReadOnlyDictionary<string, string>? BuildBuildLogContext(AnalyzeBuildLogRequest request)
    {
        var context = new Dictionary<string, string>(StringComparer.Ordinal);
        AddIfPresent(context, "projectName", request.ProjectName);
        AddIfPresent(context, "repository", request.Repository);
        AddIfPresent(context, "branch", request.Branch);
        AddIfPresent(context, "commitSha", request.CommitSha);

        if (request.Environment is not null)
        {
            AddIfPresent(context, "environment.os", request.Environment.Os);
            if (request.Environment.Ci.HasValue)
            {
                context["environment.ci"] = request.Environment.Ci.Value ? "true" : "false";
            }
            AddIfPresent(context, "environment.machineName", request.Environment.MachineName);
        }

        return context.Count == 0 ? null : context;
    }

    private static IReadOnlyDictionary<string, string>? BuildCompilerErrorContext(AnalyzeCompilerErrorRequest request)
    {
        var context = new Dictionary<string, string>(StringComparer.Ordinal);

        if (request.Context is not null)
        {
            AddIfPresent(context, "projectName", request.Context.ProjectName);
            AddIfPresent(context, "repository", request.Context.Repository);
            AddIfPresent(context, "branch", request.Context.Branch);
            AddIfPresent(context, "commitSha", request.Context.CommitSha);
        }

        return context.Count == 0 ? null : context;
    }

    private static void AddIfPresent(IDictionary<string, string> context, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            context[key] = value.Trim();
        }
    }

    private static DiagnosticRecord? SelectPrimaryDiagnostic(AnalysisResult result)
    {
        var primaryCandidateDiagnosticId = result.RootCauseCandidates.FirstOrDefault()?.DiagnosticId;
        if (!string.IsNullOrWhiteSpace(primaryCandidateDiagnosticId))
        {
            var match = result.Diagnostics.FirstOrDefault(d => string.Equals(d.Id, primaryCandidateDiagnosticId, StringComparison.Ordinal));
            if (match is not null)
            {
                return match;
            }
        }

        return result.Diagnostics.FirstOrDefault(d => d.IsPrimaryCandidate) ?? result.Diagnostics.FirstOrDefault();
    }

    private static RootCauseCandidate? SelectPrimaryCandidate(AnalysisResult result, DiagnosticRecord? diagnostic)
    {
        if (diagnostic is not null)
        {
            var match = result.RootCauseCandidates.FirstOrDefault(c => string.Equals(c.DiagnosticId, diagnostic.Id, StringComparison.Ordinal));
            if (match is not null)
            {
                return match;
            }
        }

        return result.RootCauseCandidates.FirstOrDefault();
    }


    private static AnalysisNarrativeContract ToContract(AnalysisNarrative narrative) =>
        new(
            Summary: narrative.Summary,
            GroupSummaries: narrative.GroupSummaries,
            RecommendedNextSteps: narrative.RecommendedNextSteps,
            Source: narrative.Source,
            Provider: narrative.Provider,
            Model: narrative.Model,
            Status: narrative.Status,
            FallbackUsed: narrative.FallbackUsed,
            Reason: narrative.Reason);

    private static RootCauseCandidateContract ToContract(RootCauseCandidate candidate) =>
        new(
            Fingerprint: candidate.Fingerprint.Value,
            Title: candidate.Title,
            Explanation: candidate.Explanation,
            Confidence: candidate.Confidence,
            Signals: candidate.Signals,
            LikelyCauses: candidate.LikelyCauses,
            SuggestedFixes: candidate.SuggestedFixes);

    private static DiagnosticGroupContract ToContract(DiagnosticGroup group) =>
        new(
            Fingerprint: group.Fingerprint.Value,
            Count: group.Count,
            GroupReason: group.GroupReason,
            PrimaryDiagnosticId: group.PrimaryDiagnosticId,
            RelatedDiagnosticIds: group.RelatedDiagnosticIds);

    private static DiagnosticContract ToContract(DiagnosticRecord diagnostic) =>
        new(
            Id: diagnostic.Id,
            Tool: ToContractTool(diagnostic.ToolKind),
            Code: diagnostic.Code,
            Severity: ToContractSeverity(diagnostic.Severity),
            Message: diagnostic.Message,
            NormalizedMessage: diagnostic.NormalizedMessage,
            FilePath: diagnostic.Location?.FilePath,
            Line: diagnostic.Location?.Line,
            Column: diagnostic.Location?.Column,
            EndLine: diagnostic.Location?.EndLine,
            EndColumn: diagnostic.Location?.EndColumn,
            Category: ToContractCategory(diagnostic.Category),
            Subcategory: diagnostic.Subcategory,
            Fingerprint: diagnostic.Fingerprint?.Value,
            IsPrimaryCandidate: diagnostic.IsPrimaryCandidate);

    private static MatchedRuleContract ToContract(RuleMatch ruleMatch) =>
        new(
            RuleId: ruleMatch.RuleId,
            TargetType: ruleMatch.TargetType,
            TargetId: ruleMatch.TargetId,
            MatchedConditions: ruleMatch.MatchedConditions,
            AppliedActions: ruleMatch.AppliedActions,
            AppliedAt: ruleMatch.AppliedAt);

    private static ProcessingMetadataContract ToContract(ProcessingMetadata metadata) =>
        new(
            UsedAi: metadata.UsedAi,
            DurationMs: metadata.DurationMs,
            Parser: metadata.Parser,
            CorrelationId: metadata.CorrelationId,
            ToolDetectionConfidence: metadata.ToolDetectionConfidence,
            ParseConfidence: metadata.ParseConfidence,
            UnparsedSegmentCount: metadata.UnparsedSegmentCount,
            Notes: metadata.Notes,
            Ai: metadata.Ai is null ? null : ToContract(metadata.Ai),
            AiTasks: metadata.AiTasks.Select(ToContract).ToArray());

    private static AiProcessingMetadataContract ToContract(AiProcessingMetadata metadata) =>
        new(
            Requested: metadata.Requested,
            Provider: metadata.Provider,
            Model: metadata.Model,
            Status: metadata.Status,
            FallbackUsed: metadata.FallbackUsed,
            Reason: metadata.Reason,
            Feature: metadata.Feature);

    private static IReadOnlyList<string> BuildLikelyCauses(DiagnosticRecord? diagnostic)
    {
        if (diagnostic is null)
        {
            return Array.Empty<string>();
        }

        if (string.Equals(diagnostic.Code, "CS0103", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                "Typo in variable or member name",
                "Missing declaration",
                "Wrong scope or missing using/reference"
            };
        }

        return diagnostic.Category switch
        {
            DiagnosticCategory.MissingSymbol => new[]
            {
                "Typo in referenced symbol",
                "Missing declaration or import",
                "Wrong scope or unavailable project reference"
            },
            DiagnosticCategory.NullableSafety => new[]
            {
                "Member not initialized before constructor exit",
                "Nullability annotation does not match runtime behavior",
                "Required/init pattern missing where expected"
            },
            DiagnosticCategory.Dependency => new[]
            {
                "Package, assembly, or project reference is missing",
                "Namespace or import is incorrect",
                "Restore/build state is out of sync"
            },
            DiagnosticCategory.BuildSystem => new[]
            {
                "Locked file or output path issue",
                "Bad build target or configuration",
                "Stale build artifacts interfering with compilation"
            },
            _ => new[]
            {
                "The earliest high-severity diagnostic is the most likely starting point",
                "A configuration or code issue may be causing downstream noise",
                "This pattern needs a specialized explainer once it repeats"
            }
        };
    }

    private static string ToContractTool(ToolKind toolKind) => toolKind switch
    {
        ToolKind.DotNet => "dotnet",
        ToolKind.TypeScript => "typescript",
        ToolKind.Npm => "npm",
        ToolKind.Vite => "vite",
        ToolKind.Python => "python",
        ToolKind.Generic => "generic",
        _ => "unknown"
    };

    private static string ToContractSeverity(Severity severity) => severity switch
    {
        Severity.Info => "info",
        Severity.Warning => "warning",
        Severity.Error => "error",
        Severity.Fatal => "fatal",
        _ => "unknown"
    };

    private static string ToContractCategory(DiagnosticCategory category) => category switch
    {
        DiagnosticCategory.MissingSymbol => "missing-symbol",
        DiagnosticCategory.TypeMismatch => "type-mismatch",
        DiagnosticCategory.NullableSafety => "nullable-safety",
        DiagnosticCategory.RuntimeEnvironment => "runtime-environment",
        DiagnosticCategory.BuildSystem => "build-system",
        DiagnosticCategory.TestFailure => "test-failure",
        _ => category.ToString().ToLowerInvariant()
    };
}
