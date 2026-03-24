using System.Collections.Generic;
using System.Linq;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Application.Analyses.Services;

public sealed class DiagnosticGroupingService
{
    public IReadOnlyList<DiagnosticGroup> Group(IReadOnlyList<DiagnosticRecord> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            return new List<DiagnosticGroup>();
        }

        var groups = diagnostics
            .Where(static d => d.Fingerprint is not null)
            .GroupBy(static d => d.Fingerprint!.Value.Value, static d => d)
            .Select(BuildGroup)
            .OrderByDescending(static g => g.Count)
            .ThenBy(static g => g.PrimaryDiagnosticId, System.StringComparer.Ordinal)
            .ToList();

        return groups;
    }

    private static DiagnosticGroup BuildGroup(IGrouping<string, DiagnosticRecord> grouping)
    {
        var orderedDiagnostics = grouping
            .OrderByDescending(GetPrimarySignalScore)
            .ThenBy(static d => d.Id, System.StringComparer.Ordinal)
            .ToList();

        var primary = orderedDiagnostics[0];
        var groupReason = orderedDiagnostics.Count == 1
            ? "single-primary-diagnostic"
            : "exact-fingerprint-dedupe";

        return new DiagnosticGroup(
            fingerprint: new DiagnosticFingerprint(primary.Fingerprint!.Value),
            primaryDiagnosticId: primary.Id,
            relatedDiagnosticIds: orderedDiagnostics.Select(static d => d.Id).ToList(),
            groupReason: groupReason);
    }

    private static int GetPrimarySignalScore(DiagnosticRecord diagnostic) => diagnostic.Severity switch
    {
        Severity.Fatal => 400,
        Severity.Error => 300,
        Severity.Warning => 200,
        Severity.Info => 100,
        _ => 0
    };
}
