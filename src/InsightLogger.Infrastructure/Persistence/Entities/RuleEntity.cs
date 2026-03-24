namespace InsightLogger.Infrastructure.Persistence.Entities;

public sealed class RuleEntity
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public int Priority { get; set; }

    public string? ToolKindCondition { get; set; }
    public string? CodeCondition { get; set; }
    public string? SeverityCondition { get; set; }
    public string? CategoryCondition { get; set; }
    public string? MessageRegexCondition { get; set; }
    public string? FilePathRegexCondition { get; set; }
    public string? FingerprintCondition { get; set; }
    public string? ProjectNameCondition { get; set; }
    public string? RepositoryCondition { get; set; }

    public string? TitleAction { get; set; }
    public string? ExplanationAction { get; set; }
    public string? SuggestedFixesJson { get; set; }
    public double ConfidenceAdjustmentAction { get; set; }
    public bool MarkAsPrimaryCauseAction { get; set; }

    public string? TagsJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int MatchCount { get; set; }
    public DateTimeOffset? LastMatchedAtUtc { get; set; }
}
