using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Analyses.Queries;

public sealed record GetAnalysisNarrativesQuery(
    ToolKind? ToolKind = null,
    string? Source = null,
    string? ProjectName = null,
    string? Repository = null,
    string? Text = null,
    int Limit = 20);
