using InsightLogger.Application.Diagnostics.DTOs;
using InsightLogger.Application.Patterns.DTOs;
using InsightLogger.Contracts.Diagnostics;
using InsightLogger.Contracts.Patterns;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Api.Mapping;

public static class PatternContractMapper
{
    public static GetErrorByFingerprintResponse ToContract(ErrorFingerprintDetailsDto dto)
    {
        return new GetErrorByFingerprintResponse(
            Fingerprint: dto.Fingerprint,
            Title: dto.Title,
            Tool: ToToolContract(dto.ToolKind),
            Category: ToCategoryContract(dto.Category),
            CanonicalMessage: dto.CanonicalMessage,
            OccurrenceCount: dto.OccurrenceCount,
            FirstSeenAt: dto.FirstSeenAtUtc,
            LastSeenAt: dto.LastSeenAtUtc,
            KnownFixes: dto.KnownFixes,
            RelatedRules: dto.RelatedRules
                .Select(static rule => new RelatedRuleSummaryContract(
                    rule.Id,
                    rule.Name,
                    rule.MatchedBy,
                    rule.MatchCount,
                    rule.LastMatchedAtUtc,
                    rule.ProjectName,
                    rule.Repository))
                .ToArray());
    }

    public static GetTopPatternsResponse ToContract(IReadOnlyList<TopPatternItemDto> items)
    {
        return new GetTopPatternsResponse(
            items.Select(ToContract).ToArray());
    }

    public static TopPatternItemContract ToContract(TopPatternItemDto dto)
    {
        return new TopPatternItemContract(
            Fingerprint: dto.Fingerprint,
            Title: dto.Title,
            Tool: ToToolContract(dto.ToolKind),
            Category: ToCategoryContract(dto.Category),
            OccurrenceCount: dto.OccurrenceCount,
            LastSeenAt: dto.LastSeenAtUtc);
    }

    private static string ToToolContract(ToolKind toolKind)
    {
        return toolKind switch
        {
            ToolKind.DotNet => "dotnet",
            ToolKind.TypeScript => "typescript",
            ToolKind.Npm => "npm",
            ToolKind.Vite => "vite",
            ToolKind.Python => "python",
            ToolKind.Generic => "generic",
            _ => "unknown"
        };
    }

    private static string ToCategoryContract(DiagnosticCategory category)
    {
        return category switch
        {
            DiagnosticCategory.Syntax => "syntax",
            DiagnosticCategory.MissingSymbol => "missing-symbol",
            DiagnosticCategory.TypeMismatch => "type-mismatch",
            DiagnosticCategory.NullableSafety => "nullable-safety",
            DiagnosticCategory.Dependency => "dependency",
            DiagnosticCategory.Configuration => "configuration",
            DiagnosticCategory.BuildSystem => "build-system",
            DiagnosticCategory.RuntimeEnvironment => "runtime-environment",
            DiagnosticCategory.Serialization => "serialization",
            DiagnosticCategory.TestFailure => "test-failure",
            _ => "unknown"
        };
    }
}
