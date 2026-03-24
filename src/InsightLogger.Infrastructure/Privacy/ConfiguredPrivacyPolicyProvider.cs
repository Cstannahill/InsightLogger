using InsightLogger.Application.Abstractions.Privacy;
using InsightLogger.Infrastructure.Persistence.Db;
using Microsoft.Extensions.Options;

namespace InsightLogger.Infrastructure.Privacy;

public sealed class ConfiguredPrivacyPolicyProvider : IPrivacyPolicyProvider
{
    private readonly IOptions<PrivacyOptions> _options;

    public ConfiguredPrivacyPolicyProvider(IOptions<PrivacyOptions> options)
    {
        _options = options;
    }

    public PrivacyPolicy GetCurrentPolicy()
    {
        var options = _options.Value;
        return new PrivacyPolicy(
            RawContentStorageEnabled: options.RawContentStorageEnabled,
            RedactRawContentOnWrite: options.RedactRawContentOnWrite,
            RawContentRetentionDays: NormalizeDays(options.RawContentRetentionDays),
            AnalysisRetentionDays: NormalizeDays(options.AnalysisRetentionDays));
    }

    private static int? NormalizeDays(int? value)
        => value is > 0 ? value : null;
}
