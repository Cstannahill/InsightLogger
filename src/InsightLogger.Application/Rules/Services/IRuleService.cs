using InsightLogger.Application.Rules.Commands;
using InsightLogger.Application.Rules.DTOs;

namespace InsightLogger.Application.Rules.Services;

public interface IRuleService
{
    Task<CreatedRuleDto> CreateAsync(CreateRuleCommand command, CancellationToken cancellationToken = default);

    Task<RuleListResultDto> ListAsync(
        bool? isEnabled,
        string? tool,
        string? tag,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    Task<RuleDetailsDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<RuleDetailsDto?> UpdateAsync(UpdateRuleCommand command, CancellationToken cancellationToken = default);

    Task<RuleEnabledStateDto?> SetEnabledAsync(string id, bool isEnabled, CancellationToken cancellationToken = default);

    Task<RuleTestResultDto?> TestAsync(TestRuleCommand command, CancellationToken cancellationToken = default);
}
