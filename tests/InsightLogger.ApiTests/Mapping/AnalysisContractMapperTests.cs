using System;
using System.Collections.Generic;
using InsightLogger.Api.Mapping;
using InsightLogger.Application.Analyses.DTOs;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;
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
            IncludeProcessingMetadata: false,
            UseAiRootCauseNarrative: false);

        var response = AnalysisContractMapper.ToBuildLogResponse(result, options);

        Assert.Empty(response.Diagnostics);
        Assert.Empty(response.Groups);
        Assert.Null(response.Processing);
        Assert.Single(response.RootCauseCandidates);
        Assert.NotEmpty(response.RootCauseCandidates[0].LikelyCauses);
        Assert.NotNull(response.Narrative);
    }

    [Fact]
    public void ToBuildLogResponse_ShouldProjectAiTaskMetadataAndWarnings()
    {
        var aiTasks = new[]
        {
            new AiProcessingMetadata(
                Requested: true,
                Provider: "ollama",
                Model: "qwen3.5:latest",
                Status: "completed",
                FallbackUsed: false,
                Reason: null,
                Feature: "explanation-enrichment"),
            new AiProcessingMetadata(
                Requested: true,
                Provider: "ollama",
                Model: "qwen3.5:latest",
                Status: "completed",
                FallbackUsed: false,
                Reason: null,
                Feature: "root-cause-narrative")
        };

        var result = CreateAnalysisResult().WithProcessing(
            new ProcessingMetadata(
                UsedAi: true,
                DurationMs: 18,
                Parser: "dotnet-diagnostic-parser-v1",
                CorrelationId: "corr_ai_001",
                ToolDetectionConfidence: 1.0,
                ParseConfidence: 0.98,
                UnparsedSegmentCount: 0,
                Notes: null,
                Ai: null,
                AiTasks: aiTasks));

        result = result.WithNarrative(
            result.Narrative!.WithAi(
                summary: "AI narrative summary.",
                groupSummaries: new[] { "AI group summary." },
                recommendedNextSteps: new[] { "AI next step." },
                provider: "ollama",
                model: "qwen3.5:latest",
                fallbackUsed: false),
            result.Processing,
            new[]
            {
                "AI root-cause narrative generation was requested but could not be completed. Deterministic narrative was returned instead."
            });

        var response = AnalysisContractMapper.ToBuildLogResponse(result);

        Assert.NotNull(response.Narrative);
        Assert.Equal("ai", response.Narrative!.Source);
        Assert.Equal("AI narrative summary.", response.Narrative.Summary);
        Assert.NotNull(response.Processing);
        Assert.Null(response.Processing!.Ai);
        Assert.Equal(2, response.Processing.AiTasks.Count);
        Assert.Contains(response.Processing.AiTasks, task => task.Feature == "explanation-enrichment");
        Assert.Contains(response.Processing.AiTasks, task => task.Feature == "root-cause-narrative");
        Assert.Single(response.Warnings!);
    }


    [Fact]
    public void ToContract_ForPersistedNarrativeDtos_ShouldMapHistoryAndDetailContracts()
    {
        var detailDto = new PersistedAnalysisNarrativeDto(
            AnalysisId: "anl_001",
            InputType: InputType.BuildLog,
            ToolDetected: ToolKind.DotNet,
            CreatedAtUtc: new DateTimeOffset(2026, 03, 24, 6, 0, 0, TimeSpan.Zero),
            Summary: new AnalysisSummary(3, 2, 2, 2, 1),
            Narrative: AnalysisNarrative.Deterministic(
                summary: "The .NET log contains multiple grouped issues.",
                groupSummaries: new[] { "Unknown symbol cluster." },
                recommendedNextSteps: new[] { "Fix the first error." }),
            ProjectName: "InsightLogger.Api",
            Repository: "InsightLogger",
            KnowledgeReferences: new[]
            {
                new InsightLogger.Application.Abstractions.Knowledge.KnowledgeReference(
                    id: "official:dotnet:CS0103",
                    kind: "official-doc",
                    source: "official",
                    title: "Compiler Error CS0103",
                    summary: "Docs.",
                    url: "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0103",
                    tags: new[] { "dotnet", "CS0103" })
            });

        var historyItems = new[]
        {
            new AnalysisNarrativeHistoryItemDto(
                AnalysisId: "anl_001",
                ToolDetected: ToolKind.DotNet,
                CreatedAtUtc: detailDto.CreatedAtUtc,
                Summary: detailDto.Summary,
                SummaryText: detailDto.Narrative.Summary,
                Source: detailDto.Narrative.Source,
                Provider: detailDto.Narrative.Provider,
                Model: detailDto.Narrative.Model,
                Status: detailDto.Narrative.Status,
                FallbackUsed: detailDto.Narrative.FallbackUsed,
                ProjectName: detailDto.ProjectName,
                Repository: detailDto.Repository,
                MatchedFields: new[] { "summary", "recommendedNextSteps" },
                MatchSnippet: "...multiple grouped issues...")
        };

        var history = AnalysisContractMapper.ToContract(historyItems);
        var detail = AnalysisContractMapper.ToContract(detailDto);

        Assert.Single(history.Items);
        Assert.Equal("build-log", detail.InputType);
        Assert.Equal("dotnet", detail.ToolDetected);
        Assert.Equal("InsightLogger.Api", detail.ProjectName);
        Assert.Single(detail.KnowledgeReferences);
        Assert.Equal("deterministic", history.Items[0].Source);
        Assert.Contains("summary", history.Items[0].MatchedFields);
        Assert.NotNull(history.Items[0].MatchSnippet);
    }

    [Fact]
    public void ToContract_ForPersistedAnalysisDto_ShouldMapFullDetailContract()
    {
        var source = CreateAnalysisResult();
        var dto = new PersistedAnalysisDto(
            AnalysisId: source.AnalysisId,
            InputType: source.InputType,
            ToolDetected: source.ToolDetected,
            CreatedAtUtc: new DateTimeOffset(2026, 03, 24, 7, 30, 0, TimeSpan.Zero),
            Summary: source.Summary,
            RootCauseCandidates: source.RootCauseCandidates,
            Groups: source.Groups,
            Diagnostics: source.Diagnostics,
            MatchedRules: new[]
            {
                new RuleMatch(
                    RuleId: "rule_001",
                    TargetType: "candidate",
                    TargetId: source.RootCauseCandidates[0].Fingerprint.Value,
                    MatchedConditions: new[] { "tool", "code" },
                    AppliedActions: new[] { "explanation", "suggested-fixes" },
                    AppliedAt: new DateTimeOffset(2026, 03, 24, 7, 30, 0, TimeSpan.Zero))
            },
            Narrative: source.Narrative,
            Processing: source.Processing,
            Warnings: new[] { "deterministic fallback retained for a secondary item" },
            Context: new Dictionary<string, string>
            {
                ["projectName"] = "InsightLogger.Api",
                ["repository"] = "InsightLogger"
            },
            ProjectName: "InsightLogger.Api",
            Repository: "InsightLogger",
            RawContentHash: "hash_123",
            RawContentRedacted: false,
            RawContent: null,
            KnowledgeReferences: new[]
            {
                new InsightLogger.Application.Abstractions.Knowledge.KnowledgeReference(
                    id: "internal:pattern:fp_cs0103_name_missing",
                    kind: "recurring-pattern",
                    source: "internal",
                    title: "Known recurring pattern",
                    summary: "Observed before.",
                    resourceType: "error-pattern",
                    resourceId: "fp_cs0103_name_missing",
                    tags: new[] { "dotnet", "CS0103" })
            });

        var contract = AnalysisContractMapper.ToContract(dto);

        Assert.Equal(dto.AnalysisId, contract.AnalysisId);
        Assert.Equal("build-log", contract.InputType);
        Assert.Equal("dotnet", contract.ToolDetected);
        Assert.Equal(dto.Diagnostics.Count, contract.Diagnostics.Count);
        Assert.Equal(dto.Groups.Count, contract.Groups.Count);
        Assert.Equal(dto.RootCauseCandidates.Count, contract.RootCauseCandidates.Count);
        Assert.Single(contract.MatchedRules);
        Assert.Equal("hash_123", contract.RawContentHash);
        Assert.False(contract.RawContentStored);
        Assert.False(contract.RawContentRedacted);
        Assert.NotNull(contract.Context);
        Assert.Equal("InsightLogger.Api", contract.ProjectName);
        Assert.Single(contract.KnowledgeReferences);
    }

    [Fact]
    public void ToCompilerErrorResponse_ShouldUsePrimaryDiagnosticAndPrimaryCandidate()
    {
        var result = CreateSingleDiagnosticAnalysisResult();

        var response = AnalysisContractMapper.ToCompilerErrorResponse(result);

        Assert.Equal("fp_cs0103_name_missing", response.Fingerprint);
        Assert.NotNull(response.Diagnostic);
        Assert.Equal("CS0103", response.Diagnostic!.Code);
        Assert.Equal("dotnet", response.ToolDetected);
        Assert.NotEmpty(response.LikelyCauses);
        Assert.NotEmpty(response.SuggestedFixes);
        Assert.Equal("Typo in variable or member name", response.LikelyCauses[0]);
        Assert.NotNull(response.KnowledgeReferences);
    }

    [Fact]
    public void ToCommand_ForBuildLog_ShouldFlattenContext_And_MapSeparateNarrativeToggle()
    {
        var request = new AnalyzeBuildLogRequest(
            Tool: "dotnet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
            ProjectName: "InsightLogger.Api",
            Repository: "InsightLogger",
            Branch: "main",
            CommitSha: "abc123",
            Environment: new AnalyzeEnvironmentContract(Os: "windows", Ci: false, MachineName: "DEVBOX"),
            Options: new AnalyzeRequestOptionsContract(
                Persist: false,
                UseAiEnrichment: true,
                IncludeRawDiagnostics: true,
                IncludeGroups: true,
                IncludeProcessingMetadata: true,
                UseAiRootCauseNarrative: true,
                PersistRawContent: true));

        var command = AnalysisContractMapper.ToCommand(request, "corr_demo_001");

        Assert.Equal(InputType.BuildLog, command.InputType);
        Assert.Equal(ToolKind.DotNet, command.ToolHint);
        Assert.Equal("InsightLogger.Api", command.Context!["projectName"]);
        Assert.Equal("windows", command.Context["environment.os"]);
        Assert.Equal("false", command.Context["environment.ci"]);
        Assert.True(command.UseAiEnrichment);
        Assert.True(command.UseAiRootCauseNarrative);
        Assert.True(command.StoreRawContentWhenPersisting);
    }

    private static AnalysisResult CreateAnalysisResult()
    {
        var fingerprint = new DiagnosticFingerprint("fp_cs0103_name_missing");
        var firstDiagnostic = new DiagnosticRecord(
            id: "diag_1",
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

        var secondDiagnostic = new DiagnosticRecord(
            id: "diag_2",
            toolKind: ToolKind.DotNet,
            severity: Severity.Error,
            message: "The name 'servicez' does not exist in the current context",
            rawSnippet: "Program.cs(15,9): error CS0103: The name 'servicez' does not exist in the current context",
            code: "CS0103",
            normalizedMessage: "The name '{identifier}' does not exist in the current context",
            location: new DiagnosticLocation("Program.cs", 15, 9),
            category: DiagnosticCategory.MissingSymbol,
            isPrimaryCandidate: false,
            fingerprint: fingerprint);

        var group = new DiagnosticGroup(
            fingerprint: fingerprint,
            primaryDiagnosticId: firstDiagnostic.Id,
            relatedDiagnosticIds: new[] { firstDiagnostic.Id, secondDiagnostic.Id },
            groupReason: "same-fingerprint");

        var candidate = new RootCauseCandidate(
            Fingerprint: fingerprint,
            Title: "Unknown symbol in current context",
            Explanation: "The compiler cannot resolve a referenced name in the current scope.",
            Confidence: 0.96,
            Signals: new[] { "diagnostic-code:CS0103", "category:missing-symbol" },
            LikelyCauses: new[]
            {
                "Typo in variable or member name",
                "Missing declaration",
                "Wrong scope or missing using/reference"
            },
            SuggestedFixes: new[]
            {
                "Check the symbol spelling.",
                "Verify the symbol is declared before use."
            },
            DiagnosticId: firstDiagnostic.Id,
            GroupId: fingerprint.Value);

        return new AnalysisResult(
            inputType: InputType.BuildLog,
            toolDetected: ToolKind.DotNet,
            summary: new AnalysisSummary(2, 1, 1, 2, 0),
            diagnostics: new[] { firstDiagnostic, secondDiagnostic },
            groups: new[] { group },
            rootCauseCandidates: new[] { candidate },
            narrative: AnalysisNarrative.Deterministic(
                summary: "The .NET log contains 2 diagnostics grouped into 1 likely issue cluster.",
                groupSummaries: new[] { "Unknown symbol in current context: 2 related diagnostics matched fingerprint fp_cs0103_name_missing." },
                recommendedNextSteps: new[] { "Check the symbol spelling.", "Verify the symbol is declared before use." }),
            processing: new ProcessingMetadata(false, 12, "dotnet-diagnostic-parser-v1", "corr_demo_001", 1.0, 0.98, 0, null));
    }

    private static AnalysisResult CreateSingleDiagnosticAnalysisResult()
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
            LikelyCauses: new[]
            {
                "Typo in variable or member name",
                "Missing declaration",
                "Wrong scope or missing using/reference"
            },
            SuggestedFixes: new[]
            {
                "Check the symbol spelling.",
                "Verify the symbol is declared before use."
            },
            DiagnosticId: diagnostic.Id,
            GroupId: fingerprint.Value);

        return new AnalysisResult(
            inputType: InputType.SingleDiagnostic,
            toolDetected: ToolKind.DotNet,
            summary: new AnalysisSummary(1, 1, 1, 1, 0),
            diagnostics: new[] { diagnostic },
            groups: new[] { group },
            rootCauseCandidates: new[] { candidate },
            processing: new ProcessingMetadata(false, 12, "dotnet-diagnostic-parser-v1", "corr_demo_001", 1.0, 0.98, 0, null));
    }
}



