using System.Reflection;
using InsightLogger.Api.Mapping;
using InsightLogger.Application.Ai.Queries;
using InsightLogger.Contracts.Ai;
using InsightLogger.Contracts.Common;
using InsightLogger.Contracts.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InsightLogger.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var healthGroup = endpoints.MapGroup("/health").WithTags("Health");
        var providersGroup = endpoints.MapGroup("/providers").WithTags("Providers");

        healthGroup.MapGet(string.Empty, HandleGetHealth)
            .WithName("GetHealth")
            .Produces<GetHealthResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Return basic service health.");

        healthGroup.MapGet("/ai", HandleGetAiHealthAsync)
            .WithName("GetAiHealth")
            .Produces<GetAiHealthResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Return AI subsystem and provider health information.");

        providersGroup.MapGet("/ai", HandleGetAiProvidersAsync)
            .WithName("GetAiProviders")
            .Produces<GetAiProvidersResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Return configured AI providers and normalized capability metadata.");

        return endpoints;
    }

    private static IResult HandleGetHealth()
    {
        return TypedResults.Ok(new GetHealthResponse(
            Status: "healthy",
            Service: "InsightLogger.Api",
            Version: ResolveVersion(),
            Timestamp: DateTimeOffset.UtcNow));
    }

    private static async Task<IResult> HandleGetAiHealthAsync(
        IAiMetadataQueryService queryService,
        CancellationToken cancellationToken)
    {
        var result = await queryService.GetHealthAsync(cancellationToken);
        return TypedResults.Ok(AiContractMapper.ToContract(result));
    }

    private static async Task<IResult> HandleGetAiProvidersAsync(
        IAiMetadataQueryService queryService,
        CancellationToken cancellationToken)
    {
        var result = await queryService.GetProvidersAsync(cancellationToken);
        return TypedResults.Ok(AiContractMapper.ToContract(result));
    }

    private static string ResolveVersion()
    {
        var assembly = typeof(Program).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var plusIndex = informationalVersion.IndexOf('+');
            return plusIndex >= 0
                ? informationalVersion[..plusIndex]
                : informationalVersion;
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.1.0";
    }
}
