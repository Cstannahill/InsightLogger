using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Patterns.DTOs;
using InsightLogger.Application.Abstractions.Persistence;
using InsightLogger.Application.Diagnostics.DTOs;
using InsightLogger.Application.Diagnostics.Queries;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.ApplicationTests.Diagnostics;

public sealed class ErrorFingerprintQueryServiceTests
{
    [Fact]
    public async Task GetByFingerprintAsync_Enriches_Response_With_Related_Rules()
    {
        var service = new ErrorFingerprintQueryService(new FakePatternRepo(), new FakeRuleRepo());

        var result = await service.GetByFingerprintAsync(new GetErrorByFingerprintQuery("fp_cs0103_name_missing"));

        Assert.NotNull(result);
        Assert.Single(result!.RelatedRules);
        Assert.Equal("rule_1", result.RelatedRules[0].Id);
    }

    private sealed class FakePatternRepo : IErrorPatternReadRepository
    {
        public Task<ErrorFingerprintDetailsDto?> GetByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default)
        {
            ErrorFingerprintDetailsDto dto = new(
                Fingerprint: fingerprint,
                Title: "Unknown symbol in current context",
                ToolKind: ToolKind.DotNet,
                Category: DiagnosticCategory.MissingSymbol,
                CanonicalMessage: "The name '{identifier}' does not exist in the current context",
                OccurrenceCount: 10,
                FirstSeenAtUtc: DateTimeOffset.UtcNow.AddDays(-2),
                LastSeenAtUtc: DateTimeOffset.UtcNow,
                KnownFixes: new[] { "Check spelling." },
                RelatedRules: Array.Empty<RelatedRuleSummaryDto>());

            return Task.FromResult<ErrorFingerprintDetailsDto?>(dto);
        }

        public Task<IReadOnlyList<TopPatternItemDto>> GetTopPatternsAsync(ToolKind? toolKind, int limit, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class FakeRuleRepo : IRuleRepository
    {
        public Task<Domain.Rules.Rule> CreateAsync(Domain.Rules.Rule rule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsByNameAsync(string name, string? excludingId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Domain.Rules.Rule?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Domain.Rules.Rule>> ListAsync(bool? isEnabled, ToolKind? toolKind, string? tag, int limit, int offset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> CountAsync(bool? isEnabled, ToolKind? toolKind, string? tag, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Domain.Rules.Rule> UpdateAsync(Domain.Rules.Rule rule, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Domain.Rules.Rule>> GetEnabledRulesAsync(ToolKind? toolKind, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RecordMatchesAsync(IReadOnlyList<string> ruleIds, DateTimeOffset matchedAtUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<RelatedRuleSummaryDto>> GetRelatedRuleSummariesByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<RelatedRuleSummaryDto> items =
            [
                new RelatedRuleSummaryDto("rule_1", "Common missing symbol guidance", new[] { "fingerprint" }, 4, DateTimeOffset.UtcNow)
            ];

            return Task.FromResult(items);
        }
    }
}
