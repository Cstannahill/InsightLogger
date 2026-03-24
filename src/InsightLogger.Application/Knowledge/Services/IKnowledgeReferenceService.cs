using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Knowledge;

namespace InsightLogger.Application.Knowledge.Services;

public interface IKnowledgeReferenceService
{
    Task<IReadOnlyList<KnowledgeReference>> GetReferencesAsync(
        KnowledgeReferenceRequest request,
        CancellationToken cancellationToken = default);
}
