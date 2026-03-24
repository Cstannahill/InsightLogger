using System;
using InsightLogger.Api.Constants;
using Microsoft.AspNetCore.Http;

namespace InsightLogger.Api.Extensions;

public static class HttpContextExtensions
{
    public static string? GetCorrelationId(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (httpContext.Items.TryGetValue(ApiHttpContextItems.CorrelationId, out var value) && value is string correlationId && !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        return string.IsNullOrWhiteSpace(httpContext.TraceIdentifier)
            ? null
            : httpContext.TraceIdentifier;
    }
}
