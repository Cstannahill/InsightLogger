namespace InsightLogger.Contracts.Health;

public sealed record TelemetryFingerprintItemContract(
    string Fingerprint,
    long Count);
