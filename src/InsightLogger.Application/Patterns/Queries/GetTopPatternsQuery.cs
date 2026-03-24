using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Patterns.Queries;

public sealed record GetTopPatternsQuery(
    ToolKind? ToolKind,
    int Limit = 10);
