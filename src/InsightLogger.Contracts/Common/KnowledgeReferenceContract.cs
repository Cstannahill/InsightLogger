using System.Collections.Generic;

namespace InsightLogger.Contracts.Common;

public sealed record KnowledgeReferenceContract(
    string Id,
    string Kind,
    string Source,
    string Title,
    string Summary,
    string? Url,
    string? ResourceType,
    string? ResourceId,
    IReadOnlyList<string> Tags);
