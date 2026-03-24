using System;
using System.Text;
using System.Threading.Tasks;
using InsightLogger.Api.Constants;
using Microsoft.AspNetCore.Http;

namespace InsightLogger.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const int MaxCorrelationIdLength = 128;
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var requestId = string.IsNullOrWhiteSpace(context.TraceIdentifier)
            ? Guid.NewGuid().ToString("n")
            : context.TraceIdentifier;

        var correlationId = ResolveCorrelationId(context, requestId);
        context.Items[ApiHttpContextItems.CorrelationId] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ApiHeaders.CorrelationId] = correlationId;
            context.Response.Headers[ApiHeaders.RequestId] = requestId;
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static string ResolveCorrelationId(HttpContext context, string requestId)
    {
        if (context.Request.Headers.TryGetValue(ApiHeaders.CorrelationId, out var values))
        {
            var headerValue = NormalizeCorrelationId(values.ToString());
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue;
            }
        }

        return requestId;
    }

    private static string NormalizeCorrelationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Math.Min(value.Length, MaxCorrelationIdLength));
        foreach (var ch in value.Trim())
        {
            if (builder.Length >= MaxCorrelationIdLength)
            {
                break;
            }

            if (char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' or ':')
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }
}
