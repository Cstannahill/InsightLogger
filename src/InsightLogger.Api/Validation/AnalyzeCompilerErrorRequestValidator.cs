using System.Collections.Generic;
using System.Linq;
using InsightLogger.Api.Constants;
using InsightLogger.Api.Exceptions;
using InsightLogger.Contracts.Analyses;
using Microsoft.AspNetCore.Http;

namespace InsightLogger.Api.Validation;

public sealed class AnalyzeCompilerErrorRequestValidator : IApiRequestValidator<AnalyzeCompilerErrorRequest>
{
    public void ValidateAndThrow(AnalyzeCompilerErrorRequest request)
    {
        var errors = AnalysisRequestValidator.Validate(request);
        if (errors.Count == 0)
        {
            return;
        }

        throw CreateValidationException(errors);
    }

    private static RequestValidationException CreateValidationException(IReadOnlyList<InsightLogger.Contracts.Common.ValidationErrorDetail> errors)
    {
        var hasPayloadTooLarge = errors.Any(static error => error.Field == "content" && error.Message.Contains("characters or fewer", System.StringComparison.Ordinal));
        return new RequestValidationException(
            hasPayloadTooLarge ? StatusCodes.Status413PayloadTooLarge : StatusCodes.Status400BadRequest,
            hasPayloadTooLarge ? ApiErrorCodes.PayloadTooLarge : ApiErrorCodes.ValidationFailed,
            hasPayloadTooLarge ? "The request content exceeds the configured limit." : "One or more validation errors occurred.",
            errors);
    }
}
