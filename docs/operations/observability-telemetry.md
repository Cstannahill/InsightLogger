# Observability and Telemetry

InsightLogger now exposes a lightweight observability slice built around two layers:

1. **OpenTelemetry wiring** for traces and metrics export
2. **In-process telemetry snapshot reporting** for local development and UI/admin diagnostics

This keeps v1 practical: you can inspect live behavior immediately through the API without needing Prometheus, Grafana, Jaeger, or a hosted collector on day one.

## What is instrumented

### Analysis pipeline

The analysis flow records deterministic metrics for:

- total analysis requests
- completed vs failed analyses
- parse failures
- AI requested vs AI completed counts
- persistence failures
- average analysis duration
- average diagnostics per analysis
- unmatched/unknown analysis ratio
- tool selection counts
- parser selection counts
- top diagnostic fingerprints

### HTTP/API activity

The API middleware records:

- total HTTP requests
- average request duration
- methods
- status codes
- normalized routes

### Tracing

The API registers OpenTelemetry tracing for:

- ASP.NET Core request spans
- outgoing `HttpClient` spans
- custom `InsightLogger.Analysis` internal activities started by `AnalysisService`

## Endpoint

### `GET /health/telemetry`

Returns an in-process snapshot of current counters and aggregated summaries.

This endpoint is intended for:

- local debugging
- frontend admin/status views
- test/runtime inspection
- understanding parser usage and analysis behavior over time

It is **not** intended as a replacement for a real metrics backend.

## Configuration

```json
"Telemetry": {
  "Enabled": true,
  "ConsoleExporterEnabled": false,
  "TopFingerprintLimit": 10
}
```

### Fields

- `Enabled`: turns custom telemetry recording on/off
- `ConsoleExporterEnabled`: enables OpenTelemetry console exporter for traces/metrics
- `TopFingerprintLimit`: caps the number of fingerprint entries returned in the snapshot

## Why this shape

This slice intentionally avoids premature infrastructure choices.

It does **not** add:

- Prometheus-specific scraping endpoints
- vendor-specific observability SDK lock-in
- persisted telemetry tables
- heavy dashboard/reporting modules

Instead, it establishes the right seam first:

- application code records telemetry through an abstraction
- infrastructure owns the concrete counters/activities
- API exposes a safe summary surface
- OpenTelemetry exporters can be turned on later without rewriting analysis flow

## Next likely observability moves

Once this baseline proves useful, the next strongest additions are:

- structured log enrichment with correlation IDs and analysis ids
- secret/path redaction in logs before external export
- Prometheus or OTLP exporter support
- per-provider AI latency/error metrics
- persistence-specific timings and DB failure classification
- dashboard views for fingerprint trends and parser reliability
