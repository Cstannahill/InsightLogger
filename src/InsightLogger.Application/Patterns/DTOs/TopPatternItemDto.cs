using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Patterns.DTOs;

public sealed record TopPatternItemDto(
    string Fingerprint,
    string Title,
    ToolKind ToolKind,
    DiagnosticCategory Category,
    int OccurrenceCount,
    DateTimeOffset LastSeenAtUtc);
