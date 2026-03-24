using System;
using System.Collections.Generic;
using InsightLogger.Contracts.Common;

namespace InsightLogger.Api.Exceptions;

public class ApiException : Exception
{
    public ApiException(
        int statusCode,
        string errorCode,
        string message,
        IReadOnlyList<ValidationErrorDetail>? details = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Details = details;
    }

    public int StatusCode { get; }

    public string ErrorCode { get; }

    public IReadOnlyList<ValidationErrorDetail>? Details { get; }
}
