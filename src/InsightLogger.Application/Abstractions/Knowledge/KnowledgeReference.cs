using System;
using System.Collections.Generic;
using System.Linq;

namespace InsightLogger.Application.Abstractions.Knowledge;

public sealed record KnowledgeReference
{
    public KnowledgeReference(
        string id,
        string kind,
        string source,
        string title,
        string summary,
        string? url = null,
        string? resourceType = null,
        string? resourceId = null,
        IReadOnlyList<string>? tags = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        Id = id.Trim();
        Kind = kind.Trim();
        Source = source.Trim();
        Title = title.Trim();
        Summary = summary.Trim();
        Url = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        ResourceType = string.IsNullOrWhiteSpace(resourceType) ? null : resourceType.Trim();
        ResourceId = string.IsNullOrWhiteSpace(resourceId) ? null : resourceId.Trim();
        Tags = tags?
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();
    }

    public string Id { get; }
    public string Kind { get; }
    public string Source { get; }
    public string Title { get; }
    public string Summary { get; }
    public string? Url { get; }
    public string? ResourceType { get; }
    public string? ResourceId { get; }
    public IReadOnlyList<string> Tags { get; }
}
