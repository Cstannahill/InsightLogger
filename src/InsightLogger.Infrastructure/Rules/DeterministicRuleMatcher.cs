using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using InsightLogger.Application.Abstractions.Rules;
using InsightLogger.Application.Rules.DTOs;
using InsightLogger.Domain.Diagnostics;
using InsightLogger.Domain.Rules;

namespace InsightLogger.Infrastructure.Rules;

public sealed class DeterministicRuleMatcher : IRuleMatcher
{
    public Task<IReadOnlyList<RuleApplicationResult>> MatchAsync(
        IReadOnlyList<Rule> rules,
        IReadOnlyList<DiagnosticRecord> diagnostics,
        IReadOnlyList<DiagnosticGroup> groups,
        IReadOnlyDictionary<string, string>? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(groups);

        var matches = new List<RuleApplicationResult>();

        foreach (var rule in rules.OrderByDescending(static rule => rule.Priority))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var diagnostic in diagnostics)
            {
                if (TryMatchDiagnostic(rule, diagnostic, context, out var matchedConditions))
                {
                    matches.Add(new RuleApplicationResult(
                        Rule: rule,
                        TargetType: "diagnostic",
                        TargetId: diagnostic.Id,
                        MatchedFingerprint: diagnostic.Fingerprint?.Value,
                        MatchedConditions: matchedConditions,
                        AppliedActions: DescribeAppliedActions(rule)));
                }
            }

            foreach (var group in groups)
            {
                if (TryMatchGroup(rule, group, context, out var matchedConditions))
                {
                    matches.Add(new RuleApplicationResult(
                        Rule: rule,
                        TargetType: "group",
                        TargetId: group.PrimaryDiagnosticId,
                        MatchedFingerprint: group.Fingerprint.Value,
                        MatchedConditions: matchedConditions,
                        AppliedActions: DescribeAppliedActions(rule)));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<RuleApplicationResult>>(matches);
    }

    private static bool TryMatchDiagnostic(
        Rule rule,
        DiagnosticRecord diagnostic,
        IReadOnlyDictionary<string, string>? context,
        out IReadOnlyList<string> matchedConditions)
    {
        var matched = new List<string>();
        var condition = rule.Condition;

        if (!TryMatchScope(condition, context, matched, out matchedConditions))
        {
            return false;
        }

        if (condition.ToolKind is not null)
        {
            if (diagnostic.ToolKind != condition.ToolKind.Value)
            {
                matchedConditions = Array.Empty<string>();
                return false;
            }

            matched.Add("tool");
        }

        if (!string.IsNullOrWhiteSpace(condition.Code))
        {
            if (!string.Equals(diagnostic.Code, condition.Code, StringComparison.OrdinalIgnoreCase))
            {
                matchedConditions = Array.Empty<string>();
                return false;
            }

            matched.Add("code");
        }

        if (condition.Severity is not null)
        {
            if (diagnostic.Severity != condition.Severity.Value)
            {
                matchedConditions = Array.Empty<string>();
                return false;
            }

            matched.Add("severity");
        }

        if (condition.Category is not null)
        {
            if (diagnostic.Category != condition.Category.Value)
            {
                matchedConditions = Array.Empty<string>();
                return false;
            }

            matched.Add("category");
        }

        if (!string.IsNullOrWhiteSpace(condition.Fingerprint))
        {
            var fingerprint = diagnostic.Fingerprint?.Value;
            if (!string.Equals(fingerprint, condition.Fingerprint, StringComparison.Ordinal))
            {
                matchedConditions = Array.Empty<string>();
                return false;
            }

            matched.Add("fingerprint");
        }

        if (!string.IsNullOrWhiteSpace(condition.MessageRegex))
        {
            var text = diagnostic.NormalizedMessage ?? diagnostic.Message;
            if (!Regex.IsMatch(text, condition.MessageRegex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                matchedConditions = Array.Empty<string>();
                return false;
            }

            matched.Add("messageRegex");
        }

        if (!string.IsNullOrWhiteSpace(condition.FilePathRegex))
        {
            var filePath = diagnostic.Location?.FilePath ?? string.Empty;
            if (!Regex.IsMatch(filePath, condition.FilePathRegex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                matchedConditions = Array.Empty<string>();
                return false;
            }

            matched.Add("filePathRegex");
        }

        matchedConditions = matched;
        return matched.Count > 0;
    }

    private static bool TryMatchGroup(
        Rule rule,
        DiagnosticGroup group,
        IReadOnlyDictionary<string, string>? context,
        out IReadOnlyList<string> matchedConditions)
    {
        var matched = new List<string>();
        var condition = rule.Condition;

        if (!TryMatchScope(condition, context, matched, out matchedConditions))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(condition.Fingerprint))
        {
            if (!string.Equals(group.Fingerprint.Value, condition.Fingerprint, StringComparison.Ordinal))
            {
                matchedConditions = Array.Empty<string>();
                return false;
            }

            matched.Add("fingerprint");
        }

        matchedConditions = matched;
        return matched.Count > 0;
    }

    private static bool TryMatchScope(
        RuleCondition condition,
        IReadOnlyDictionary<string, string>? context,
        List<string> matched,
        out IReadOnlyList<string> matchedConditions)
    {
        if (!TryMatchContextValue(condition.ProjectName, context, "projectName", matched))
        {
            matchedConditions = Array.Empty<string>();
            return false;
        }

        if (!TryMatchContextValue(condition.Repository, context, "repository", matched))
        {
            matchedConditions = Array.Empty<string>();
            return false;
        }

        matchedConditions = matched;
        return true;
    }

    private static bool TryMatchContextValue(
        string? expected,
        IReadOnlyDictionary<string, string>? context,
        string key,
        List<string> matched)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            return true;
        }

        var actual = GetContextValue(context, key);
        if (string.IsNullOrWhiteSpace(actual) || !string.Equals(expected.Trim(), actual.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        matched.Add(key);
        return true;
    }

    private static string? GetContextValue(IReadOnlyDictionary<string, string>? context, string key)
    {
        if (context is null || context.Count == 0)
        {
            return null;
        }

        if (context.TryGetValue(key, out var value))
        {
            return value;
        }

        foreach (var pair in context)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> DescribeAppliedActions(Rule rule)
    {
        var actions = new List<string>();

        if (!string.IsNullOrWhiteSpace(rule.Action.Title))
        {
            actions.Add("title");
        }

        if (!string.IsNullOrWhiteSpace(rule.Action.Explanation))
        {
            actions.Add("explanation");
        }

        if (rule.Action.SuggestedFixesOrEmpty.Count > 0)
        {
            actions.Add("suggestedFixes");
        }

        if (rule.Action.ConfidenceAdjustment != 0d)
        {
            actions.Add("confidenceAdjustment");
        }

        if (rule.Action.MarkAsPrimaryCause)
        {
            actions.Add("markAsPrimaryCause");
        }

        return actions;
    }
}
