using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Analyses.Commands;
using InsightLogger.Application.Analyses.DTOs;
using InsightLogger.Application.Rules.DTOs;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;

namespace InsightLogger.Application.Analyses.Persistence;

public sealed class AnalysisPersistenceService
{
    private readonly IAnalysisPersistenceRepository _analysisPersistenceRepository;
    private readonly IErrorPatternRepository _errorPatternRepository;
    private readonly IRuleRepository _ruleRepository;
    private readonly IInsightLoggerUnitOfWork _unitOfWork;

    public AnalysisPersistenceService(
        IAnalysisPersistenceRepository analysisPersistenceRepository,
        IErrorPatternRepository errorPatternRepository,
        IRuleRepository ruleRepository,
        IInsightLoggerUnitOfWork unitOfWork)
    {
        _analysisPersistenceRepository = analysisPersistenceRepository;
        _errorPatternRepository = errorPatternRepository;
        _ruleRepository = ruleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<(bool Persisted, string? FailureReason)> TryPersistAsync(
        string analysisId,
        AnalyzeInputCommand command,
        ToolKind toolDetected,
        AnalysisSummary summary,
        IReadOnlyList<DiagnosticRecord> diagnostics,
        IReadOnlyList<DiagnosticGroup> groups,
        IReadOnlyList<RootCauseCandidate> rootCauseCandidates,
        IReadOnlyList<RuleMatch> matchedRules,
        AnalysisNarrative? narrative,
        IReadOnlyList<RuleApplicationResult> matchedRuleApplications,
        ProcessingMetadata processing,
        IReadOnlyList<string> warnings,
        CancellationToken cancellationToken = default)
    {
        if (!command.Persist)
        {
            return (true, null);
        }

        try
        {
            var request = BuildRequest(
                analysisId,
                command,
                toolDetected,
                summary,
                diagnostics,
                groups,
                rootCauseCandidates,
                matchedRules,
                narrative,
                processing,
                warnings);

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await _analysisPersistenceRepository.SaveAsync(request, ct);
                await _errorPatternRepository.UpsertFromAnalysisAsync(request, ct);
                await _ruleRepository.RecordMatchesAsync(
                    matchedRuleApplications.Select(static match => match.Rule.Id).ToArray(),
                    request.CreatedAtUtc,
                    ct);
            }, cancellationToken);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"persistence-failure:{ex.GetType().Name}");
        }
    }

    private static AnalysisPersistenceRequest BuildRequest(
        string analysisId,
        AnalyzeInputCommand command,
        ToolKind toolDetected,
        AnalysisSummary summary,
        IReadOnlyList<DiagnosticRecord> diagnostics,
        IReadOnlyList<DiagnosticGroup> groups,
        IReadOnlyList<RootCauseCandidate> rootCauseCandidates,
        IReadOnlyList<RuleMatch> matchedRules,
        AnalysisNarrative? narrative,
        ProcessingMetadata processing,
        IReadOnlyList<string> warnings)
    {
        var createdAtUtc = DateTimeOffset.UtcNow;
        return new AnalysisPersistenceRequest(
            AnalysisId: analysisId,
            InputType: command.InputType,
            ToolDetected: toolDetected,
            Summary: summary,
            Diagnostics: diagnostics,
            Groups: groups,
            RootCauseCandidates: rootCauseCandidates,
            MatchedRules: matchedRules,
            Narrative: narrative,
            Processing: processing,
            Warnings: warnings,
            Context: command.Context,
            ProjectName: TryGetContextValue(command.Context, "projectName"),
            Repository: TryGetContextValue(command.Context, "repository"),
            RawContentHash: ComputeSha256(command.Content),
            RawContent: command.StoreRawContentWhenPersisting ? command.Content : null,
            CreatedAtUtc: createdAtUtc);
    }

    public static PersistedAnalysisDto BuildPersistedAnalysisDto(AnalysisPersistenceRequest request)
        => new(
            AnalysisId: request.AnalysisId,
            InputType: request.InputType,
            ToolDetected: request.ToolDetected,
            CreatedAtUtc: request.CreatedAtUtc,
            Summary: request.Summary,
            RootCauseCandidates: request.RootCauseCandidates,
            Groups: request.Groups,
            Diagnostics: request.Diagnostics,
            MatchedRules: request.MatchedRules,
            Narrative: request.Narrative,
            Processing: request.Processing,
            Warnings: request.Warnings,
            Context: request.Context,
            ProjectName: request.ProjectName,
            Repository: request.Repository,
            RawContentHash: request.RawContentHash,
            RawContent: request.RawContent);

    private static string? TryGetContextValue(IReadOnlyDictionary<string, string>? context, string key)
    {
        if (context is null)
        {
            return null;
        }

        return context.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
