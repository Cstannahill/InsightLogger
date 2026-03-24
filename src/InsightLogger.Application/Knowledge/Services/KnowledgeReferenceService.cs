using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Knowledge;

namespace InsightLogger.Application.Knowledge.Services;

public sealed class KnowledgeReferenceService : IKnowledgeReferenceService
{
    private readonly IReadOnlyList<IKnowledgeReferenceSource> _sources;

    public KnowledgeReferenceService(IEnumerable<IKnowledgeReferenceSource> sources)
    {
        _sources = sources?.ToArray() ?? Array.Empty<IKnowledgeReferenceSource>();
    }

    public async Task<IReadOnlyList<KnowledgeReference>> GetReferencesAsync(
        KnowledgeReferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_sources.Count == 0)
        {
            return Array.Empty<KnowledgeReference>();
        }

        var merged = new List<KnowledgeReference>();

        foreach (var source in _sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var items = await source.GetReferencesAsync(request, cancellationToken);
            if (items.Count == 0)
            {
                continue;
            }

            merged.AddRange(items);
        }

        return merged
            .Where(static item => item is not null)
            .GroupBy(static item => item.Id, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(GetKindPriority)
            .ThenBy(static item => item.Title, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToArray();
    }

    private static int GetKindPriority(KnowledgeReference item)
        => item.Kind switch
        {
            "official-doc" => 0,
            "rule" => 1,
            "recurring-pattern" => 2,
            "prior-analysis" => 3,
            _ => 9
        };
}
