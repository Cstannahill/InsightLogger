using System;
using System.Collections.Generic;
using System.Linq;

namespace InsightLogger.Domain.Diagnostics;

public sealed class DiagnosticGroup
{
    public DiagnosticGroup(
        DiagnosticFingerprint fingerprint,
        string primaryDiagnosticId,
        IReadOnlyList<string> relatedDiagnosticIds,
        string groupReason)
    {
        if (string.IsNullOrWhiteSpace(primaryDiagnosticId))
        {
            throw new ArgumentException("A diagnostic group must have a primary diagnostic id.", nameof(primaryDiagnosticId));
        }

        if (relatedDiagnosticIds is null || relatedDiagnosticIds.Count == 0)
        {
            throw new ArgumentException("A diagnostic group must contain at least one related diagnostic id.", nameof(relatedDiagnosticIds));
        }

        if (!relatedDiagnosticIds.Contains(primaryDiagnosticId, StringComparer.Ordinal))
        {
            throw new ArgumentException("The primary diagnostic id must be included in the related diagnostic ids.", nameof(primaryDiagnosticId));
        }

        Fingerprint = fingerprint;
        PrimaryDiagnosticId = primaryDiagnosticId.Trim();
        RelatedDiagnosticIds = relatedDiagnosticIds;
        GroupReason = string.IsNullOrWhiteSpace(groupReason) ? "exact-fingerprint-dedupe" : groupReason.Trim();
    }

    public DiagnosticFingerprint Fingerprint { get; }
    public int Count => RelatedDiagnosticIds.Count;
    public string PrimaryDiagnosticId { get; }
    public IReadOnlyList<string> RelatedDiagnosticIds { get; }
    public string GroupReason { get; }
}
