using System.Collections.Generic;
using InsightLogger.Api.Constants;
using InsightLogger.Contracts.Common;

namespace InsightLogger.Api.Results;

public static class ApiErrorResultFactory
{
    public static ApiErrorResponse ValidationFailed(IReadOnlyList<ValidationErrorDetail> details, string? correlationId) =>
        Create(
            ApiErrorCodes.ValidationFailed,
            "One or more validation errors occurred.",
            correlationId,
            details);

    public static ApiErrorResponse Create(
        string code,
        string message,
        string? correlationId,
        IReadOnlyList<ValidationErrorDetail>? details = null) =>
        new(new ApiErrorBody(
            Code: code,
            Message: message,
            Details: details,
            CorrelationId: correlationId));
}
