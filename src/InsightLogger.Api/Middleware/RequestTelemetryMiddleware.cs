using System.Diagnostics;
using InsightLogger.Api.Extensions;
using InsightLogger.Application.Abstractions.Telemetry;
using Microsoft.AspNetCore.Routing;

namespace InsightLogger.Api.Middleware;

public sealed class RequestTelemetryMiddleware
{
    private readonly RequestDelegate _next;

    public RequestTelemetryMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IInsightLoggerTelemetry telemetry)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            telemetry.RecordHttpRequest(new HttpRequestTelemetryEvent(
                Method: context.Request.Method,
                Route: ResolveRoute(context),
                StatusCode: context.Response.StatusCode,
                DurationMs: (int)Math.Max(1, stopwatch.ElapsedMilliseconds),
                CorrelationId: context.GetCorrelationId()));
        }
    }

    private static string ResolveRoute(HttpContext context)
    {
        if (context.GetEndpoint() is RouteEndpoint routeEndpoint && !string.IsNullOrWhiteSpace(routeEndpoint.RoutePattern.RawText))
        {
            return routeEndpoint.RoutePattern.RawText!;
        }

        return string.IsNullOrWhiteSpace(context.Request.Path.Value)
            ? "/"
            : context.Request.Path.Value!;
    }
}
