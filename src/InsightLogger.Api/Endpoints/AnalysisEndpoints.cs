using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Api.Extensions;
using InsightLogger.Api.Filters;
using InsightLogger.Api.Mapping;
using InsightLogger.Application.Analyses.Queries;
using InsightLogger.Application.Analyses.Services;
using InsightLogger.Application.Privacy.Services;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Contracts.Common;
using InsightLogger.Domain.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InsightLogger.Api.Endpoints;

public static class AnalysisEndpoints
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var analyzeGroup = endpoints.MapGroup("/analyze").WithTags("Analysis");
        var analysesGroup = endpoints.MapGroup("/analyses").WithTags("Analysis History");

        analyzeGroup.MapPost("/build-log", HandleBuildLog)
            .AddEndpointFilter<ValidationEndpointFilter<AnalyzeBuildLogRequest>>()
            .WithName("AnalyzeBuildLog")
            .Accepts<AnalyzeBuildLogRequest>("application/json")
            .Produces<AnalyzeBuildLogResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status413PayloadTooLarge)
            .Produces<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Analyze a full build or tool log.");

        analyzeGroup.MapPost("/compiler-error", HandleCompilerError)
            .AddEndpointFilter<ValidationEndpointFilter<AnalyzeCompilerErrorRequest>>()
            .WithName("AnalyzeCompilerError")
            .Accepts<AnalyzeCompilerErrorRequest>("application/json")
            .Produces<AnalyzeCompilerErrorResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status413PayloadTooLarge)
            .Produces<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Analyze a single compiler/runtime diagnostic or compact error block.");

        analysesGroup.MapGet("/narratives", HandleGetNarratives)
            .WithName("GetAnalysisNarratives")
            .Produces<GetAnalysisNarrativesResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("List persisted build-log narratives from prior analyses.");

        analysesGroup.MapGet("/{analysisId}", HandleGetAnalysis)
            .WithName("GetAnalysis")
            .Produces<GetAnalysisResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Get a full persisted analysis by analysis id.");

        analysesGroup.MapGet("/{analysisId}/narrative", HandleGetNarrative)
            .WithName("GetAnalysisNarrative")
            .Produces<GetAnalysisNarrativeResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Get a persisted grouped diagnostic narrative by analysis id.");

        analysesGroup.MapDelete("/{analysisId}/raw-content", HandleDeleteRawContent)
            .WithName("DeleteAnalysisRawContent")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Purge stored raw content for one persisted analysis.");

        analysesGroup.MapDelete("/{analysisId}", HandleDeleteAnalysis)
            .WithName("DeleteAnalysis")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Delete one persisted analysis and its related history rows.");

        return endpoints;
    }

    private static async Task<IResult> HandleBuildLog(
        AnalyzeBuildLogRequest request,
        HttpContext httpContext,
        IAnalysisService analysisService,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.GetCorrelationId();
        var command = AnalysisContractMapper.ToCommand(request, correlationId);
        var result = await analysisService.AnalyzeAsync(command, cancellationToken);
        var response = AnalysisContractMapper.ToBuildLogResponse(result, request.Options);
        return TypedResults.Ok(response);
    }

    private static async Task<IResult> HandleCompilerError(
        AnalyzeCompilerErrorRequest request,
        HttpContext httpContext,
        IAnalysisService analysisService,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.GetCorrelationId();
        var command = AnalysisContractMapper.ToCommand(request, correlationId);
        var result = await analysisService.AnalyzeAsync(command, cancellationToken);
        var response = AnalysisContractMapper.ToCompilerErrorResponse(result, request.Options);
        return TypedResults.Ok(response);
    }

    private static async Task<IResult> HandleGetNarratives(
        HttpContext httpContext,
        IAnalysisNarrativeQueryService queryService,
        string? tool = null,
        string? source = null,
        string? projectName = null,
        string? repository = null,
        string? text = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (limit is <= 0 or > 100)
        {
            return TypedResults.BadRequest(CreateError(
                code: "validation_failed",
                message: "Limit must be between 1 and 100.",
                correlationId: ResolveCorrelationId(httpContext),
                details: [new ValidationErrorDetail("limit", "Limit must be between 1 and 100.")]));
        }

        ToolKind? toolKind = null;
        if (!string.IsNullOrWhiteSpace(tool))
        {
            if (!AnalysisContractMapper.TryParseTool(tool, out var parsedToolKind))
            {
                return TypedResults.BadRequest(CreateError(
                    code: "validation_failed",
                    message: "Tool must be a supported value.",
                    correlationId: ResolveCorrelationId(httpContext),
                    details: [new ValidationErrorDetail("tool", "Tool must be a supported value.")]));
            }

            toolKind = parsedToolKind;
        }

        if (!string.IsNullOrWhiteSpace(source) &&
            !string.Equals(source, "deterministic", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(source, "ai", StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.BadRequest(CreateError(
                code: "validation_failed",
                message: "Source must be either 'deterministic' or 'ai'.",
                correlationId: ResolveCorrelationId(httpContext),
                details: [new ValidationErrorDetail("source", "Source must be either 'deterministic' or 'ai'.")]));
        }

        if (!string.IsNullOrWhiteSpace(text) && text.Trim().Length > 200)
        {
            return TypedResults.BadRequest(CreateError(
                code: "validation_failed",
                message: "Text must be 200 characters or fewer.",
                correlationId: ResolveCorrelationId(httpContext),
                details: [new ValidationErrorDetail("text", "Text must be 200 characters or fewer.")]));
        }

        var result = await queryService.GetRecentAsync(
            new GetAnalysisNarrativesQuery(
                ToolKind: toolKind,
                Source: source,
                ProjectName: projectName,
                Repository: repository,
                Text: text,
                Limit: limit),
            cancellationToken);

        return TypedResults.Ok(AnalysisContractMapper.ToContract(result));
    }

    private static async Task<IResult> HandleGetAnalysis(
        string analysisId,
        HttpContext httpContext,
        IAnalysisQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(analysisId))
        {
            return TypedResults.BadRequest(CreateError(
                code: "validation_failed",
                message: "Analysis id is required.",
                correlationId: ResolveCorrelationId(httpContext),
                details: [new ValidationErrorDetail("analysisId", "Analysis id is required.")]));
        }

        var result = await queryService.GetByAnalysisIdAsync(new GetAnalysisByIdQuery(analysisId), cancellationToken);
        if (result is null)
        {
            return TypedResults.NotFound(CreateError(
                code: "not_found",
                message: $"No persisted analysis exists for analysis '{analysisId}'.",
                correlationId: ResolveCorrelationId(httpContext)));
        }

        return TypedResults.Ok(AnalysisContractMapper.ToContract(result));
    }

    private static async Task<IResult> HandleGetNarrative(
        string analysisId,
        HttpContext httpContext,
        IAnalysisNarrativeQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(analysisId))
        {
            return TypedResults.BadRequest(CreateError(
                code: "validation_failed",
                message: "Analysis id is required.",
                correlationId: ResolveCorrelationId(httpContext),
                details: [new ValidationErrorDetail("analysisId", "Analysis id is required.")]));
        }

        var result = await queryService.GetByAnalysisIdAsync(new GetAnalysisNarrativeQuery(analysisId), cancellationToken);
        if (result is null)
        {
            return TypedResults.NotFound(CreateError(
                code: "not_found",
                message: $"No persisted narrative exists for analysis '{analysisId}'.",
                correlationId: ResolveCorrelationId(httpContext)));
        }

        return TypedResults.Ok(AnalysisContractMapper.ToContract(result));
    }

    private static async Task<IResult> HandleDeleteRawContent(
        string analysisId,
        HttpContext httpContext,
        IPrivacyControlService privacyControlService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(analysisId))
        {
            return TypedResults.BadRequest(CreateError(
                code: "validation_failed",
                message: "Analysis id is required.",
                correlationId: ResolveCorrelationId(httpContext),
                details: [new ValidationErrorDetail("analysisId", "Analysis id is required.")]));
        }

        var deleted = await privacyControlService.PurgeRawContentAsync(analysisId, cancellationToken);
        if (deleted)
        {
            return TypedResults.NoContent();
        }

        return TypedResults.NotFound(CreateError(
            code: "not_found",
            message: $"No stored raw content exists for analysis '{analysisId}'.",
            correlationId: ResolveCorrelationId(httpContext)));
    }

    private static async Task<IResult> HandleDeleteAnalysis(
        string analysisId,
        HttpContext httpContext,
        IPrivacyControlService privacyControlService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(analysisId))
        {
            return TypedResults.BadRequest(CreateError(
                code: "validation_failed",
                message: "Analysis id is required.",
                correlationId: ResolveCorrelationId(httpContext),
                details: [new ValidationErrorDetail("analysisId", "Analysis id is required.")]));
        }

        var deleted = await privacyControlService.DeleteAnalysisAsync(analysisId, cancellationToken);
        if (deleted)
        {
            return TypedResults.NoContent();
        }

        return TypedResults.NotFound(CreateError(
            code: "not_found",
            message: $"No persisted analysis exists for analysis '{analysisId}'.",
            correlationId: ResolveCorrelationId(httpContext)));
    }

    private static ApiErrorResponse CreateError(
        string code,
        string message,
        string? correlationId,
        IReadOnlyList<ValidationErrorDetail>? details = null)
        => new(new ApiErrorBody(
            Code: code,
            Message: message,
            Details: details,
            CorrelationId: correlationId));

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
