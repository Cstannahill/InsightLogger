using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Api.Extensions;
using InsightLogger.Api.Filters;
using InsightLogger.Api.Mapping;
using InsightLogger.Application.Analyses.Services;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Contracts.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InsightLogger.Api.Endpoints;

public static class AnalysisEndpoints
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/analyze").WithTags("Analysis");

        group.MapPost("/build-log", HandleBuildLog)
            .AddEndpointFilter<ValidationEndpointFilter<AnalyzeBuildLogRequest>>()
            .WithName("AnalyzeBuildLog")
            .Accepts<AnalyzeBuildLogRequest>("application/json")
            .Produces<AnalyzeBuildLogResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status413PayloadTooLarge)
            .Produces<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Analyze a full build or tool log.");

        group.MapPost("/compiler-error", HandleCompilerError)
            .AddEndpointFilter<ValidationEndpointFilter<AnalyzeCompilerErrorRequest>>()
            .WithName("AnalyzeCompilerError")
            .Accepts<AnalyzeCompilerErrorRequest>("application/json")
            .Produces<AnalyzeCompilerErrorResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status413PayloadTooLarge)
            .Produces<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Analyze a single compiler/runtime diagnostic or compact error block.");

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
}
