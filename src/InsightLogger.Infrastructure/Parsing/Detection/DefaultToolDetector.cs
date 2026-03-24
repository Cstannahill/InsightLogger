using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using InsightLogger.Application.Abstractions.Parsing;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Infrastructure.Parsing.Detection;

public sealed class DefaultToolDetector : IToolDetector
{
    public Task<ToolDetectionResult> DetectAsync(string content, ToolKind? explicitHint = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        if (explicitHint is not null and not ToolKind.Unknown)
        {
            return Task.FromResult(new ToolDetectionResult(explicitHint.Value, 1.0d, "explicit-tool-hint", true));
        }

        if (content.Contains("[vite]", StringComparison.OrdinalIgnoreCase)
            || content.Contains("vite v", StringComparison.OrdinalIgnoreCase)
            || content.Contains("error during build:", StringComparison.OrdinalIgnoreCase)
            || content.Contains("Rollup failed to resolve import", StringComparison.OrdinalIgnoreCase)
            || content.Contains("is not exported by", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ToolDetectionResult(ToolKind.Vite, 0.93d, "matched-vite-pattern"));
        }

        if (Regex.IsMatch(content, @"\b(CS\d{4}|MSB\d{4}|NETSDK\d{4}|NU\d{4})\b", RegexOptions.IgnoreCase)
            || content.Contains("Build FAILED", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(content, @"\.csproj\]$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            return Task.FromResult(new ToolDetectionResult(ToolKind.DotNet, 0.97d, "matched-dotnet-diagnostic-pattern"));
        }

        if (Regex.IsMatch(content, @"\bTS\d{4}\b", RegexOptions.IgnoreCase))
        {
            return Task.FromResult(new ToolDetectionResult(ToolKind.TypeScript, 0.97d, "matched-typescript-diagnostic-pattern"));
        }

        if (content.Contains("npm ERR!", StringComparison.OrdinalIgnoreCase)
            || content.Contains("npm error", StringComparison.OrdinalIgnoreCase)
            || content.Contains("Missing script:", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ToolDetectionResult(ToolKind.Npm, 0.9d, "matched-npm-error-pattern"));
        }

        if (content.Contains("Traceback (most recent call last):", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new ToolDetectionResult(ToolKind.Python, 0.95d, "matched-python-traceback-pattern"));
        }

        return Task.FromResult(new ToolDetectionResult(ToolKind.Unknown, 0.05d, "no-tool-pattern-matched"));
    }
}
