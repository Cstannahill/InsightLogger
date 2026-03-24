using InsightLogger.Api.DependencyInjection;
using InsightLogger.Api.Endpoints;
using InsightLogger.Api.Middleware;
using InsightLogger.Application.DependencyInjection;
using InsightLogger.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInsightLoggerApi();
builder.Services.AddInsightLoggerApplication();
builder.Services.AddInsightLoggerInfrastructureParsing();
builder.Services.AddInsightLoggerInfrastructurePersistence(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.UseMiddleware<RequireJsonContentTypeMiddleware>();

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

app.Run();

public partial class Program;
