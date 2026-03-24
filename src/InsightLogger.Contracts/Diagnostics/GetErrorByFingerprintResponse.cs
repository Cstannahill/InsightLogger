using System.Collections.Generic;
using InsightLogger.Contracts.Common;

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
    string? DiagnosticCode,
    IReadOnlyList<string> KnownFixes,
    IReadOnlyList<RelatedRuleSummaryContract> RelatedRules,
    IReadOnlyList<KnowledgeReferenceContract> KnowledgeReferences);
