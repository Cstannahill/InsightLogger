using System.Collections.Generic;

namespace InsightLogger.Contracts.Analyses;

public sealed record DiagnosticGroupContract(
    string Fingerprint,
    int Count,
    string GroupReason,
    string PrimaryDiagnosticId,
    IReadOnlyList<string> RelatedDiagnosticIds);
