# AI Integration Architecture

## Purpose

This document defines how AI providers integrate into InsightLogger.

The goal is to support multiple direct and aggregator providers without letting any single vendor SDK or API shape define the application architecture.

InsightLogger should treat AI as an optional enrichment subsystem that can:

- rewrite or improve explanations
- generate clearer suggested fixes
- summarize grouped diagnostics
- provide root-cause narratives after deterministic analysis

It should not depend on AI for core deterministic responsibilities such as parsing, fingerprinting, grouping, or rule matching.

## Design Goals

The AI integration layer must:

- support multiple providers cleanly
- keep application logic provider-agnostic
- support both local and hosted inference
- allow streaming and non-streaming generation
- support tool-calling where available
- normalize provider errors and usage information
- allow routing and fallback behavior
- expose provider capability metadata
- fail gracefully without breaking deterministic analysis

## Non-Goals

The AI layer is not intended to:

- define the core diagnostic domain model
- own log parsing
- leak vendor request/response models into the application layer
- force every provider into fake feature parity
- couple the rest of the system to one provider SDK

## High-Level Architecture

```text
Application Layer
  -> AI Abstractions
      -> Provider Router
          -> Provider Adapter
              -> Vendor API / Local Runtime
```

Detailed view:

```text
InsightLogger.Application
  -> IExplanationEnricher
  -> IRootCauseNarrativeGenerator
  -> IAiProviderRouter
  -> IChatCompletionProvider
  -> IStreamingCompletionProvider
  -> IToolCallingProvider
  -> IModelCatalogProvider
  -> IAiProviderHealthService
         |
         v
InsightLogger.Infrastructure.AI
  -> Routing
  -> Providers
      -> OpenAI
      -> Anthropic
      -> Ollama
      -> OpenRouter
      -> Gemini
      -> Bedrock
      -> AzureOpenAI
      -> Mistral
      -> Cohere
      -> OpenAICompatible
  -> Models
  -> Capabilities
  -> Prompting
  -> ErrorHandling
  -> Health
```

## Architectural Principles

### Internal normalization first

The system should define one internal AI request/response model and adapt vendors into it.

### Provider-specific features are optional extensions

A provider may support tool-calling, JSON mode, or streaming. The application should be able to query capabilities rather than assume them.

### Local and cloud are equal citizens

Ollama/local inference must not be treated as a second-class implementation.

### Routing belongs in infrastructure

The application layer should request an AI capability or task, not manually encode provider-specific routing behavior.

### Safe degradation

If AI fails, deterministic analysis should still succeed whenever possible.

## Provider Taxonomy

### Tier 1: First-class providers

These should have dedicated adapters and strong support.

- OpenAI
- Anthropic
- Ollama
- OpenRouter

### Tier 2: Strategic providers

These should be straightforward to add once the abstractions are stable.

- Google Gemini
- Amazon Bedrock
- Azure OpenAI / Azure AI Foundry
- Mistral

### Tier 3: Compatibility providers

These should use a reusable adapter path where possible.

- Groq
- Together
- xAI
- other OpenAI-compatible APIs

## Core Abstractions

These interfaces should live at the application boundary or in a shared abstraction layer used by the application.

### `IChatCompletionProvider`

Handles non-streaming text generation.

```csharp
public interface IChatCompletionProvider
{
    Task<AiGenerationResponse> GenerateAsync(
        AiGenerationRequest request,
        CancellationToken cancellationToken = default);
}
```

### `IStreamingCompletionProvider`

Handles streaming partial output.

```csharp
public interface IStreamingCompletionProvider
{
    IAsyncEnumerable<AiStreamEvent> StreamAsync(
        AiGenerationRequest request,
        CancellationToken cancellationToken = default);
}
```

### `IToolCallingProvider`

Handles tool-call capable providers.

```csharp
public interface IToolCallingProvider
{
    Task<AiToolCallResponse> GenerateWithToolsAsync(
        AiToolGenerationRequest request,
        CancellationToken cancellationToken = default);
}
```

### `IModelCatalogProvider`

Returns model metadata when available.

```csharp
public interface IModelCatalogProvider
{
    Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(
        CancellationToken cancellationToken = default);
}
```

### `IAiProviderHealthService`

Reports whether a provider is available and usable.

```csharp
public interface IAiProviderHealthService
{
    Task<AiProviderHealthStatus> CheckHealthAsync(
        string providerName,
        CancellationToken cancellationToken = default);
}
```

### `IAiCapabilityMapper`

Maps provider/model capabilities into normalized flags.

```csharp
public interface IAiCapabilityMapper
{
    AiProviderCapabilities GetCapabilities(
        string providerName,
        string? modelName = null);
}
```

### `IAiProviderRouter`

Selects a provider/model for a given request.

```csharp
public interface IAiProviderRouter
{
    Task<AiResolvedRoute> ResolveAsync(
        AiRoutingRequest request,
        CancellationToken cancellationToken = default);
}
```

### `IExplanationEnricher`

Task-oriented abstraction used by the application layer.

```csharp
public interface IExplanationEnricher
{
    Task<AiExplanationEnrichmentResult> EnrichAsync(
        ExplanationEnrichmentRequest request,
        CancellationToken cancellationToken = default);
}
```

### `IRootCauseNarrativeGenerator`

Used to summarize multiple deterministic findings.

```csharp
public interface IRootCauseNarrativeGenerator
{
    Task<RootCauseNarrativeResult> GenerateAsync(
        RootCauseNarrativeRequest request,
        CancellationToken cancellationToken = default);
}
```

## Normalized Request Models

These internal models should be the only request shape application code depends on.

### `AiGenerationRequest`

```csharp
public sealed class AiGenerationRequest
{
    public string TaskType { get; init; } = default!;
    public string? ProviderName { get; init; }
    public string? ModelName { get; init; }
    public IReadOnlyList<AiMessage> Messages { get; init; } = Array.Empty<AiMessage>();
    public AiGenerationOptions Options { get; init; } = new();
    public AiGenerationConstraints Constraints { get; init; } = new();
    public AiRequestMetadata Metadata { get; init; } = new();
}
```

### `AiMessage`

```csharp
public sealed class AiMessage
{
    public AiMessageRole Role { get; init; }
    public string Content { get; init; } = default!;
    public string? Name { get; init; }
    public IReadOnlyDictionary<string, string>? Attributes { get; init; }
}
```

### `AiGenerationOptions`

```csharp
public sealed class AiGenerationOptions
{
    public bool UseStreaming { get; init; }
    public bool PreferJsonOutput { get; init; }
    public bool AllowToolCalling { get; init; }
    public bool AllowFallback { get; init; } = true;
    public bool PrivacySensitive { get; init; }
    public double? Temperature { get; init; }
    public int? MaxOutputTokens { get; init; }
    public string? ReasoningEffort { get; init; }
}
```

### `AiGenerationConstraints`

```csharp
public sealed class AiGenerationConstraints
{
    public IReadOnlyList<string> AllowedProviders { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DisallowedProviders { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RequiredCapabilities { get; init; } = Array.Empty<string>();
    public string? PreferredRegion { get; init; }
    public bool RequireLocalOnly { get; init; }
}
```

### `AiRequestMetadata`

```csharp
public sealed class AiRequestMetadata
{
    public string? CorrelationId { get; init; }
    public string? FeatureName { get; init; }
    public string? UserScope { get; init; }
    public string? ProjectName { get; init; }
    public IReadOnlyDictionary<string, string>? Tags { get; init; }
}
```

### `AiToolGenerationRequest`

```csharp
public sealed class AiToolGenerationRequest : AiGenerationRequest
{
    public IReadOnlyList<AiToolDefinition> Tools { get; init; } = Array.Empty<AiToolDefinition>();
}
```

### `AiToolDefinition`

```csharp
public sealed class AiToolDefinition
{
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string JsonSchema { get; init; } = default!;
}
```

## Normalized Response Models

### `AiGenerationResponse`

```csharp
public sealed class AiGenerationResponse
{
    public string ProviderName { get; init; } = default!;
    public string ModelName { get; init; } = default!;
    public string OutputText { get; init; } = default!;
    public AiFinishReason FinishReason { get; init; }
    public AiUsage Usage { get; init; } = new();
    public AiResponseMetadata Metadata { get; init; } = new();
    public IReadOnlyList<AiWarning> Warnings { get; init; } = Array.Empty<AiWarning>();
}
```

### `AiToolCallResponse`

```csharp
public sealed class AiToolCallResponse
{
    public string ProviderName { get; init; } = default!;
    public string ModelName { get; init; } = default!;
    public string? OutputText { get; init; }
    public IReadOnlyList<AiToolCall> ToolCalls { get; init; } = Array.Empty<AiToolCall>();
    public AiUsage Usage { get; init; } = new();
    public AiResponseMetadata Metadata { get; init; } = new();
    public IReadOnlyList<AiWarning> Warnings { get; init; } = Array.Empty<AiWarning>();
}
```

### `AiToolCall`

```csharp
public sealed class AiToolCall
{
    public string ToolName { get; init; } = default!;
    public string ArgumentsJson { get; init; } = default!;
    public string? ProviderCallId { get; init; }
}
```

### `AiUsage`

```csharp
public sealed class AiUsage
{
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
    public int? TotalTokens { get; init; }
    public decimal? EstimatedCostUsd { get; init; }
    public TimeSpan? Duration { get; init; }
}
```

### `AiResponseMetadata`

```csharp
public sealed class AiResponseMetadata
{
    public bool WasFallbackUsed { get; init; }
    public string? FallbackFromProvider { get; init; }
    public string? FallbackReason { get; init; }
    public bool WasStreamingUsed { get; init; }
    public bool WasToolCallingUsed { get; init; }
    public string? RawProviderFinishReason { get; init; }
    public string? RequestId { get; init; }
}
```

### `AiStreamEvent`

```csharp
public sealed class AiStreamEvent
{
    public AiStreamEventType EventType { get; init; }
    public string? TextDelta { get; init; }
    public string? ToolName { get; init; }
    public string? ToolArgumentsDelta { get; init; }
    public AiUsage? UsageSnapshot { get; init; }
    public string? Warning { get; init; }
}
```

## Capability Matrix Model

The system should maintain a normalized capability descriptor rather than hardcode assumptions everywhere.

### `AiProviderCapabilities`

```csharp
public sealed class AiProviderCapabilities
{
    public bool SupportsStreaming { get; init; }
    public bool SupportsSystemPrompts { get; init; }
    public bool SupportsJsonMode { get; init; }
    public bool SupportsToolCalling { get; init; }
    public bool SupportsReasoningControl { get; init; }
    public bool SupportsVisionInput { get; init; }
    public bool SupportsEmbeddings { get; init; }
    public bool SupportsModelListing { get; init; }
    public bool SupportsOpenAiCompatibility { get; init; }
}
```

### Conceptual capability matrix

This matrix is intentionally conceptual. It should be resolved from provider/model metadata or static configuration rather than hardcoded application assumptions.

| Provider                  | Streaming                |             Tool Calling |    JSON/Structured Output | Local/Self-Hosted |  Aggregator | Notes                           |
| ------------------------- | ------------------------ | -----------------------: | ------------------------: | ----------------: | ----------: | ------------------------------- |
| OpenAI                    | Yes                      |                  Usually |                   Usually |                No |          No | First-class direct provider     |
| Anthropic                 | Yes                      |                  Usually | Partial/Provider-specific |                No |          No | First-class direct provider     |
| Ollama                    | Depends on model/runtime | Depends on model/runtime |  Depends on model/runtime |               Yes |          No | Local-first provider            |
| OpenRouter                | Usually                  |  Depends on routed model |   Depends on routed model |                No |         Yes | Aggregator / router             |
| Gemini                    | Usually                  |        Provider-specific |         Provider-specific |                No |          No | Strategic provider              |
| Bedrock                   | Provider-dependent       |       Provider-dependent |        Provider-dependent |                No | Yes/Managed | Meta-provider via AWS           |
| Azure OpenAI              | Usually                  |                  Usually |                   Usually |                No |     Managed | Enterprise-hosted OpenAI family |
| Mistral                   | Usually                  |        Provider-specific |         Provider-specific |                No |          No | Direct provider                 |
| OpenAI-Compatible vendors | Varies                   |                   Varies |                    Varies |            Varies |      Varies | Never assume parity             |

## Routing Architecture

Routing decides which provider and model should handle a request.

### Inputs to routing

- explicit provider name
- explicit model name
- required capabilities
- privacy sensitivity
- local-only requirement
- allowed/disallowed providers
- feature/task type
- provider health
- fallback policy

### Routing outputs

- resolved provider
- resolved model
- fallback chain
- resolved capabilities snapshot

### Example routing rules

#### Rule 1: explicit provider wins

If the request explicitly names a provider and it is enabled, use it.

#### Rule 2: local-only mode

If `RequireLocalOnly = true`, only local/self-hosted providers may be considered.

#### Rule 3: privacy-sensitive mode

If `PrivacySensitive = true`, route to configured privacy-approved providers only.

#### Rule 4: capability matching

If the request requires tool-calling or JSON mode, filter out providers that do not support the required capability.

#### Rule 5: preferred defaults by task

Examples:

- explanation rewrite -> cheap/fast provider or local provider
- root-cause narrative -> stronger reasoning-capable provider
- bulk enrichment job -> cheaper provider first

#### Rule 6: fallback policy

If the selected provider fails and fallback is allowed, retry with the next valid provider in the fallback chain.

## Fallback Rules

Fallback should be explicit and conservative.

### When fallback is allowed

- provider unavailable
- rate limit hit
- transient network failure
- timeout
- model unavailable
- provider returns unsupported-feature error for requested capability

### When fallback should not happen automatically

- privacy-sensitive request where fallback provider violates policy
- explicit provider requested with no fallback allowed
- tool-calling required but fallback provider lacks tool support
- structured JSON output required and fallback provider cannot satisfy it safely

### Suggested fallback order

This should be configurable, but a reasonable default might be:

1. explicit provider/model
2. same provider different configured model
3. same class of provider
4. configured default provider
5. aggregator fallback if allowed

### Example strategies

#### Local-first strategy

- Ollama
- OpenRouter
- OpenAI

#### Premium-direct strategy

- OpenAI
- Anthropic
- OpenRouter

#### Cost-sensitive strategy

- local provider
- cheaper aggregator route
- premium direct provider last

## Configuration Schema

AI configuration should support provider definitions, routing defaults, privacy controls, and per-feature overrides.

### Example configuration

```json
{
  "AI": {
    "Enabled": true,
    "DefaultProvider": "openrouter",
    "FallbackEnabled": true,
    "Providers": {
      "openai": {
        "Enabled": true,
        "Type": "OpenAI",
        "ApiKey": "${OPENAI_API_KEY}",
        "BaseUrl": null,
        "DefaultModel": "gpt-5-mini",
        "TimeoutSeconds": 60
      },
      "anthropic": {
        "Enabled": true,
        "Type": "Anthropic",
        "ApiKey": "${ANTHROPIC_API_KEY}",
        "DefaultModel": "claude-sonnet",
        "TimeoutSeconds": 60
      },
      "openrouter": {
        "Enabled": true,
        "Type": "OpenRouter",
        "ApiKey": "${OPENROUTER_API_KEY}",
        "DefaultModel": "stepfun/step-3.5-flash:free",
        "TimeoutSeconds": 60
      },
      "ollama": {
        "Enabled": true,
        "Type": "Ollama",
        "BaseUrl": "http://localhost:11434",
        "DefaultModel": "qwen3.5:latest",
        "TimeoutSeconds": 120
      }
    },
    "Routing": {
      "DefaultStrategy": "local-first",
      "FallbackChains": {
        "local-first": ["ollama", "openrouter", "openai"],
        "premium-direct": ["openai", "anthropic", "openrouter"],
        "cost-sensitive": ["ollama", "openrouter"]
      }
    },
    "Privacy": {
      "LocalOnlyProviders": ["ollama"],
      "DisallowFallbackWhenPrivacySensitive": true
    },
    "Features": {
      "ExplanationEnrichment": {
        "DefaultProvider": "openrouter",
        "DefaultModel": "stepfun/step-3.5-flash:free"
      },
      "RootCauseNarrative": {
        "DefaultProvider": "anthropic",
        "DefaultModel": "claude-sonnet"
      }
    }
  }
}
```

## Streaming Behavior

Streaming should be a first-class behavior, not an afterthought.

### Rules

- streaming should be requested explicitly or by feature configuration
- providers that do not support streaming should fall back to buffered generation when allowed
- streamed deltas should be normalized into `AiStreamEvent`
- final usage and finish reason should be emitted when available
- partial output should be safe to discard if the request is canceled

### Streaming event types

Suggested event kinds:

- `Started`
- `TextDelta`
- `ToolCallStarted`
- `ToolArgumentsDelta`
- `ToolCallCompleted`
- `Usage`
- `Completed`
- `Warning`
- `Error`

### Application guidance

For InsightLogger’s likely use cases, streaming is most useful for:

- UI-facing explanation generation
- long narrative summaries

It is less important for tiny enrichment payloads or background jobs.

## Tool-Calling Behavior

Tool-calling support should be normalized but optional.

### Primary use cases in InsightLogger

Potential future use cases:

- retrieve internal error-pattern references during explanation generation
- fetch rule documentation
- consult a knowledge base of recurring fixes
- inspect stored snippets or internal help articles

### Rules

- tool-calling must only be enabled when explicitly requested
- providers without tool support must be rejected for tool-required requests
- tool call payloads should be normalized into `AiToolCall`
- tool execution should stay outside the provider adapter; adapters only return requested tool invocations

### Important boundary

Provider adapter responsibility ends at:

- sending tool definitions
- receiving tool-call requests
- normalizing them

Actual execution of tools belongs elsewhere in the application/infrastructure workflow.

## Provider Error Normalization

Provider errors vary wildly. The system needs one normalized error model.

### `AiErrorKind`

Suggested values:

- `Unknown`
- `Authentication`
- `Authorization`
- `RateLimit`
- `QuotaExceeded`
- `Timeout`
- `Network`
- `Unavailable`
- `ModelNotFound`
- `UnsupportedCapability`
- `InvalidRequest`
- `ContentFiltered`
- `ProviderInternal`
- `Canceled`

### `AiProviderException`

```csharp
public sealed class AiProviderException : Exception
{
    public string ProviderName { get; init; } = default!;
    public AiErrorKind ErrorKind { get; init; }
    public int? StatusCode { get; init; }
    public bool IsRetryable { get; init; }
    public string? ProviderErrorCode { get; init; }
    public string? ProviderRequestId { get; init; }
    public string? RawMessage { get; init; }
}
```

### Error normalization rules

- preserve original provider details for logging
- map them into normalized error kinds for application logic
- mark retryability explicitly
- do not leak raw secrets or sensitive headers into logs or responses

### Fallback interaction

Fallback should rely on normalized error categories, not provider-specific string matching.

Examples:

- `RateLimit` -> fallback maybe allowed
- `Authentication` -> fallback usually not automatic unless another provider is already configured and allowed
- `UnsupportedCapability` -> fallback allowed only if alternate provider supports required capability
- `InvalidRequest` -> fallback usually not useful

## Health Checks

Each provider should support health inspection where practical.

### Health dimensions

- configured
- reachable
- authenticated
- model available
- streaming available if expected
- tool-calling available if expected

### Health statuses

Suggested values:

- `Healthy`
- `Degraded`
- `Unavailable`
- `Misconfigured`

## Logging and Telemetry

Track at minimum:

- provider selection count
- model usage count
- fallback rate
- provider error rate by normalized category
- latency by provider/model
- streaming usage rate
- tool-calling usage rate
- estimated cost by provider/feature

## Security and Privacy

The AI layer may handle build logs, code snippets, and potentially sensitive project details.

### Rules

- do not send data to non-approved providers when privacy-sensitive mode is enabled
- support local-only operation
- redact obvious secrets before provider submission where appropriate
- keep provider API keys in secure configuration sources
- log request metadata carefully; do not log full prompts indiscriminately in production mode

## Implementation Guidance

### Recommended folder structure

```text
InsightLogger.Infrastructure/
  AI/
    Abstractions/
    Routing/
    Prompting/
    Models/
    Capabilities/
    ErrorHandling/
    Health/
    Providers/
      OpenAI/
      Anthropic/
      Ollama/
      OpenRouter/
      Gemini/
      Bedrock/
      AzureOpenAI/
      Mistral/
      Cohere/
      OpenAICompatible/
```

### Recommended build order

1. define normalized request/response models
2. define provider abstractions
3. implement OpenAI adapter
4. implement Ollama adapter
5. implement OpenRouter adapter
6. implement Anthropic adapter
7. add provider router and fallback chain logic
8. add capability mapping
9. add streaming normalization
10. add tool-calling normalization
11. add health checks and telemetry

## Example Application Usage

### Explanation enrichment flow

1. application creates deterministic explanation payload
2. application builds `AiGenerationRequest`
3. router resolves provider/model
4. provider adapter generates response
5. application merges AI-enriched text back into analysis result
6. if AI fails, deterministic explanation remains

### Root-cause narrative flow

1. application passes grouped findings and ranked causes
2. AI generates narrative summary
3. response metadata records provider/model/fallback use
4. narrative is labeled as AI enrichment, not deterministic fact extraction

## Summary

The AI integration architecture for InsightLogger is built around one core rule: provider-specific complexity must be absorbed inside adapters and routing infrastructure, not leaked into the rest of the system. If this boundary holds, the product can support local inference, direct premium providers, aggregators, and future vendors without rewriting its core application logic.
