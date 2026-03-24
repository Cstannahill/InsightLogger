using System;

namespace InsightLogger.Domain.Diagnostics;

public readonly record struct DiagnosticFingerprint
{
    public DiagnosticFingerprint(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Fingerprint value cannot be null or whitespace.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;

    public static implicit operator string(DiagnosticFingerprint fingerprint) => fingerprint.Value;
}
