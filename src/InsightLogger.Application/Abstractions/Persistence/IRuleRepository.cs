using InsightLogger.Application.Diagnostics.DTOs;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;

namespace InsightLogger.Application.Abstractions.Persistence;

public interface IRuleRepository
{
    Task<bool> ExistsByNameAsync(string name, string? excludingId = null, CancellationToken cancellationToken = default);

    Task<Rule> CreateAsync(Rule rule, CancellationToken cancellationToken = default);

    Task<Rule?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Rule>> ListAsync(
        bool? isEnabled,
        ToolKind? toolKind,
        string? tag,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        bool? isEnabled,
        ToolKind? toolKind,
        string? tag,
        CancellationToken cancellationToken = default);

    Task<Rule> UpdateAsync(Rule rule, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Rule>> GetEnabledRulesAsync(ToolKind? toolKind, CancellationToken cancellationToken = default);

    Task RecordMatchesAsync(
        IReadOnlyList<string> ruleIds,
        DateTimeOffset matchedAtUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RelatedRuleSummaryDto>> GetRelatedRuleSummariesByFingerprintAsync(
        string fingerprint,
        CancellationToken cancellationToken = default);
}
