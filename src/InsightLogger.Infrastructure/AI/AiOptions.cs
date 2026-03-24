namespace InsightLogger.Infrastructure.Ai;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public bool Enabled { get; set; }
    public string? DefaultProvider { get; set; }
    public Dictionary<string, AiProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AiProviderOptions
{
    public string Type { get; set; } = "Custom";
    public bool Enabled { get; set; }
    public string? DefaultModel { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public bool RequiresApiKey { get; set; }
    public AiProviderCapabilitiesOptions Capabilities { get; set; } = new();
}

public sealed class AiProviderCapabilitiesOptions
{
    public bool SupportsStreaming { get; set; }
    public bool SupportsToolCalling { get; set; }
    public bool SupportsJsonMode { get; set; }
    public bool SupportsOpenAiCompatibility { get; set; }
    public bool IsLocal { get; set; }
}
