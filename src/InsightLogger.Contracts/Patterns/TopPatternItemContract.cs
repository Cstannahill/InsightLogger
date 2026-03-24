namespace InsightLogger.Contracts.Patterns;

public sealed record TopPatternItemContract(
    string Fingerprint,
    string Title,
    string Tool,
    string Category,
    int OccurrenceCount,
    DateTimeOffset LastSeenAt);
