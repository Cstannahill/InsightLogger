using System.Collections.Generic;
using InsightLogger.Application.Abstractions.Knowledge;
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
    string? DiagnosticCode,
    IReadOnlyList<string> KnownFixes,
    IReadOnlyList<RelatedRuleSummaryDto> RelatedRules,
    IReadOnlyList<KnowledgeReference>? KnowledgeReferences = null);
