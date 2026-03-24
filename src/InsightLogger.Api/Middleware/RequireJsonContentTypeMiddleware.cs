using System;
using System.Threading.Tasks;
using InsightLogger.Api.Constants;
using InsightLogger.Api.Exceptions;
using Microsoft.AspNetCore.Http;

namespace InsightLogger.Api.Middleware;

public sealed class RequireJsonContentTypeMiddleware
{
    private readonly RequestDelegate _next;

    public RequireJsonContentTypeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (RequiresJsonBody(context) && !context.Request.HasJsonContentType())
        {
            throw new ApiException(
                StatusCodes.Status415UnsupportedMediaType,
                ApiErrorCodes.UnsupportedMediaType,
                "Content-Type must be application/json.");
        }

        await _next(context);
    }

    private static bool RequiresJsonBody(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method)
        && context.Request.Path.StartsWithSegments("/analyze", StringComparison.OrdinalIgnoreCase);
}
