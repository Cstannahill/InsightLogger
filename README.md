# InsightLogger

InsightLogger is a .NET 9 API for turning raw build/compiler/runtime/tool output into structured diagnostics, ranked root-cause candidates, and actionable remediation guidance.

It is designed to be deterministic first, with optional AI enrichment layered on top.

## Key capabilities

- Deterministic analysis pipeline
- Tool detection and parser coordination
- Diagnostic normalization, classification, fingerprinting, grouping, and ranking
- Rule engine with create/list/get/update/enable APIs
- Rule dry-run testing (`POST /rules/test`)
- Scoped rules (`projectName`, `repository`)
- Rule recurrence analytics (`matchCount`, `lastMatchedAtUtc`)
- Pattern analytics (`GET /errors/{fingerprint}`, `GET /patterns/top`)
- Persisted analysis retrieval (`GET /analyses/{analysisId}`)
- Persisted narrative history and text search (`GET /analyses/narratives`)
- Persisted narrative detail (`GET /analyses/{analysisId}/narrative`)
- AI health/provider metadata APIs (`GET /health/ai`, `GET /providers/ai`)
- Optional AI explanation enrichment and optional AI root-cause narrative generation

Supported parser ecosystems:

- .NET / Roslyn
- TypeScript (`tsc`)
- Python tracebacks
- npm
- Vite

## API endpoints

Analysis:

- `POST /analyze/build-log`
- `POST /analyze/compiler-error`

Persisted analysis and narratives:

- `GET /analyses/narratives`
- `GET /analyses/{analysisId}`
- `GET /analyses/{analysisId}/narrative`

Patterns:

- `GET /errors/{fingerprint}`
- `GET /patterns/top`

Rules:

- `POST /rules`
- `GET /rules`
- `GET /rules/{id}`
- `PUT /rules/{id}`
- `PATCH /rules/{id}/enabled`
- `POST /rules/test`

Health and provider metadata:

- `GET /health`
- `GET /health/ai`
- `GET /providers/ai`

OpenAPI:

- `GET /openapi/v1.json`

## Quick start

Prerequisite:

- .NET SDK 9

Run the API:

```bash
dotnet run --project src/InsightLogger.Api
```

Then open:

- Swagger UI: `http://localhost:5031/swagger`
- OpenAPI JSON: `http://localhost:5031/openapi/v1.json`

## Typical workflow

1. Submit log content with `POST /analyze/build-log` (or `POST /analyze/compiler-error`).
2. Inspect ranked candidates, grouped diagnostics, rules, and processing metadata.
3. Persist analyses and query historical narratives via `GET /analyses/narratives`.
4. Open complete replayable details via `GET /analyses/{analysisId}`.
5. Use fingerprints in `GET /errors/{fingerprint}` to inspect recurrence and related rules.

## Repository structure

```text
src/
  InsightLogger.Api
  InsightLogger.Application
  InsightLogger.Domain
  InsightLogger.Infrastructure
  InsightLogger.Contracts
tests/
  InsightLogger.UnitTests
  InsightLogger.ApplicationTests
  InsightLogger.InfrastructureTests
  InsightLogger.ApiTests
  InsightLogger.IntegrationTests
  InsightLogger.GoldenLogs.Tests
docs/
samples/logs/
```

## Documentation

Architecture:

- [System overview](docs/architecture/system-overview.md)
- [Request processing pipeline](docs/architecture/request-processing-pipeline.md)
- [Module responsibilities](docs/architecture/module-responsibilities.md)

API:

- [Endpoints reference](docs/api/endpoints.md)
- [Request/response examples](docs/api/request-response-examples.md)

Project status:

- [Implementation log and slice status](docs/README.md)

Frontend integration reference (separate workspace):

- [React + TypeScript integration guide](docs/frontend/react-typescript-integration.md)

## Status

The roadmap implementation is currently integrated through `FrontendIntegrationReferenceSpecSlice` (23 slices). For detailed slice-by-slice history, see [docs/README.md](docs/README.md).
