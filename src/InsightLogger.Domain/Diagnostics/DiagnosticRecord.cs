using System;
using System.Collections.Generic;

namespace InsightLogger.Domain.Diagnostics;

public sealed class DiagnosticRecord
{
    public DiagnosticRecord(
        ToolKind toolKind,
        Severity severity,
        string message,
        string rawSnippet,
        string? id = null,
        string? source = null,
        string? code = null,
        string? normalizedMessage = null,
        DiagnosticLocation? location = null,
        DiagnosticCategory category = DiagnosticCategory.Unknown,
        string? subcategory = null,
        bool isPrimaryCandidate = false,
        DiagnosticFingerprint? fingerprint = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(rawSnippet))
        {
            throw new ArgumentException("A diagnostic must have either a message or a raw snippet.");
        }

        Id = string.IsNullOrWhiteSpace(id) ? $"diag_{Guid.NewGuid():N}" : id.Trim();
        ToolKind = toolKind;
        Severity = severity;
        Message = message?.Trim() ?? string.Empty;
        RawSnippet = rawSnippet?.Trim() ?? string.Empty;
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        NormalizedMessage = string.IsNullOrWhiteSpace(normalizedMessage) ? Message : normalizedMessage.Trim();
        Location = location;
        Category = category;
        Subcategory = string.IsNullOrWhiteSpace(subcategory) ? null : subcategory.Trim();
        IsPrimaryCandidate = isPrimaryCandidate;
        Fingerprint = fingerprint;
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    public string Id { get; }
    public ToolKind ToolKind { get; }
    public string? Source { get; }
    public string? Code { get; }
    public Severity Severity { get; }
    public string Message { get; }
    public string NormalizedMessage { get; }
    public DiagnosticLocation? Location { get; }
    public string RawSnippet { get; }
    public DiagnosticCategory Category { get; }
    public string? Subcategory { get; }
    public bool IsPrimaryCandidate { get; }
    public DiagnosticFingerprint? Fingerprint { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }

    public DiagnosticRecord WithFingerprint(DiagnosticFingerprint fingerprint) =>
        new(
            toolKind: ToolKind,
            severity: Severity,
            message: Message,
            rawSnippet: RawSnippet,
            id: Id,
            source: Source,
            code: Code,
            normalizedMessage: NormalizedMessage,
            location: Location,
            category: Category,
            subcategory: Subcategory,
            isPrimaryCandidate: IsPrimaryCandidate,
            fingerprint: fingerprint,
            metadata: Metadata);

    public DiagnosticRecord MarkAsPrimaryCandidate() =>
        new(
            toolKind: ToolKind,
            severity: Severity,
            message: Message,
            rawSnippet: RawSnippet,
            id: Id,
            source: Source,
            code: Code,
            normalizedMessage: NormalizedMessage,
            location: Location,
            category: Category,
            subcategory: Subcategory,
            isPrimaryCandidate: true,
            fingerprint: Fingerprint,
            metadata: Metadata);
}
