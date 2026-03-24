using InsightLogger.Api.Mapping;
using InsightLogger.Contracts.Common;
using InsightLogger.Contracts.Privacy;
using InsightLogger.Application.Privacy.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InsightLogger.Api.Endpoints;

public static class PrivacyEndpoints
{
    public static IEndpointRouteBuilder MapPrivacyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var privacyGroup = endpoints.MapGroup("/privacy").WithTags("Privacy");

        privacyGroup.MapGet("/settings", HandleGetSettings)
            .WithName("GetPrivacySettings")
            .Produces<GetPrivacySettingsResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Get configured raw-content storage and retention settings.");

        privacyGroup.MapPost("/retention/apply", HandleApplyRetention)
            .WithName("ApplyPrivacyRetention")
            .Produces<ApplyRetentionPoliciesResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Apply configured retention policies to stored raw content and analysis history.");

        return endpoints;
    }

    private static async Task<IResult> HandleGetSettings(
        IPrivacyControlService service,
        CancellationToken cancellationToken)
    {
        var settings = await service.GetSettingsAsync(cancellationToken);
        return TypedResults.Ok(PrivacyContractMapper.ToContract(settings));
    }

    private static async Task<IResult> HandleApplyRetention(
        IPrivacyControlService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ApplyRetentionAsync(cancellationToken);
        return TypedResults.Ok(PrivacyContractMapper.ToContract(result));
    }
}
