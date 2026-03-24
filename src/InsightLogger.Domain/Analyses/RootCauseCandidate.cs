using System.Collections.Generic;
using InsightLogger.Domain.Diagnostics;

namespace InsightLogger.Domain.Analyses;

public sealed record RootCauseCandidate(
    DiagnosticFingerprint Fingerprint,
    string Title,
    string Explanation,
    double Confidence,
    IReadOnlyList<string> Signals,
    IReadOnlyList<string> LikelyCauses,
    IReadOnlyList<string> SuggestedFixes,
    string? DiagnosticId = null,
    string? GroupId = null);
