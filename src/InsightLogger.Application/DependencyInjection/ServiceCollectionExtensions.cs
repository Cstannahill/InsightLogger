using InsightLogger.Application.Ai.Queries;
using InsightLogger.Application.Analyses.Services;
using InsightLogger.Application.Diagnostics.Queries;
using InsightLogger.Application.Patterns.Queries;
using InsightLogger.Application.Rules.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InsightLogger.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInsightLoggerApplication(this IServiceCollection services)
    {
        services.AddScoped<DiagnosticGroupingService>();
        services.AddScoped<AnalysisNarrativeFactory>();
        services.AddScoped<RootCauseRankingService>();
        services.AddScoped<RuleMatchingService>();
        services.AddScoped<IAnalysisService, AnalysisService>();

        services.AddScoped<IAiMetadataQueryService, AiMetadataQueryService>();
        services.AddScoped<IErrorFingerprintQueryService, ErrorFingerprintQueryService>();
        services.AddScoped<IPatternQueryService, PatternQueryService>();
        services.AddScoped<IRuleService, RuleService>();

        return services;
    }
}
