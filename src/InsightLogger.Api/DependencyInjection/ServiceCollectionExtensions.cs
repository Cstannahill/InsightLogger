using InsightLogger.Api.Validation;
using InsightLogger.Contracts.Analyses;
using Microsoft.Extensions.DependencyInjection;

namespace InsightLogger.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInsightLoggerApi(this IServiceCollection services)
    {
        services.AddSingleton<IApiRequestValidator<AnalyzeBuildLogRequest>, AnalyzeBuildLogRequestValidator>();
        services.AddSingleton<IApiRequestValidator<AnalyzeCompilerErrorRequest>, AnalyzeCompilerErrorRequestValidator>();
        return services;
    }
}
