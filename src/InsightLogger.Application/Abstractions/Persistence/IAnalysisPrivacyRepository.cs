using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Privacy.DTOs;

namespace InsightLogger.Application.Abstractions.Persistence;

public interface IAnalysisPrivacyRepository
{
    Task<bool> PurgeRawContentAsync(string analysisId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAnalysisAsync(string analysisId, CancellationToken cancellationToken = default);

    Task<RetentionExecutionResultDto> ApplyRetentionAsync(
        int? rawContentRetentionDays,
        int? analysisRetentionDays,
        CancellationToken cancellationToken = default);
}
