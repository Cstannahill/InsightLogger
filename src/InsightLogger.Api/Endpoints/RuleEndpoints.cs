using System.Linq;
using InsightLogger.Api.Constants;
using InsightLogger.Api.Mapping;
using InsightLogger.Api.Results;
using InsightLogger.Api.Validation;
using InsightLogger.Application.Rules.Exceptions;
using InsightLogger.Application.Rules.Services;
using InsightLogger.Contracts.Common;
using InsightLogger.Contracts.Rules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InsightLogger.Api.Endpoints;

public static class RuleEndpoints
{
    public static IEndpointRouteBuilder MapRuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/rules").WithTags("Rules");

        group.MapPost(string.Empty, HandleCreateRuleAsync)
            .WithName("CreateRule")
            .Accepts<CreateRuleRequest>("application/json")
            .Produces<CreateRuleResponse>(StatusCodes.Status201Created)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Create a new custom deterministic rule.");

        group.MapGet(string.Empty, HandleGetRulesAsync)
            .WithName("GetRules")
            .Produces<GetRulesResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("List rules with optional filters.");

        group.MapGet("/{id}", HandleGetRuleByIdAsync)
            .WithName("GetRuleById")
            .Produces<GetRuleResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Get one rule by identifier.");

        group.MapPut("/{id}", HandleUpdateRuleAsync)
            .WithName("UpdateRule")
            .Accepts<UpdateRuleRequest>("application/json")
            .Produces<GetRuleResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
            .Produces<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Replace an existing rule definition.");

        group.MapPatch("/{id}/enabled", HandleSetRuleEnabledAsync)
            .WithName("SetRuleEnabled")
            .Accepts<SetRuleEnabledRequest>("application/json")
            .Produces<SetRuleEnabledResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Enable or disable a rule.");

        group.MapPost("/test", HandleTestRuleAsync)
            .WithName("TestRule")
            .Accepts<RuleTestRequest>("application/json")
            .Produces<RuleTestResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
            .WithSummary("Dry-run a rule against sample content without persisting analysis results.");

        return endpoints;
    }

    private static async Task<IResult> HandleCreateRuleAsync(
        CreateRuleRequest request,
        HttpContext httpContext,
        IRuleService ruleService,
        CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId(httpContext);
        var errors = RuleRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.BadRequest(ApiErrorResultFactory.ValidationFailed(errors, correlationId));
        }

        try
        {
            var command = RuleContractMapper.ToCommand(request);
            var created = await ruleService.CreateAsync(command, cancellationToken);
            var response = RuleContractMapper.ToResponse(created);

            return TypedResults.Created($"/rules/{response.Id}", response);
        }
        catch (RuleAlreadyExistsException ex)
        {
            return TypedResults.Conflict(BuildRuleConflictResponse(ex.Name, ex.Message, correlationId));
        }
    }

    private static async Task<IResult> HandleGetRulesAsync(
        HttpContext httpContext,
        IRuleService ruleService,
        bool? enabled,
        string? tool,
        string? tag,
        int? limit,
        int? offset,
        CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId(httpContext);
        var errors = new List<ValidationErrorDetail>();

        if (limit is <= 0 or > 100)
        {
            errors.Add(new ValidationErrorDetail("limit", "Limit must be between 1 and 100."));
        }

        if (offset < 0)
        {
            errors.Add(new ValidationErrorDetail("offset", "Offset must be greater than or equal to zero."));
        }

        if (!string.IsNullOrWhiteSpace(tool) && !AnalysisContractMapper.TryParseTool(tool, out _))
        {
            errors.Add(new ValidationErrorDetail("tool", "Tool must be a supported value."));
        }

        if (errors.Count > 0)
        {
            return TypedResults.BadRequest(ApiErrorResultFactory.ValidationFailed(errors, correlationId));
        }

        var result = await ruleService.ListAsync(enabled, tool, tag, limit ?? 50, offset ?? 0, cancellationToken);
        return TypedResults.Ok(RuleContractMapper.ToResponse(result));
    }

    private static async Task<IResult> HandleGetRuleByIdAsync(
        string id,
        HttpContext httpContext,
        IRuleService ruleService,
        CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId(httpContext);
        var rule = await ruleService.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return TypedResults.NotFound(BuildNotFoundResponse(correlationId, $"Rule '{id}' was not found."));
        }

        return TypedResults.Ok(RuleContractMapper.ToResponse(rule));
    }

    private static async Task<IResult> HandleUpdateRuleAsync(
        string id,
        UpdateRuleRequest request,
        HttpContext httpContext,
        IRuleService ruleService,
        CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId(httpContext);
        var errors = RuleRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.BadRequest(ApiErrorResultFactory.ValidationFailed(errors, correlationId));
        }

        try
        {
            var command = RuleContractMapper.ToCommand(id, request);
            var updated = await ruleService.UpdateAsync(command, cancellationToken);
            if (updated is null)
            {
                return TypedResults.NotFound(BuildNotFoundResponse(correlationId, $"Rule '{id}' was not found."));
            }

            return TypedResults.Ok(RuleContractMapper.ToResponse(updated));
        }
        catch (RuleAlreadyExistsException ex)
        {
            return TypedResults.Conflict(BuildRuleConflictResponse(ex.Name, ex.Message, correlationId));
        }
    }

    private static async Task<IResult> HandleSetRuleEnabledAsync(
        string id,
        SetRuleEnabledRequest request,
        HttpContext httpContext,
        IRuleService ruleService,
        CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId(httpContext);
        var errors = RuleRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.BadRequest(ApiErrorResultFactory.ValidationFailed(errors, correlationId));
        }

        var updated = await ruleService.SetEnabledAsync(id, request.IsEnabled, cancellationToken);
        if (updated is null)
        {
            return TypedResults.NotFound(BuildNotFoundResponse(correlationId, $"Rule '{id}' was not found."));
        }

        return TypedResults.Ok(RuleContractMapper.ToResponse(updated));
    }

    private static async Task<IResult> HandleTestRuleAsync(
        RuleTestRequest request,
        HttpContext httpContext,
        IRuleService ruleService,
        CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId(httpContext);
        var errors = RuleRequestValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.BadRequest(ApiErrorResultFactory.ValidationFailed(errors, correlationId));
        }

        var command = RuleContractMapper.ToCommand(request);
        var result = await ruleService.TestAsync(command, cancellationToken);
        if (result is null)
        {
            var missingRuleId = request.RuleId?.Trim() ?? "rule";
            return TypedResults.NotFound(BuildNotFoundResponse(correlationId, $"Rule '{missingRuleId}' was not found."));
        }

        return TypedResults.Ok(RuleContractMapper.ToResponse(result));
    }

    private static ApiErrorResponse BuildRuleConflictResponse(string name, string message, string? correlationId)
        => new(new ApiErrorBody(
            Code: ApiErrorCodes.RuleConflict,
            Message: message,
            Details: new[] { new ValidationErrorDetail("name", $"A rule named '{name}' already exists.") },
            CorrelationId: correlationId));

    private static ApiErrorResponse BuildNotFoundResponse(string? correlationId, string message)
        => ApiErrorResultFactory.Create(ApiErrorCodes.NotFound, message, correlationId);

    private static string? ResolveCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var values))
        {
            var value = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return httpContext.TraceIdentifier;
    }
}
