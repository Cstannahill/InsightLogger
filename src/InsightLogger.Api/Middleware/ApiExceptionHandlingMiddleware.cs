using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InsightLogger.Api.Constants;
using InsightLogger.Api.Exceptions;
using InsightLogger.Api.Extensions;
using InsightLogger.Api.Results;
using InsightLogger.Application.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InsightLogger.Api.Middleware;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException apiException)
        {
            await WriteApiExceptionAsync(context, apiException);
        }
        catch (BadHttpRequestException badRequestException)
        {
            await WriteBadHttpRequestAsync(context, badRequestException);
        }
        catch (Exception exception)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["correlationId"] = context.GetCorrelationId(),
                ["requestId"] = context.GetRequestId(),
                ["traceId"] = context.GetTraceId(),
                ["spanId"] = context.GetSpanId(),
                ["httpMethod"] = context.Request.Method,
                ["requestPath"] = context.Request.Path.Value
            });

            _logger.LogError(
                "Unhandled exception while processing request. Method={Method} Path={Path} ExceptionType={ExceptionType} ExceptionMessage={ExceptionMessage}.",
                context.Request.Method,
                context.Request.Path.Value,
                exception.GetType().Name,
                LogRedactor.Redact(exception.Message));

            await WriteInternalErrorAsync(context, exception);
        }
    }

    private static async Task WriteApiExceptionAsync(HttpContext context, ApiException exception)
    {
        var payload = ApiErrorResultFactory.Create(
            exception.ErrorCode,
            exception.Message,
            context.GetCorrelationId(),
            exception.Details);

        context.Response.StatusCode = exception.StatusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(payload);
    }

    private static async Task WriteBadHttpRequestAsync(HttpContext context, BadHttpRequestException exception)
    {
        var statusCode = exception.StatusCode is >= 400 and < 600
            ? exception.StatusCode
            : StatusCodes.Status400BadRequest;

        var code = statusCode == StatusCodes.Status415UnsupportedMediaType
            ? ApiErrorCodes.UnsupportedMediaType
            : ApiErrorCodes.ValidationFailed;

        var message = statusCode == StatusCodes.Status415UnsupportedMediaType
            ? "Content-Type must be application/json."
            : "The request body could not be read as a valid JSON request.";

        var payload = ApiErrorResultFactory.Create(code, message, context.GetCorrelationId());

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(payload);
    }

    private static async Task WriteInternalErrorAsync(HttpContext context, Exception _)
    {
        var payload = ApiErrorResultFactory.Create(
            ApiErrorCodes.InternalError,
            "An unexpected error occurred while processing the request.",
            context.GetCorrelationId());

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(payload);
    }
}
