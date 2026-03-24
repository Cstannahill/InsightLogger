using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Privacy.DTOs;
using InsightLogger.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace InsightLogger.Infrastructure.Persistence.Repositories;

public sealed class EfCoreAnalysisPrivacyRepository : IAnalysisPrivacyRepository
{
    private readonly InsightLoggerDbContext _dbContext;

    public EfCoreAnalysisPrivacyRepository(InsightLoggerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> PurgeRawContentAsync(string analysisId, CancellationToken cancellationToken = default)
    {
        var analyses = await _dbContext.Analyses
            .Where(x => x.Id == analysisId && x.RawContent != null)
            .ToListAsync(cancellationToken);

        if (analyses.Count == 0)
        {
            return false;
        }

        foreach (var analysis in analyses)
        {
            analysis.RawContent = null;
            analysis.RawContentRedacted = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAnalysisAsync(string analysisId, CancellationToken cancellationToken = default)
    {
        var analyses = await _dbContext.Analyses
            .Where(x => x.Id == analysisId)
            .ToListAsync(cancellationToken);

        if (analyses.Count == 0)
        {
            return false;
        }

        _dbContext.Analyses.RemoveRange(analyses);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RetentionExecutionResultDto> ApplyRetentionAsync(
        int? rawContentRetentionDays,
        int? analysisRetentionDays,
        CancellationToken cancellationToken = default)
    {
        var appliedAtUtc = DateTimeOffset.UtcNow;
        DateTimeOffset? rawContentCutoffUtc = rawContentRetentionDays.HasValue
            ? appliedAtUtc.AddDays(-rawContentRetentionDays.Value)
            : null;
        DateTimeOffset? analysisCutoffUtc = analysisRetentionDays.HasValue
            ? appliedAtUtc.AddDays(-analysisRetentionDays.Value)
            : null;

        var rawContentPurgedCount = 0;
        var analysesDeletedCount = 0;

        if (rawContentCutoffUtc.HasValue)
        {
            var rawContentCandidates = await _dbContext.Analyses
                .Where(x => x.RawContent != null)
                .ToListAsync(cancellationToken);

            var analysesToPurge = rawContentCandidates
                .Where(x => x.CreatedAtUtc < rawContentCutoffUtc.Value)
                .ToList();

            rawContentPurgedCount = analysesToPurge.Count;
            foreach (var analysis in analysesToPurge)
            {
                analysis.RawContent = null;
                analysis.RawContentRedacted = false;
            }
        }

        if (analysisCutoffUtc.HasValue)
        {
            var analysesToDelete = (await _dbContext.Analyses
                    .ToListAsync(cancellationToken))
                .Where(x => x.CreatedAtUtc < analysisCutoffUtc.Value)
                .ToList();

            analysesDeletedCount = analysesToDelete.Count;
            if (analysesToDelete.Count > 0)
            {
                _dbContext.Analyses.RemoveRange(analysesToDelete);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RetentionExecutionResultDto(
            AppliedAtUtc: appliedAtUtc,
            RawContentRetentionDays: rawContentRetentionDays,
            AnalysisRetentionDays: analysisRetentionDays,
            RawContentCutoffUtc: rawContentCutoffUtc,
            AnalysisCutoffUtc: analysisCutoffUtc,
            RawContentPurgedCount: rawContentPurgedCount,
            AnalysesDeletedCount: analysesDeletedCount);
    }
}
