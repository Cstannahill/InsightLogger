namespace InsightLogger.Application.Abstractions.Ai;

public interface IAiExplanationEnricher
{
    Task<AiExplanationEnrichmentResult> EnrichAsync(
        ExplanationEnrichmentRequest request,
        CancellationToken cancellationToken = default);
}
