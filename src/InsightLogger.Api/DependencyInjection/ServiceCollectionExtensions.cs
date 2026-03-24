using InsightLogger.Api.Validation;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Infrastructure.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace InsightLogger.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInsightLoggerApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IApiRequestValidator<AnalyzeBuildLogRequest>, AnalyzeBuildLogRequestValidator>();
        services.AddSingleton<IApiRequestValidator<AnalyzeCompilerErrorRequest>, AnalyzeCompilerErrorRequestValidator>();

        var telemetrySection = configuration.GetSection(InsightLoggerTelemetryOptions.SectionName);
        var telemetryOptions = telemetrySection.Get<InsightLoggerTelemetryOptions>() ?? new InsightLoggerTelemetryOptions();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("InsightLogger.Api"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(InsightLoggerTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (telemetryOptions.ConsoleExporterEnabled)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(InsightLoggerTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (telemetryOptions.ConsoleExporterEnabled)
                {
                    metrics.AddConsoleExporter();
                }
            });

        return services;
    }
}
