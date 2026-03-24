using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Privacy.DTOs;

namespace InsightLogger.Application.Privacy.Services;

public interface IPrivacyControlService
{
    Task<PrivacySettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<RetentionExecutionResultDto> ApplyRetentionAsync(CancellationToken cancellationToken = default);

    Task<bool> PurgeRawContentAsync(string analysisId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAnalysisAsync(string analysisId, CancellationToken cancellationToken = default);
}
