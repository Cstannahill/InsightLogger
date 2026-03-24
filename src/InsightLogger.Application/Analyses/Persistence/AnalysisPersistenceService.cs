using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Analyses.Commands;
using InsightLogger.Application.Rules.DTOs;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;

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
        IReadOnlyList<RuleApplicationResult> matchedRules,
        ProcessingMetadata processing,
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
                processing);

            await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await _analysisPersistenceRepository.SaveAsync(request, ct);
                await _errorPatternRepository.UpsertFromAnalysisAsync(request, ct);
                await _ruleRepository.RecordMatchesAsync(
                    matchedRules.Select(static match => match.Rule.Id).ToArray(),
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
        ProcessingMetadata processing)
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
            Processing: processing,
            Context: command.Context,
            RawContentHash: ComputeSha256(command.Content),
            RawContent: command.StoreRawContentWhenPersisting ? command.Content : null,
            CreatedAtUtc: createdAtUtc);
    }

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
