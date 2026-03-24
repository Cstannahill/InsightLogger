using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InsightLogger.GoldenLogs.Tests.Infrastructure;

public sealed class GoldenLogCase
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = default!;

    [JsonPropertyName("sampleLogPath")]
    public string SampleLogPath { get; init; } = default!;

    [JsonPropertyName("inputType")]
    public string InputType { get; init; } = default!;

    [JsonPropertyName("toolHint")]
    public string? ToolHint { get; init; }

    [JsonPropertyName("expect")]
    public GoldenLogExpectations Expect { get; init; } = new();
}

public sealed class GoldenLogExpectations
{
    [JsonPropertyName("toolDetected")]
    public string? ToolDetected { get; init; }

    [JsonPropertyName("summary")]
    public GoldenLogSummaryExpectations? Summary { get; init; }

    [JsonPropertyName("primaryFingerprints")]
    public IReadOnlyList<string> PrimaryFingerprints { get; init; } = [];

    [JsonPropertyName("requiredCategories")]
    public IReadOnlyList<string> RequiredCategories { get; init; } = [];

    [JsonPropertyName("requiredDiagnosticCodes")]
    public IReadOnlyList<string> RequiredDiagnosticCodes { get; init; } = [];

    [JsonPropertyName("requiredMessageFragments")]
    public IReadOnlyList<string> RequiredMessageFragments { get; init; } = [];

    [JsonPropertyName("parserName")]
    public string? ParserName { get; init; }

    [JsonPropertyName("maxUnparsedSegments")]
    public int? MaxUnparsedSegments { get; init; }
}

public sealed class GoldenLogSummaryExpectations
{
    [JsonPropertyName("totalDiagnostics")]
    public int? TotalDiagnostics { get; init; }

    [JsonPropertyName("groupCount")]
    public int? GroupCount { get; init; }

    [JsonPropertyName("primaryIssueCount")]
    public int? PrimaryIssueCount { get; init; }

    [JsonPropertyName("errorCount")]
    public int? ErrorCount { get; init; }

    [JsonPropertyName("warningCount")]
    public int? WarningCount { get; init; }
}
