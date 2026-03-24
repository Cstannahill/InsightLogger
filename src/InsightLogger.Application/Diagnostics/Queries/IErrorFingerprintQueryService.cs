using InsightLogger.Application.Diagnostics.DTOs;

namespace InsightLogger.Application.Diagnostics.Queries;

public interface IErrorFingerprintQueryService
{
    Task<ErrorFingerprintDetailsDto?> GetByFingerprintAsync(
        GetErrorByFingerprintQuery query,
        CancellationToken cancellationToken = default);
}
