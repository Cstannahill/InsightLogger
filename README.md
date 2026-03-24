# InsightLogger

InsightLogger is a developer-focused API that accepts raw build/compiler/runtime/tool output, normalizes it into structured diagnostics, identifies likely root causes, reduces noise, and returns actionable guidance.

## Why this exists

Build logs are noisy. InsightLogger helps answer:

- What actually failed?
- Which diagnostics matter most?
- What should I try next?
- Is this a recurring issue?

## What it does today

- Deterministic-first analysis pipeline:
  - tool detection
  - parser coordination
  - normalization and classification
  - fingerprinting
  - dedupe/grouping
  - root-cause ranking
- Rule engine:
  - create/update/list/enable rules
  - dry-run rule testing (`POST /rules/test`)
  - scoped rules (`projectName`, `repository`)
  - rule recurrence analytics (`matchCount`, `lastMatchedAtUtc`)
- Pattern analytics:
  - lookup by fingerprint
  - top recurring patterns
- Health and AI metadata:
  - overall health
  - AI provider health
  - AI provider capability catalog
- Persistence with EF Core + SQLite migrations
- Golden-log tests for deterministic behavior

Supported input ecosystems in current parsers:

- .NET / Roslyn
- TypeScript (`tsc`)
- Python tracebacks
- npm
- Vite

## API surface

Core endpoints:

- `POST /analyze/build-log`
- `POST /analyze/compiler-error`
- `GET /errors/{fingerprint}`
- `GET /patterns/top`
- `POST /rules`
- `GET /rules`
- `GET /rules/{id}`
- `PUT /rules/{id}`
- `PATCH /rules/{id}/enabled`
- `POST /rules/test`
- `GET /health`
- `GET /health/ai`
- `GET /providers/ai`

OpenAPI:

- `GET /openapi/v1.json`

See full endpoint details in [docs/api/endpoints.md](docs/api/endpoints.md).

## Quick start

Prerequisites:

- .NET SDK 9

Run the API:

```bash
dotnet run --project src/InsightLogger.Api
```

Then open:

- Swagger UI: `http://localhost:5000/swagger` (or configured port)
- OpenAPI JSON: `http://localhost:5000/openapi/v1.json`

## Typical usage

1. Send a full build log to `POST /analyze/build-log`.
2. Receive normalized diagnostics, groups, root-cause candidates, matched rules, and processing metadata.
3. Use returned fingerprints with `GET /errors/{fingerprint}` to inspect recurrence history and related rules.
4. Add project-specific guidance via rules, and validate rules with `POST /rules/test` before enabling them.

## Project structure

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

## Architecture and design docs

- System overview: [docs/architecture/system-overview.md](docs/architecture/system-overview.md)
- Request pipeline: [docs/architecture/request-processing-pipeline.md](docs/architecture/request-processing-pipeline.md)
- Module responsibilities: [docs/architecture/module-responsibilities.md](docs/architecture/module-responsibilities.md)
- API contracts and examples:
  - [docs/api/endpoints.md](docs/api/endpoints.md)
  - [docs/api/request-response-examples.md](docs/api/request-response-examples.md)

## Current status

The implemented roadmap currently includes the slices through:

- `ScopedRuleSlice`
- `AIHealthAndProviderMetadataSlice`
- `AIExplanationEnrichmentSlice`

Detailed implementation log is maintained in [docs/README.md](docs/README.md).
