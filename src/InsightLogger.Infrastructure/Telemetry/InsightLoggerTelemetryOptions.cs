namespace InsightLogger.Infrastructure.Telemetry;

public sealed class InsightLoggerTelemetryOptions
{
    public const string SectionName = "Telemetry";

    public bool Enabled { get; set; } = true;

    public bool ConsoleExporterEnabled { get; set; }

    public int TopFingerprintLimit { get; set; } = 10;
}
