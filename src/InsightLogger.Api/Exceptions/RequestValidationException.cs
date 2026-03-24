using System.Collections.Generic;
using InsightLogger.Contracts.Common;

namespace InsightLogger.Api.Exceptions;

public sealed class RequestValidationException : ApiException
{
    public RequestValidationException(
        int statusCode,
        string errorCode,
        string message,
        IReadOnlyList<ValidationErrorDetail> details)
        : base(statusCode, errorCode, message, details)
    {
    }
}
