using System;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Analyses.Commands;
using InsightLogger.Application.Analyses.Services;
using InsightLogger.Contracts.Analyses;
using InsightLogger.Contracts.Common;
using InsightLogger.Domain.Analyses;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.ApiTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace InsightLogger.ApiTests.Endpoints;

public sealed class AnalysisEndpointsTests : IClassFixture<ApiTestWebApplicationFactory>
{
    private readonly ApiTestWebApplicationFactory _factory;

    public AnalysisEndpointsTests(ApiTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnalyzeBuildLog_ShouldReturnValidationErrorEnvelope()
    {
        using var client = _factory.CreateClient();

        var request = new AnalyzeBuildLogRequest(
            Tool: "dotnet",
            Content: " ");

        using var response = await client.PostAsJsonAsync("/analyze/build-log", request);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("validation_failed", payload!.Error.Code);
        Assert.Contains(payload.Error.Details!, detail => detail.Field == "content");
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
        Assert.True(response.Headers.Contains("X-Request-Id"));
    }

    [Fact]
    public async Task AnalyzeBuildLog_ShouldReturnUnsupportedMediaTypeEnvelope()
    {
        using var client = _factory.CreateClient();

        using var content = new StringContent("plain text body", Encoding.UTF8, "text/plain");
        using var response = await client.PostAsync("/analyze/build-log", content);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("unsupported_media_type", payload!.Error.Code);
    }

    [Fact]
    public async Task AnalyzeCompilerError_ShouldReturnStructuredSuccessResponse()
    {
        using var client = _factory.CreateClient();

        var request = new AnalyzeCompilerErrorRequest(
            Tool: "dotnet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context");

        using var response = await client.PostAsJsonAsync("/analyze/compiler-error", request);
        var payload = await response.Content.ReadFromJsonAsync<AnalyzeCompilerErrorResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("fp_cs0103_name_missing", payload!.Fingerprint);
        Assert.Equal("dotnet", payload.ToolDetected);
        Assert.NotNull(payload.Diagnostic);
        Assert.Equal("CS0103", payload.Diagnostic!.Code);
    }

    [Fact]
    public async Task AnalyzeCompilerError_ShouldReturnInternalErrorEnvelope_WhenUnhandledExceptionOccurs()
    {
        await using var factory = new ThrowingAnalysisWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new AnalyzeCompilerErrorRequest(
            Tool: "dotnet",
            Content: "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context");

        using var response = await client.PostAsJsonAsync("/analyze/compiler-error", request);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("internal_error", payload!.Error.Code);
        Assert.Equal("An unexpected error occurred while processing the request.", payload.Error.Message);
    }

    private sealed class ThrowingAnalysisWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAnalysisService>();
                services.AddSingleton<IAnalysisService, ThrowingAnalysisService>();
            });
        }
    }

    private sealed class ThrowingAnalysisService : IAnalysisService
    {
        public Task<AnalysisResult> AnalyzeAsync(
            AnalyzeInputCommand command,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Boom");
    }
}
