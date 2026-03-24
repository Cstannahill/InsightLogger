namespace InsightLogger.Application.Abstractions.Ai;

public interface IAiRootCauseNarrativeGenerator
{
    Task<AiRootCauseNarrativeResult> GenerateAsync(
        RootCauseNarrativeRequest request,
        CancellationToken cancellationToken = default);
}
