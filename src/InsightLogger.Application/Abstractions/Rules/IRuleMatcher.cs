using InsightLogger.Application.Rules.DTOs;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;

namespace InsightLogger.Application.Abstractions.Rules;

public interface IRuleMatcher
{
    Task<IReadOnlyList<RuleApplicationResult>> MatchAsync(
        IReadOnlyList<Rule> rules,
        IReadOnlyList<DiagnosticRecord> diagnostics,
        IReadOnlyList<DiagnosticGroup> groups,
        IReadOnlyDictionary<string, string>? context = null,
        CancellationToken cancellationToken = default);
}
