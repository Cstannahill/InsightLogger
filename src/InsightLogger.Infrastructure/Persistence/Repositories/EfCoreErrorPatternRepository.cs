using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Analyses.Persistence;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InsightLogger.Infrastructure.Persistence.Repositories;

public sealed class EfCoreErrorPatternRepository : IErrorPatternRepository
{
    private readonly InsightLoggerDbContext _dbContext;

    public EfCoreErrorPatternRepository(InsightLoggerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpsertFromAnalysisAsync(AnalysisPersistenceRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var diagnosticsWithFingerprints = request.Diagnostics
            .Where(static d => d.Fingerprint is not null)
            .ToList();

        if (diagnosticsWithFingerprints.Count == 0)
        {
            return;
        }

        var fingerprintSet = diagnosticsWithFingerprints
            .Select(d => d.Fingerprint!.Value.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var existingPatterns = await LoadExistingPatternsAsync(fingerprintSet, cancellationToken);

        foreach (var diagnostic in diagnosticsWithFingerprints)
        {
            var fingerprint = diagnostic.Fingerprint!.Value.Value;
            if (!existingPatterns.TryGetValue(fingerprint, out var pattern))
            {
                pattern = new ErrorPatternEntity
                {
                    Fingerprint = fingerprint,
                    Title = SelectTitle(request, fingerprint, diagnostic.Code),
                    CanonicalMessage = diagnostic.NormalizedMessage,
                    ToolKind = diagnostic.ToolKind.ToString(),
                    Category = diagnostic.Category.ToString(),
                    DiagnosticCode = diagnostic.Code,
                    FirstSeenAtUtc = request.CreatedAtUtc,
                    LastSeenAtUtc = request.CreatedAtUtc,
                    OccurrenceCount = 0,
                    LastSuggestedFix = SelectLastSuggestedFix(request, fingerprint)
                };

                await _dbContext.ErrorPatterns.AddAsync(pattern, cancellationToken);
                existingPatterns[fingerprint] = pattern;
            }

            pattern.LastSeenAtUtc = request.CreatedAtUtc;
            pattern.OccurrenceCount += 1;
            pattern.Title ??= SelectTitle(request, fingerprint, diagnostic.Code);
            pattern.CanonicalMessage = diagnostic.NormalizedMessage;
            pattern.ToolKind = diagnostic.ToolKind.ToString();
            pattern.Category = diagnostic.Category.ToString();
            pattern.DiagnosticCode = diagnostic.Code ?? pattern.DiagnosticCode;
            pattern.LastSuggestedFix = SelectLastSuggestedFix(request, fingerprint) ?? pattern.LastSuggestedFix;

            await _dbContext.PatternOccurrences.AddAsync(new PatternOccurrenceEntity
            {
                Id = $"po_{Guid.NewGuid():N}",
                Fingerprint = fingerprint,
                AnalysisId = request.AnalysisId,
                DiagnosticId = diagnostic.Id,
                SeenAtUtc = request.CreatedAtUtc
            }, cancellationToken);
        }
    }

    private static string? SelectTitle(AnalysisPersistenceRequest request, string fingerprint, string? fallbackCode)
    {
        var candidateTitle = request.RootCauseCandidates
            .FirstOrDefault(candidate => string.Equals(candidate.Fingerprint.Value, fingerprint, StringComparison.Ordinal))
            ?.Title;

        if (!string.IsNullOrWhiteSpace(candidateTitle))
        {
            return candidateTitle;
        }

        return string.IsNullOrWhiteSpace(fallbackCode)
            ? "Observed recurring diagnostic pattern"
            : $"Recurring {fallbackCode} diagnostic";
    }

    private static string? SelectLastSuggestedFix(AnalysisPersistenceRequest request, string fingerprint)
    {
        return request.RootCauseCandidates
            .FirstOrDefault(candidate => string.Equals(candidate.Fingerprint.Value, fingerprint, StringComparison.Ordinal))
            ?.SuggestedFixes
            ?.FirstOrDefault();
    }

    private async Task<Dictionary<string, ErrorPatternEntity>> LoadExistingPatternsAsync(
        IReadOnlyCollection<string> fingerprintSet,
        CancellationToken cancellationToken)
    {
        var existingPatterns = _dbContext.ErrorPatterns.Local
            .Where(pattern => fingerprintSet.Contains(pattern.Fingerprint))
            .ToDictionary(pattern => pattern.Fingerprint, StringComparer.Ordinal);

        var missingFingerprints = fingerprintSet
            .Where(fingerprint => !existingPatterns.ContainsKey(fingerprint))
            .ToArray();

        if (missingFingerprints.Length == 0)
        {
            return existingPatterns;
        }

        foreach (var pattern in await _dbContext.ErrorPatterns
                     .Where(pattern => missingFingerprints.Contains(pattern.Fingerprint))
                     .ToListAsync(cancellationToken))
        {
            existingPatterns[pattern.Fingerprint] = pattern;
        }

        return existingPatterns;
    }
}
