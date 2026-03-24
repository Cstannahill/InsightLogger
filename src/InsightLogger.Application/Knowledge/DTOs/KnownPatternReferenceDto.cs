using System;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Knowledge.DTOs;

public sealed record KnownPatternReferenceDto(
    string Fingerprint,
    string Title,
    ToolKind ToolKind,
    DiagnosticCategory Category,
    int OccurrenceCount,
    DateTimeOffset LastSeenAtUtc,
    string? DiagnosticCode,
    string? LastSuggestedFix);
