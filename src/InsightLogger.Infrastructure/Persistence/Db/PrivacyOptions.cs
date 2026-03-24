namespace InsightLogger.Infrastructure.Persistence.Db;

public sealed class PrivacyOptions
{
    public const string SectionName = "Privacy";

    public bool RawContentStorageEnabled { get; set; } = true;
    public bool RedactRawContentOnWrite { get; set; } = true;
    public int? RawContentRetentionDays { get; set; } = 7;
    public int? AnalysisRetentionDays { get; set; } = 90;
}
