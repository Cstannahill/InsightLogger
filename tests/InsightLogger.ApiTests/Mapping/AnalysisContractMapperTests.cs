using System;
using System.Collections.Generic;
using InsightLogger.Api.Mapping;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using Xunit;

namespace InsightLogger.ApiTests.Mapping;

public sealed class AnalysisContractMapperTests
{
    [Fact]
    public void ToBuildLogResponse_ShouldRespectProjectionOptions()
    {
        var result = CreateAnalysisResult();
        var options = new AnalyzeRequestOptionsContract(
            Persist: false,
            UseAiEnrichment: false,
            IncludeRawDiagnostics: false,
            IncludeGroups: false,
            IncludeProcessingMetadata: false);

        var response = AnalysisContractMapper.ToBuildLogResponse(result, options);

        Assert.Empty(response.Diagnostics);
        Assert.Empty(response.Groups);
        Assert.Null(response.Processing);
        Assert.Single(response.RootCauseCandidates);
    }

    [Fact]
    public void ToCompilerErrorResponse_ShouldUsePrimaryDiagnosticAndPrimaryCandidate()
    {
        var result = CreateAnalysisResult();

        var response = AnalysisContractMapper.ToCompilerErrorResponse(result);

        Assert.Equal("fp_cs0103_name_missing", response.Fingerprint);
        Assert.NotNull(response.Diagnostic);
        Assert.Equal("CS0103", response.Diagnostic!.Code);
        Assert.Equal("dotnet", response.ToolDetected);
        Assert.NotEmpty(response.LikelyCauses);
        Assert.NotEmpty(response.SuggestedFixes);
    }

    [Fact]
    public void ToCommand_ForBuildLog_ShouldFlattenContext()
    {
        var request = new AnalyzeBuildLogRequest(
            Tool: "dotnet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            ProjectName: "InsightLogger.Api",
            Repository: "InsightLogger",
            Branch: "main",
            CommitSha: "abc123",
            Environment: new AnalyzeEnvironmentContract(Os: "windows", Ci: false, MachineName: "DEVBOX"));

        var command = AnalysisContractMapper.ToCommand(request, "corr_demo_001");

        Assert.Equal(InputType.BuildLog, command.InputType);
        Assert.Equal(ToolKind.DotNet, command.ToolHint);
        Assert.Equal("InsightLogger.Api", command.Context!["projectName"]);
        Assert.Equal("windows", command.Context["environment.os"]);
        Assert.Equal("false", command.Context["environment.ci"]);
    }

    private static AnalysisResult CreateAnalysisResult()
    {
        var fingerprint = new DiagnosticFingerprint("fp_cs0103_name_missing");
        var diagnostic = new DiagnosticRecord(
            toolKind: ToolKind.DotNet,
            severity: Severity.Error,
            message: "The name 'builderz' does not exist in the current context",
            rawSnippet: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            code: "CS0103",
            normalizedMessage: "The name '{identifier}' does not exist in the current context",
            location: new DiagnosticLocation("Program.cs", 14, 9),
            category: DiagnosticCategory.MissingSymbol,
            isPrimaryCandidate: true,
            fingerprint: fingerprint);

        var group = new DiagnosticGroup(
            fingerprint: fingerprint,
            primaryDiagnosticId: diagnostic.Id,
            relatedDiagnosticIds: new[] { diagnostic.Id },
            groupReason: "single-primary-diagnostic");

        var candidate = new RootCauseCandidate(
            Fingerprint: fingerprint,
            Title: "Unknown symbol in current context",
            Explanation: "The compiler cannot resolve a referenced name in the current scope.",
            Confidence: 0.96,
            Signals: new[] { "diagnostic-code:CS0103", "category:missing-symbol" },
            SuggestedFixes: new[]
            {
                "Check the symbol spelling.",
                "Verify the symbol is declared before use."
            },
            DiagnosticId: diagnostic.Id,
            GroupId: fingerprint.Value);

        return new AnalysisResult(
            inputType: InputType.BuildLog,
            toolDetected: ToolKind.DotNet,
            summary: new AnalysisSummary(1, 1, 1, 1, 0),
            diagnostics: new[] { diagnostic },
            groups: new[] { group },
            rootCauseCandidates: new[] { candidate },
            processing: new ProcessingMetadata(false, 12, "dotnet-diagnostic-parser-v1", "corr_demo_001", 1.0, 0.98, 0, null));
    }
}
