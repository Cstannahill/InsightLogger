namespace InsightLogger.Contracts.Diagnostics;

public sealed record GetErrorByFingerprintResponse(
    string Fingerprint,
    string Title,
    string Tool,
    string Category,
    string CanonicalMessage,
    int OccurrenceCount,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    IReadOnlyList<string> KnownFixes,
    IReadOnlyList<RelatedRuleSummaryContract> RelatedRules);
