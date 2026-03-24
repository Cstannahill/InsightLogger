using InsightLogger.Api.Mapping;
using InsightLogger.Application.Diagnostics.Queries;
using InsightLogger.Application.Patterns.Queries;
using InsightLogger.Contracts.Common;
using InsightLogger.Contracts.Diagnostics;
using InsightLogger.Contracts.Patterns;
using InsightLogger.Domain.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InsightLogger.Api.Endpoints;

public static class PatternEndpoints
{
    public static IEndpointRouteBuilder MapPatternEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var diagnosticsGroup = endpoints.MapGroup("/errors").WithTags("Diagnostics");
        var patternsGroup = endpoints.MapGroup("/patterns").WithTags("Patterns");

        diagnosticsGroup.MapGet("/{fingerprint}", HandleGetByFingerprintAsync)
            .WithName("GetErrorByFingerprint")
            .Produces<GetErrorByFingerprintResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Look up a known recurring error pattern by fingerprint.");

        patternsGroup.MapGet("/top", HandleGetTopPatternsAsync)
            .WithName("GetTopPatterns")
            .Produces<GetTopPatternsResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("List the top recurring error patterns.");

        return endpoints;
    }

    private static async Task<IResult> HandleGetByFingerprintAsync(
        string fingerprint,
        HttpContext httpContext,
        IErrorFingerprintQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            return TypedResults.BadRequest(CreateError(
                code: "validation_failed",
                message: "Fingerprint is required.",
                correlationId: ResolveCorrelationId(httpContext),
                details: new[] { new ValidationErrorDetail("fingerprint", "Fingerprint is required.") }));
        }

        var query = new GetErrorByFingerprintQuery(fingerprint);
        var result = await queryService.GetByFingerprintAsync(query, cancellationToken);

        if (result is null)
        {
            return TypedResults.NotFound(CreateError(
                code: "not_found",
                message: $"No known error pattern exists for fingerprint '{fingerprint}'.",
                correlationId: ResolveCorrelationId(httpContext)));
        }

        return TypedResults.Ok(PatternContractMapper.ToContract(result));
    }

    private static async Task<IResult> HandleGetTopPatternsAsync(
        HttpContext httpContext,
        IPatternQueryService queryService,
        string? tool = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (limit is <= 0 or > 100)
        {
            return TypedResults.BadRequest(CreateError(
                code: "validation_failed",
                message: "Limit must be between 1 and 100.",
                correlationId: ResolveCorrelationId(httpContext),
                details: new[] { new ValidationErrorDetail("limit", "Limit must be between 1 and 100.") }));
        }

        ToolKind? toolKind = null;
        if (!string.IsNullOrWhiteSpace(tool))
        {
            if (!Enum.TryParse<ToolKind>(tool, ignoreCase: true, out var parsedToolKind))
            {
                return TypedResults.BadRequest(CreateError(
                    code: "validation_failed",
                    message: "Tool must be a supported value.",
                    correlationId: ResolveCorrelationId(httpContext),
                    details: new[] { new ValidationErrorDetail("tool", "Tool must be a supported value.") }));
            }

            toolKind = parsedToolKind;
        }

        var query = new GetTopPatternsQuery(toolKind, limit);
        var result = await queryService.GetTopPatternsAsync(query, cancellationToken);
        return TypedResults.Ok(PatternContractMapper.ToContract(result));
    }

    private static ApiErrorResponse CreateError(
        string code,
        string message,
        string? correlationId,
        IReadOnlyList<ValidationErrorDetail>? details = null)
    {
        return new ApiErrorResponse(new ApiErrorBody(
            Code: code,
            Message: message,
            Details: details,
            CorrelationId: correlationId));
    }

    private static string ResolveCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var headerValue) &&
            !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        return httpContext.TraceIdentifier;
    }
}
