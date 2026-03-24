using System;
using System.Collections.Generic;
using System.Linq;

namespace InsightLogger.Domain.Rules;

public sealed class Rule
{
    public Rule(
        string id,
        string name,
        string? description,
        bool isEnabled,
        int priority,
        RuleCondition condition,
        RuleAction action,
        IReadOnlyList<string>? tags = null,
        DateTimeOffset? createdAtUtc = null,
        DateTimeOffset? updatedAtUtc = null,
        int matchCount = 0,
        DateTimeOffset? lastMatchedAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(action);

        if (!condition.HasAnyTrigger())
        {
            throw new ArgumentException("A rule must define at least one condition trigger.", nameof(condition));
        }

        if (!action.HasAnyAction())
        {
            throw new ArgumentException("A rule must define at least one action.", nameof(action));
        }

        if (matchCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(matchCount), "Match count cannot be negative.");
        }

        Id = id.Trim();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsEnabled = isEnabled;
        Priority = priority;
        Condition = condition;
        Action = action;
        Tags = tags?
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? Array.Empty<string>();
        CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow;
        UpdatedAtUtc = updatedAtUtc ?? CreatedAtUtc;
        MatchCount = matchCount;
        LastMatchedAtUtc = lastMatchedAtUtc;
    }

    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public bool IsEnabled { get; }
    public int Priority { get; }
    public RuleCondition Condition { get; }
    public RuleAction Action { get; }
    public IReadOnlyList<string> Tags { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; }
    public int MatchCount { get; }
    public DateTimeOffset? LastMatchedAtUtc { get; }
}
