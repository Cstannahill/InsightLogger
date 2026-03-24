using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Diagnostics.DTOs;

public sealed record ErrorFingerprintDetailsDto(
    string Fingerprint,
    string Title,
    ToolKind ToolKind,
    DiagnosticCategory Category,
    string CanonicalMessage,
    int OccurrenceCount,
    DateTimeOffset FirstSeenAtUtc,
    DateTimeOffset LastSeenAtUtc,
    IReadOnlyList<string> KnownFixes,
    IReadOnlyList<RelatedRuleSummaryDto> RelatedRules);
