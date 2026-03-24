using System.Collections.Generic;
using System.Diagnostics;
using InsightLogger.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InsightLogger.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
        var method = context.Request.Method;
        var endpointName = context.GetEndpoint()?.DisplayName;

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["correlationId"] = context.GetCorrelationId(),
            ["requestId"] = context.GetRequestId(),
            ["traceId"] = context.GetTraceId(),
            ["spanId"] = context.GetSpanId(),
            ["httpMethod"] = method,
            ["requestPath"] = path,
            ["endpoint"] = endpointName
        });

        _logger.LogInformation(
            "HTTP request started. Method={Method} Path={Path} Endpoint={Endpoint}.",
            method,
            path,
            endpointName);

        try
        {
            await _next(context);
            stopwatch.Stop();

            _logger.LogInformation(
                "HTTP request completed. Method={Method} Path={Path} StatusCode={StatusCode} DurationMs={DurationMs}.",
                method,
                path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        catch
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "HTTP request failed before completion. Method={Method} Path={Path} DurationMs={DurationMs}.",
                method,
                path,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
