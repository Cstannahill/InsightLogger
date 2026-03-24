using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using InsightLogger.Application.Abstractions.Ai;
using Microsoft.Extensions.Options;

namespace InsightLogger.Infrastructure.Ai;

public sealed class ConfiguredAiRootCauseNarrativeGenerator : IAiRootCauseNarrativeGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAiProviderCatalog _providerCatalog;
    private readonly IAiProviderHealthService _providerHealthService;
    private readonly IOptionsMonitor<AiOptions> _optionsMonitor;

    public ConfiguredAiRootCauseNarrativeGenerator(
        IHttpClientFactory httpClientFactory,
        IAiProviderCatalog providerCatalog,
        IAiProviderHealthService providerHealthService,
        IOptionsMonitor<AiOptions> optionsMonitor)
    {
        _httpClientFactory = httpClientFactory;
        _providerCatalog = providerCatalog;
        _providerHealthService = providerHealthService;
        _optionsMonitor = optionsMonitor;
    }

    public async Task<AiRootCauseNarrativeResult> GenerateAsync(
        RootCauseNarrativeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = _optionsMonitor.CurrentValue;
        var feature = options.Features.RootCauseNarrative;

        if (!options.Enabled)
        {
            return AiRootCauseNarrativeResult.Failure(
                status: "disabled",
                reason: "AI subsystem is disabled.");
        }

        if (!feature.Enabled)
        {
            return AiRootCauseNarrativeResult.Failure(
                status: "disabled",
                reason: "Root-cause narrative generation is disabled.");
        }

        var providers = await _providerCatalog.GetProvidersAsync(cancellationToken);
        var healthStatuses = await _providerHealthService.GetProviderHealthAsync(cancellationToken);
        var route = ResolveRoute(options, feature, providers, healthStatuses);

        if (route is null)
        {
            return AiRootCauseNarrativeResult.Failure(
                status: "unavailable",
                reason: "No healthy AI provider is configured for root-cause narrative generation.",
                provider: feature.Provider ?? options.DefaultProvider,
                model: feature.Model);
        }

        if (!IsSupported(route.Provider))
        {
            return AiRootCauseNarrativeResult.Failure(
                status: "unavailable",
                reason: "The selected AI provider type is not supported for root-cause narrative generation yet.",
                provider: route.Provider.Name,
                model: route.Model,
                fallbackUsed: route.FallbackUsed);
        }

        try
        {
            var rawOutput = route.Provider.Type.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
                ? await GenerateWithOllamaAsync(route.Provider, route.Model, feature, request, cancellationToken)
                : await GenerateWithOpenAiCompatibleAsync(route.Provider, route.Model, feature, request, cancellationToken);

            var parsed = ExtractNarrativePayload(rawOutput);
            if (string.IsNullOrWhiteSpace(parsed.Summary))
            {
                return AiRootCauseNarrativeResult.Failure(
                    status: "degraded",
                    reason: "AI provider returned an empty narrative summary.",
                    provider: route.Provider.Name,
                    model: route.Model,
                    fallbackUsed: route.FallbackUsed);
            }

            return AiRootCauseNarrativeResult.Successful(
                summary: parsed.Summary,
                groupSummaries: parsed.GroupSummaries,
                recommendedNextSteps: parsed.RecommendedNextSteps,
                provider: route.Provider.Name,
                model: route.Model,
                fallbackUsed: route.FallbackUsed);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AiRootCauseNarrativeResult.Failure(
                status: "degraded",
                reason: "AI root-cause narrative generation timed out.",
                provider: route.Provider.Name,
                model: route.Model,
                fallbackUsed: route.FallbackUsed);
        }
        catch (HttpRequestException ex)
        {
            return AiRootCauseNarrativeResult.Failure(
                status: "degraded",
                reason: $"AI request failed: {ex.Message}",
                provider: route.Provider.Name,
                model: route.Model,
                fallbackUsed: route.FallbackUsed);
        }
        catch (JsonException ex)
        {
            return AiRootCauseNarrativeResult.Failure(
                status: "degraded",
                reason: $"AI response parsing failed: {ex.Message}",
                provider: route.Provider.Name,
                model: route.Model,
                fallbackUsed: route.FallbackUsed);
        }
    }

    private async Task<string> GenerateWithOllamaAsync(
        AiProviderDefinition provider,
        string model,
        AiRootCauseNarrativeOptions feature,
        RootCauseNarrativeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(provider.BaseUrl))
        {
            throw new InvalidOperationException("Base URL is required for Ollama root-cause narrative generation.");
        }

        using var httpClient = CreateClient(provider, feature.TimeoutSeconds);
        var uri = BuildUri(provider.BaseUrl, "/api/generate");

        var body = new
        {
            model,
            prompt = BuildCombinedPrompt(request),
            stream = false,
            format = provider.Capabilities.SupportsJsonMode ? "json" : null,
            options = new
            {
                temperature = feature.Temperature,
                num_predict = feature.MaxOutputTokens
            }
        };

        using var response = await httpClient.PostAsJsonAsync(uri, body, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return json.RootElement.TryGetProperty("response", out var responseText)
            ? responseText.GetString() ?? string.Empty
            : string.Empty;
    }

    private async Task<string> GenerateWithOpenAiCompatibleAsync(
        AiProviderDefinition provider,
        string model,
        AiRootCauseNarrativeOptions feature,
        RootCauseNarrativeRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(provider.BaseUrl))
        {
            throw new InvalidOperationException("Base URL is required for AI root-cause narrative generation.");
        }

        using var httpClient = CreateClient(provider, feature.TimeoutSeconds);
        var uri = BuildUri(provider.BaseUrl, "chat/completions");

        var body = new
        {
            model,
            temperature = feature.Temperature,
            max_tokens = feature.MaxOutputTokens,
            stream = false,
            response_format = provider.Capabilities.SupportsJsonMode
                ? new { type = "json_object" }
                : null,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You summarize deterministic grouped software diagnostics into a concise build-failure narrative. Stay grounded in the supplied facts. Do not invent files, APIs, dependencies, or code. Return a JSON object with summary, groupSummaries, and recommendedNextSteps."
                },
                new
                {
                    role = "user",
                    content = BuildUserPrompt(request)
                }
            }
        };

        using var response = await httpClient.PostAsJsonAsync(uri, body, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!json.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            return string.Empty;
        }

        var message = choices[0].GetProperty("message");
        if (!message.TryGetProperty("content", out var contentElement))
        {
            return string.Empty;
        }

        return contentElement.ValueKind switch
        {
            JsonValueKind.String => contentElement.GetString() ?? string.Empty,
            JsonValueKind.Array => ExtractContentArrayText(contentElement),
            _ => string.Empty
        };
    }

    private static string ExtractContentArrayText(JsonElement contentElement)
    {
        var builder = new StringBuilder();

        foreach (var item in contentElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                builder.AppendLine(item.GetString());
                continue;
            }

            if (item.ValueKind == JsonValueKind.Object &&
                item.TryGetProperty("text", out var textProperty) &&
                textProperty.ValueKind == JsonValueKind.String)
            {
                builder.AppendLine(textProperty.GetString());
            }
        }

        return builder.ToString();
    }

    private static ResolvedNarrativeRoute? ResolveRoute(
        AiOptions options,
        AiRootCauseNarrativeOptions feature,
        IReadOnlyList<AiProviderDefinition> providers,
        IReadOnlyList<AiProviderHealthStatus> healthStatuses)
    {
        var healthyProviders = providers
            .Join(
                healthStatuses,
                provider => provider.Name,
                status => status.Name,
                (provider, status) => new { Provider = provider, Status = status },
                StringComparer.OrdinalIgnoreCase)
            .Where(static pair => string.Equals(pair.Status.Status, "healthy", StringComparison.OrdinalIgnoreCase))
            .Select(static pair => pair.Provider)
            .ToList();

        if (healthyProviders.Count == 0)
        {
            return null;
        }

        var preferredProviderName = feature.Provider ?? options.DefaultProvider;
        if (!string.IsNullOrWhiteSpace(preferredProviderName))
        {
            var preferred = healthyProviders.FirstOrDefault(provider =>
                string.Equals(provider.Name, preferredProviderName, StringComparison.OrdinalIgnoreCase));

            if (preferred is not null)
            {
                var preferredModel = feature.Model ?? preferred.DefaultModel;
                return string.IsNullOrWhiteSpace(preferredModel)
                    ? null
                    : new ResolvedNarrativeRoute(preferred, preferredModel, false);
            }

            if (feature.AllowFallback)
            {
                var fallback = healthyProviders[0];
                var fallbackModel = feature.Model ?? fallback.DefaultModel;
                return string.IsNullOrWhiteSpace(fallbackModel)
                    ? null
                    : new ResolvedNarrativeRoute(fallback, fallbackModel, true);
            }

            return null;
        }

        var selected = healthyProviders[0];
        var selectedModel = feature.Model ?? selected.DefaultModel;
        return string.IsNullOrWhiteSpace(selectedModel)
            ? null
            : new ResolvedNarrativeRoute(selected, selectedModel, false);
    }

    private static ParsedNarrativePayload ExtractNarrativePayload(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
        {
            return ParsedNarrativePayload.Empty;
        }

        var trimmed = rawOutput.Trim();

        if (trimmed.StartsWith("```", StringComparison.Ordinal) && trimmed.Contains('\n'))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && lastFence > firstNewLine)
            {
                trimmed = trimmed[(firstNewLine + 1)..lastFence].Trim();
            }
        }

        if (trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            using var json = JsonDocument.Parse(trimmed);
            return new ParsedNarrativePayload(
                Summary: NormalizeText(TryGetString(json.RootElement, "summary")),
                GroupSummaries: ReadStringArray(json.RootElement, "groupSummaries"),
                RecommendedNextSteps: ReadStringArray(json.RootElement, "recommendedNextSteps"));
        }

        return new ParsedNarrativePayload(
            Summary: NormalizeText(trimmed),
            GroupSummaries: Array.Empty<string>(),
            RecommendedNextSteps: Array.Empty<string>());
    }

    private static string? TryGetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return property
            .EnumerateArray()
            .Where(static item => item.ValueKind == JsonValueKind.String)
            .Select(static item => item.GetString())
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedLines = value
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return string.Join(" ", normalizedLines).Trim();
    }

    private HttpClient CreateClient(AiProviderDefinition provider, int timeoutSeconds)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, timeoutSeconds));

        if (!string.IsNullOrWhiteSpace(provider.BaseUrl))
        {
            client.BaseAddress = new Uri(EnsureTrailingSlash(provider.BaseUrl), UriKind.Absolute);
        }

        if (provider.HasApiKey &&
            _optionsMonitor.CurrentValue.Providers.TryGetValue(provider.Name, out var providerOptions) &&
            !string.IsNullOrWhiteSpace(providerOptions.ApiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", providerOptions.ApiKey);
        }

        return client;
    }

    private static bool IsSupported(AiProviderDefinition provider)
        => provider.Type.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
           || provider.Type.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)
           || provider.Type.Equals("OpenRouter", StringComparison.OrdinalIgnoreCase)
           || provider.Capabilities.SupportsOpenAiCompatibility;

    private static Uri BuildUri(string baseUrl, string relativePath)
        => new(new Uri(EnsureTrailingSlash(baseUrl), UriKind.Absolute), relativePath.TrimStart('/'));

    private static string EnsureTrailingSlash(string baseUrl)
        => baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : baseUrl + "/";

    private static string BuildCombinedPrompt(RootCauseNarrativeRequest request)
        => "System instructions:\n"
           + "Rewrite the deterministic grouped diagnostic summary into a concise build-failure narrative. Stay grounded in the provided deterministic findings. Return strict JSON with summary, groupSummaries, and recommendedNextSteps. groupSummaries should have 2-4 short items. recommendedNextSteps should have 2-4 concrete items.\n\n"
           + BuildUserPrompt(request);

    private static string BuildUserPrompt(RootCauseNarrativeRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Grouped diagnostic facts:");
        builder.AppendLine($"- Tool: {request.Tool}");
        builder.AppendLine($"- Total diagnostics: {request.TotalDiagnostics}");
        builder.AppendLine($"- Group count: {request.GroupCount}");
        builder.AppendLine($"- Error count: {request.ErrorCount}");
        builder.AppendLine($"- Warning count: {request.WarningCount}");
        builder.AppendLine($"- Deterministic narrative summary: {request.DeterministicSummary}");

        if (request.TopRootCauseTitles.Count > 0)
        {
            builder.AppendLine("- Top root-cause candidates:");
            foreach (var title in request.TopRootCauseTitles)
            {
                builder.AppendLine($"  - {title}");
            }
        }

        if (request.DeterministicGroupSummaries.Count > 0)
        {
            builder.AppendLine("- Deterministic group summaries:");
            foreach (var item in request.DeterministicGroupSummaries)
            {
                builder.AppendLine($"  - {item}");
            }
        }

        if (request.DeterministicNextSteps.Count > 0)
        {
            builder.AppendLine("- Deterministic next steps:");
            foreach (var item in request.DeterministicNextSteps)
            {
                builder.AppendLine($"  - {item}");
            }
        }

        if (request.Context is not null && request.Context.Count > 0)
        {
            builder.AppendLine("- Request context:");
            foreach (var pair in request.Context.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine($"  - {pair.Key}: {pair.Value}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("Return only JSON in this shape:");
        builder.AppendLine("{");
        builder.AppendLine("  \"summary\": \"2-4 sentence build-failure narrative\",");
        builder.AppendLine("  \"groupSummaries\": [\"group 1\", \"group 2\"],");
        builder.AppendLine("  \"recommendedNextSteps\": [\"step 1\", \"step 2\"]");
        builder.AppendLine("}");
        builder.AppendLine("Do not mention that this was AI-generated. Stay tightly grounded in the deterministic findings.");
        return builder.ToString();
    }

    private sealed record ResolvedNarrativeRoute(
        AiProviderDefinition Provider,
        string Model,
        bool FallbackUsed);

    private sealed record ParsedNarrativePayload(
        string Summary,
        IReadOnlyList<string> GroupSummaries,
        IReadOnlyList<string> RecommendedNextSteps)
    {
        public static readonly ParsedNarrativePayload Empty = new(string.Empty, Array.Empty<string>(), Array.Empty<string>());
    }
}
