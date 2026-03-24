# Structured, Redacted Logging and Correlation

## Purpose

InsightLogger now emits structured JSON logs with shared correlation fields while avoiding raw build-log content and obvious secrets in log output.

## What is logged

At the orchestration boundaries, the service now logs:

- HTTP request start/completion/failure
- analysis pipeline start/completion
- AI enrichment route selection/outcome/failure
- persistence start/success/failure
- unhandled API exceptions

## Shared correlation fields

Request logs include these common fields when available:

- `correlationId`
- `requestId`
- `traceId`
- `spanId`
- `httpMethod`
- `requestPath`
- `endpoint`

Responses include:

- `X-Correlation-Id`
- `X-Request-Id`

## Privacy behavior

The logging slice avoids logging raw request bodies or full build-log content.

Instead, analysis logs use safe summaries such as:

- content length
- line count
- short SHA-256 hash prefix
- tool, parser, and counts

Exception messages and dynamic provider failures are passed through a deterministic redactor before being written to logs.

Current redaction targets include:

- `token=...`, `secret=...`, `password=...`, `api_key=...`
- bearer tokens
- full URLs
- Windows and Unix-like file paths
- email addresses

## Notes

- `correlationId` is client-supplied when `X-Correlation-Id` is provided; otherwise it falls back to the server request id.
- `requestId` is always server-generated from ASP.NET Core request tracing.
- `traceId` and `spanId` come from ambient activity tracking when available.
- This slice is intentionally logging-focused only. It does not yet add retention or raw-content privacy controls for persisted analysis history.
