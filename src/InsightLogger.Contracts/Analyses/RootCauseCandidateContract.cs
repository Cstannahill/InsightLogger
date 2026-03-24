using System.Collections.Generic;

namespace InsightLogger.Contracts.Analyses;

public sealed record RootCauseCandidateContract(
    string Fingerprint,
    string Title,
    string Explanation,
    double Confidence,
    IReadOnlyList<string> Signals,
    IReadOnlyList<string> LikelyCauses,
    IReadOnlyList<string> SuggestedFixes);
