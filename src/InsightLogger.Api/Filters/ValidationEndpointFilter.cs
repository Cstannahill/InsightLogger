using System;
using System.Linq;
using InsightLogger.Api.Validation;
using Microsoft.AspNetCore.Http;

namespace InsightLogger.Api.Filters;

public sealed class ValidationEndpointFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    private readonly IApiRequestValidator<TRequest> _validator;

    public ValidationEndpointFilter(IApiRequestValidator<TRequest> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is not null)
        {
            _validator.ValidateAndThrow(request);
        }

        return await next(context);
    }
}
