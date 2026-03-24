namespace InsightLogger.Api.Validation;

public interface IApiRequestValidator<in TRequest>
    where TRequest : class
{
    void ValidateAndThrow(TRequest request);
}
