using System.Diagnostics;
using InsightLogger.Api.DependencyInjection;
using InsightLogger.Api.Endpoints;
using InsightLogger.Api.Middleware;
using InsightLogger.Application.DependencyInjection;
using InsightLogger.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
const string AllowAllCorsPolicy = "AllowAll";

builder.Logging.ClearProviders();
builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId |
                                      ActivityTrackingOptions.TraceId |
                                      ActivityTrackingOptions.ParentId;
});
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy(AllowAllCorsPolicy, policy =>
    {
        // TODO: Restrict origins for non-development environments.
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
builder.Services.AddInsightLoggerApi(builder.Configuration);
builder.Services.AddInsightLoggerApplication();
builder.Services.AddInsightLoggerInfrastructureParsing();
builder.Services.AddInsightLoggerInfrastructurePersistence(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RequestTelemetryMiddleware>();
app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.UseMiddleware<RequireJsonContentTypeMiddleware>();
app.UseCors(AllowAllCorsPolicy);

app.UseSwagger(options =>
{
    options.RouteTemplate = "openapi/{documentName}.json";
});
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "InsightLogger API v1");
});

app.MapAnalysisEndpoints();
app.MapPatternEndpoints();
app.MapRuleEndpoints();
app.MapHealthEndpoints();
app.MapPrivacyEndpoints();

app.Run();

public partial class Program;



