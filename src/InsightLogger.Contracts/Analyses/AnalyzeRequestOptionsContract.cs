namespace InsightLogger.Contracts.Analyses;

public sealed record AnalyzeRequestOptionsContract(
    bool Persist = false,
    bool UseAiEnrichment = false,
    bool IncludeRawDiagnostics = true,
    bool IncludeGroups = true,
    bool IncludeProcessingMetadata = true,
    bool UseAiRootCauseNarrative = false);
