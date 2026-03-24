using System;
using System.IO;
using InsightLogger.Application.Abstractions.Ai;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Abstractions.Privacy;
using InsightLogger.Application.Abstractions.Rules;
using InsightLogger.Application.Abstractions.Telemetry;
using InsightLogger.Application.Analyses.Persistence;
using InsightLogger.Infrastructure.Ai;
using InsightLogger.Infrastructure.Parsing;
using InsightLogger.Infrastructure.Parsing.Detection;
using InsightLogger.Infrastructure.Parsing.DotNet;
using InsightLogger.Infrastructure.Parsing.JavaScript;
using InsightLogger.Infrastructure.Parsing.Python;
using InsightLogger.Infrastructure.Parsing.TypeScript;
using InsightLogger.Infrastructure.Persistence.Db;
using InsightLogger.Infrastructure.Persistence.Repositories;
using InsightLogger.Infrastructure.Privacy;
using InsightLogger.Infrastructure.Rules;
using InsightLogger.Infrastructure.Telemetry;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InsightLogger.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInsightLoggerInfrastructureParsing(this IServiceCollection services)
    {
        services.AddSingleton<IToolDetector, DefaultToolDetector>();
        services.AddSingleton<IDiagnosticParser, DotNetDiagnosticParser>();
        services.AddSingleton<IDiagnosticParser, TypeScriptDiagnosticParser>();
        services.AddSingleton<IDiagnosticParser, ViteDiagnosticParser>();
        services.AddSingleton<IDiagnosticParser, NpmDiagnosticParser>();
        services.AddSingleton<IDiagnosticParser, PythonTracebackParser>();
        services.AddSingleton<IDiagnosticParserCoordinator, DiagnosticParserCoordinator>();
        return services;
    }

    public static IServiceCollection AddInsightLoggerInfrastructurePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));
        services.Configure<PrivacyOptions>(configuration.GetSection(PrivacyOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<InsightLoggerTelemetryOptions>(configuration.GetSection(InsightLoggerTelemetryOptions.SectionName));

        services.AddDbContext<InsightLoggerDbContext>((serviceProvider, options) =>
        {
            var persistenceOptions = serviceProvider
                .GetRequiredService<IOptions<PersistenceOptions>>()
                .Value;

            EnsureSqliteDirectoryExists(persistenceOptions.ConnectionString);
            options.UseSqlite(persistenceOptions.ConnectionString);
        });

        services.AddHttpClient();
        services.AddSingleton<IInsightLoggerTelemetry, InsightLoggerTelemetry>();
        services.AddScoped<AnalysisPersistenceService>();
        services.AddScoped<IAnalysisPersistenceRepository, EfCoreAnalysisPersistenceRepository>();
        services.AddScoped<IAnalysisReadRepository, EfCoreAnalysisReadRepository>();
        services.AddScoped<IAnalysisPrivacyRepository, EfCoreAnalysisPrivacyRepository>();
        services.AddScoped<IAnalysisNarrativeReadRepository, EfCoreAnalysisNarrativeReadRepository>();
        services.AddScoped<IErrorPatternRepository, EfCoreErrorPatternRepository>();
        services.AddScoped<IErrorPatternReadRepository, EfCoreErrorPatternReadRepository>();
        services.AddScoped<IRuleRepository, EfCoreRuleRepository>();
        services.AddScoped<IInsightLoggerUnitOfWork, EfCoreInsightLoggerUnitOfWork>();
        services.AddSingleton<IPrivacyPolicyProvider, ConfiguredPrivacyPolicyProvider>();
        services.AddSingleton<IRuleMatcher, DeterministicRuleMatcher>();
        services.AddSingleton<IAiProviderCatalog, ConfiguredAiProviderCatalog>();
        services.AddSingleton<IAiProviderHealthService, ConfiguredAiProviderHealthService>();
        services.AddScoped<IAiExplanationEnricher, ConfiguredAiExplanationEnricher>();
        services.AddScoped<IAiRootCauseNarrativeGenerator, ConfiguredAiRootCauseNarrativeGenerator>();

        return services;
    }

    public static IServiceCollection AddInsightLoggerInfrastructurePersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.Configure<PersistenceOptions>(options =>
        {
            options.ConnectionString = connectionString;
            options.Enabled = true;
            options.AutoMigrate = false;
        });
        services.Configure<PrivacyOptions>(_ => { });
        services.Configure<AiOptions>(_ => { });
        services.Configure<InsightLoggerTelemetryOptions>(_ => { });

        EnsureSqliteDirectoryExists(connectionString);

        services.AddHttpClient();
        services.AddSingleton<IInsightLoggerTelemetry, InsightLoggerTelemetry>();
        services.AddDbContext<InsightLoggerDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<AnalysisPersistenceService>();
        services.AddScoped<IAnalysisPersistenceRepository, EfCoreAnalysisPersistenceRepository>();
        services.AddScoped<IAnalysisReadRepository, EfCoreAnalysisReadRepository>();
        services.AddScoped<IAnalysisPrivacyRepository, EfCoreAnalysisPrivacyRepository>();
        services.AddScoped<IAnalysisNarrativeReadRepository, EfCoreAnalysisNarrativeReadRepository>();
        services.AddScoped<IErrorPatternRepository, EfCoreErrorPatternRepository>();
        services.AddScoped<IErrorPatternReadRepository, EfCoreErrorPatternReadRepository>();
        services.AddScoped<IRuleRepository, EfCoreRuleRepository>();
        services.AddScoped<IInsightLoggerUnitOfWork, EfCoreInsightLoggerUnitOfWork>();
        services.AddSingleton<IPrivacyPolicyProvider, ConfiguredPrivacyPolicyProvider>();
        services.AddSingleton<IRuleMatcher, DeterministicRuleMatcher>();
        services.AddSingleton<IAiProviderCatalog, ConfiguredAiProviderCatalog>();
        services.AddSingleton<IAiProviderHealthService, ConfiguredAiProviderHealthService>();
        services.AddScoped<IAiExplanationEnricher, ConfiguredAiExplanationEnricher>();
        services.AddScoped<IAiRootCauseNarrativeGenerator, ConfiguredAiRootCauseNarrativeGenerator>();

        return services;
    }

    private static void EnsureSqliteDirectoryExists(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ":memory:")
        {
            return;
        }

        var fullPath = Path.GetFullPath(builder.DataSource);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
    }
}
