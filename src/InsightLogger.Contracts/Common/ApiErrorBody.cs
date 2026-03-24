using System.Collections.Generic;

namespace InsightLogger.Contracts.Common;

public sealed record ApiErrorBody(
    string Code,
    string Message,
    IReadOnlyList<ValidationErrorDetail>? Details = null,
    string? CorrelationId = null);
