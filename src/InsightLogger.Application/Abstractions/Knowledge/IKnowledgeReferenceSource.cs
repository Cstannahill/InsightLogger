using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InsightLogger.Application.Abstractions.Knowledge;

public interface IKnowledgeReferenceSource
{
    Task<IReadOnlyList<KnowledgeReference>> GetReferencesAsync(
        KnowledgeReferenceRequest request,
        CancellationToken cancellationToken = default);
}
