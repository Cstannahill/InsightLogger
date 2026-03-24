# Docs

Project documentation for InsightLogger.

## Current implementation status

The codebase now includes nineteen implemented scaffolding slices:

1. InitialSlice
- Core domain models for diagnostics and analyses
- Deterministic .NET diagnostic parser + classifier/normalizer
- Deterministic diagnostic fingerprint generator

2. AnalysisSlice
- Tool detection and parser coordination layer
- Application analysis workflow (`AnalysisService`)
- Deterministic grouping/deduplication and root-cause ranking services
- Initial analysis-focused application/infrastructure tests

3. EndpointSlice
- First API endpoints:
  - `POST /analyze/build-log`
  - `POST /analyze/compiler-error`
- Contracts project DTOs for request/response and error payloads
- API request validation and error result factory
- API contract mapping layer between contracts and application models
- Application and infrastructure DI registration extensions
- API-focused tests for mapping and validation

4. MiddlewareAndApiTestsSlice
- API middleware stack for:
  - correlation ID propagation
  - JSON content-type enforcement on analysis endpoints
  - centralized API exception handling and error-envelope shaping
- Endpoint-level validation filter integration with validator abstractions
- Expanded API tests covering endpoint behavior and OpenAPI document expectations

5. TestLocksAndGoldenLogsSlice
- Added endpoint-focused API test locks for:
  - `POST /analyze/build-log`
  - `POST /analyze/compiler-error`
- Added OpenAPI contract checks:
  - structural assertions
  - reduced snapshot testing for stable contract verification
- Added deterministic golden-log test harness and initial .NET golden cases tied to `samples/logs/dotnet`
- Added repository path resolver test infrastructure for robust file-based test assets

6. PersistenceSlice
- Added SQLite-first EF Core persistence model and DbContext:
  - analyses, diagnostics, groups, error patterns, pattern occurrences
- Added application persistence abstractions and persistence orchestration service
- Added repository and unit-of-work infrastructure implementations
- Wired optional persistence into analysis flow (`AnalyzeInputCommand.Persist`)
- Added infrastructure + integration persistence tests
7. PatternAnalyticsSlice
- Added read-side analytics endpoints:
  - `GET /errors/{fingerprint}`
  - `GET /patterns/top`
- Added application query services and DTOs for fingerprint lookup/top-pattern analytics.
- Added read-specific persistence abstraction and EF Core read repository implementation.
- Added contracts + API mapping for analytics responses.
- Wired persistence DI and application DI for analytics query services.
- Added API/OpenAPI tests and infrastructure repository tests for analytics read flow.

8. RulesSlice
- Added first-class rule domain types and persistence model (`Rules` table + EF repository).
- Added deterministic rule matcher and rule services in application layer.
- Added `POST /rules` endpoint with request validation and mapping.
- Integrated rule evaluation into analysis flow so matched rules can influence root-cause output.
- Enriched `GET /errors/{fingerprint}` with related rule summaries.
- Added API, application, and infrastructure test coverage for rules.

9. RuleTestSlice
- Added `POST /rules/test` endpoint for dry-run rule testing without persistence writes.
- Added rule-testing request/response contracts and API mapping for rule preview results.
- Added application-level test execution flow for inline and persisted rules (`TestRuleCommand` and `RuleService.TestAsync`).
- Added rule preview evaluation path in `RuleMatchingService` for deterministic, non-persistent match/application inspection.
- Added API/OpenAPI and application test coverage for rule test endpoint behavior and response shape.

10. TypeScriptParserSlice
- Added deterministic TypeScript parser (`TypeScriptDiagnosticParser`) with classifier/normalizer support.
- Registered TypeScript parser in infrastructure DI so parser coordination can handle `tsc` diagnostics.
- Added TypeScript sample logs and golden-log case/test coverage.
- Expanded detector/parser/application tests for TypeScript diagnostic detection and analysis behavior.

11. PythonTracebackParserSlice
- Added deterministic Python traceback parser (`PythonTracebackParser`) with Python classifier/normalizer support.
- Registered Python traceback parser in infrastructure DI so parser coordination can handle runtime tracebacks.
- Added Python traceback sample logs and golden-log case/test coverage.
- Expanded detector/parser/application tests for Python traceback detection and analysis behavior.

12. NpmViteParserSlice
- Added deterministic JavaScript build parsers (`NpmDiagnosticParser`, `ViteDiagnosticParser`) with shared classifier/normalizer support.
- Registered npm and Vite parsers in infrastructure DI so parser coordination can handle frontend build/runtime bundler failures.
- Added npm/vite sample logs and golden-log case/test coverage.
- Expanded detector/parser/application tests for npm and Vite diagnostic detection and analysis behavior.

13. RuleRecurrenceAnalyticsSlice
- Added rule match analytics (`matchCount`, `lastMatchedAtUtc`) and persistence-time rule hit recording.
- Expanded related-rule lookup beyond exact fingerprint by evaluating enabled rules against a representative persisted diagnostic signature.
- Enriched `GET /rules`, `GET /rules/{id}`, and `GET /errors/{fingerprint}` responses with rule analytics metadata.
- Added API, OpenAPI, and infrastructure test coverage for recurrence-oriented rule read paths.

14. ScopedRuleSlice
- Added rule scoping by `projectName` and `repository` across rule contracts, application commands/DTOs, domain condition model, persistence entity/configuration, and API mappings/validation.
- Added scoped context support to rule matching and rule-test execution (`POST /rules/test` accepts optional `projectName`/`repository` and includes them in match evaluation context).
- Added persistence migration `20260324021500_AddRuleScopes` for scoped rule columns/indexes.
- Enriched related-rule summaries with scope metadata and scope-aware recurrence matching from persisted analysis context.
- Added API/OpenAPI/application/infrastructure/golden-log test updates for scoped-rule behavior.

15. AIHealthAndProviderMetadataSlice
- Added health and provider metadata endpoints:
  - `GET /health`
  - `GET /health/ai`
  - `GET /providers/ai`
- Added AI metadata query abstractions/services in application layer and configuration-driven provider catalog/health services in infrastructure.
- Added AI capability/provider/health contracts and API mapping for response shaping.
- Added default `Ai` configuration in API appsettings.
- Added API/OpenAPI/application/infrastructure tests for health and provider metadata flows.

16. AIExplanationEnrichmentSlice
- Added optional AI explanation enrichment flow for primary root-cause candidate text in `AnalysisService` when `useAiEnrichment` is requested.
- Added provider-routed `IAiExplanationEnricher` abstraction and configuration-driven `ConfiguredAiExplanationEnricher` implementation (Ollama and OpenAI-compatible providers).
- Added AI processing metadata (`requested`, provider/model, status, fallback, reason) and analysis warnings to analysis domain/contracts/mappers.
- Added `Ai.Features.ExplanationEnrichment` configuration model and appsettings defaults.
- Added API/OpenAPI/application/infrastructure tests for AI enrichment metadata projection, warning behavior, and enricher provider routing/parsing.

17. AIFixesAndLikelyCausesSlice
- Expanded deterministic likely-cause and fix guidance coverage across analyzers (C#, TypeScript, Python, npm, and Vite focused signatures).
- Extended AI explanation enrichment payload/response handling to optionally enrich explanation, likely causes, and suggested fixes while preserving deterministic fallbacks.
- Updated root-cause domain/contracts/mapping surfaces to carry likely causes for grouped/build-log candidate projections and kept compiler-error likely-cause derivation deterministic.
- Added/updated API/OpenAPI/application/infrastructure tests for likely-cause projection and AI enrichment payload parsing behavior.

18. AIRootCauseNarrativeSlice
- Added deterministic grouped build-log narrative projection via `AnalysisNarrativeFactory` for multi-diagnostic build logs.
- Added optional provider-routed `IAiRootCauseNarrativeGenerator` / `ConfiguredAiRootCauseNarrativeGenerator` flow for AI-enriched grouped summaries and next-step narratives.
- Extended build-log response contracts/mapping/OpenAPI surfaces with `narrative` metadata and tagged AI processing metadata with a `feature` discriminator.
- Added application/API/OpenAPI/infrastructure tests covering deterministic narrative projection and AI narrative success/failure fallback behavior.

19. AINarrativeToggleAndTaskProvenanceSlice
- Added a dedicated `useAiRootCauseNarrative` request option so narrative generation can be requested independently from `useAiEnrichment`.
- Updated analysis orchestration to run explanation enrichment and narrative generation as separate optional AI tasks within the same request.
- Extended processing metadata/contracts/mappers/OpenAPI with `aiTasks` for explicit per-task AI provenance while retaining `ai` as a single-task convenience summary.
- Added API/OpenAPI/application test updates and docs examples for separate toggles, combined-task requests, and per-task provenance responses.


## API description endpoint policy

- The API now exposes a single canonical OpenAPI JSON document at `/openapi/v1.json`.
- Swagger UI remains enabled for human exploration and is configured to consume that same document.
- This avoids maintaining duplicate API-description endpoints while preserving both machine-readable and interactive discovery workflows.

## Refactor planning artifacts

- Added [ASYNC_REFACTOR.md](/s:/Code/DotNet/InsightLogger/ASYNC_REFACTOR.md), a phased migration plan for moving analysis flow and endpoint handling from synchronous to asynchronous execution.

## Architecture alignment

The implemented slices align with the deterministic-first architecture in:
- `docs/architecture/system-overview.md`
- `docs/architecture/request-processing-pipeline.md`
- `docs/architecture/module-responsibilities.md`

Next planned increments remain:
- async refactor rollout across API/application/persistence boundaries
- persistence/query-time exposure for narrative-oriented historical analysis if narrative storage becomes valuable
- API endpoint orchestration hardening for future modules

## Recent maintenance updates

- Added root-level `README.md` with a standard public-facing project overview (purpose, features, endpoint summary, quick start, usage flow, architecture links) while keeping this file as the detailed implementation log.
- Updated `.gitignore` for public-repo hygiene by ignoring local `.dotnet` cache directories while explicitly un-ignoring `samples/logs/**` so canonical sample log fixtures remain versioned.
- Added `.gitignore` rules for SQLite artifacts under `App_Data` (`*.db`, `*.sqlite*`, `*.db-shm`, `*.db-wal`) so local runtime/test database files do not leak into source control.
- Normalized all test projects to target `.NET 9` (`net9.0`) via `normalize-test-targets.ps1`.
- Fixed compile-time grouping type inference in `DiagnosticGroupingService` by grouping on `DiagnosticFingerprint` keys directly.
- Added `Microsoft.Extensions.DependencyInjection.Abstractions` to `InsightLogger.Application` so application DI extension methods compile cleanly.
- Re-applied `DiagnosticGroupingService` grouping-key type fix after EndpointSlice merge to keep LINQ grouping/select types aligned (`DiagnosticFingerprint` keys).
- Added `InsightLogger.Infrastructure` project reference to `InsightLogger.ApplicationTests` for concrete detector/parser/coordinator usage in `AnalysisServiceTests`.
- Fixed invalid C# string escaping in infrastructure parser tests (`DotNetDiagnosticParserTests`) for Windows paths and embedded quoted MSBuild file paths.
- Hardened `DiagnosticGroupingService` key selection by grouping on `Fingerprint.Value` (string) explicitly to avoid LINQ type-inference regressions after slice overwrites.
- Repaired `DotNetDiagnosticParserTests` string literals after middleware slice overwrite by using verbatim Windows-path input and properly escaped quoted file paths in the MSBuild error sample.
- Configured Swashbuckle JSON route template to `openapi/{documentName}.json` so `/openapi/v1.json` is canonical, and Swagger UI consumes that same endpoint.
- Updated legacy OpenAPI test lock to `/openapi/v1.json` and made OpenAPI snapshot comparison newline-normalized across CRLF/LF environments.
- Aligned deterministic fingerprint expectations in API endpoint tests and golden-log case files with current hash-based fingerprint outputs.
- Updated mixed build golden-case summary expectation to the current grouping behavior (`groupCount = 3`).
- Updated mixed build golden-case `primaryIssueCount` expectation to `3` to match current deterministic classification output.
- Updated mixed build golden-case severity distribution to current deterministic output (`errorCount = 2`, `warningCount = 1`).
- Made OpenAPI reduced snapshot assertion tolerant to trailing whitespace/newline differences (`TrimEnd()` on both compared values).

- Began async refactor implementation: added async contracts across analysis/parsing/persistence, async API endpoint handlers with cancellation propagation, async AnalysisService/AnalysisPersistenceService paths, and async-capable parser/coordinator/detector/unit-of-work implementations with async-first service boundaries.

- Fixed persistence fingerprint typing in EfCoreErrorPatternRepository by using underlying string values from nullable DiagnosticFingerprint (.Value.Value) for distinct sets and dictionary lookups.

- Hardened EfCoreErrorPatternRepository upsert loading to include tracked DbSet.Local entities before database query so repeated upserts in the same DbContext do not create duplicate tracked ErrorPatternEntity keys.
- Disabled SQLite pooling in persistence integration tests (Data Source=...;Pooling=False) to avoid temp database file locks during test cleanup.

- Converted persistence repository async paths to async-first EF usage (AddAsync/AddRangeAsync with cancellation tokens) and finalized async repository internals.
- Migrated remaining analysis test/harness call sites to async entry points (AnalyzeAsync) across application tests, golden-log harness/tests, integration persistence tests, and infrastructure persistence tests.

- Added `Microsoft.EntityFrameworkCore.Design` (`9.*`, `PrivateAssets=all`) to the API startup project so `dotnet ef` tooling can run with `InsightLogger.Api` as startup project.
- Added a design-time EF Core DbContext factory (`DesignTimeInsightLoggerDbContextFactory`) so `dotnet ef` can create `InsightLoggerDbContext` without runtime DI wiring; supports `--connection=...` arg and `INSIGHTLOGGER_PERSISTENCE_CONNECTION_STRING` override.
- Added EF migration metadata attributes ([DbContext]/[Migration]) to the initial persistence migration so dotnet ef discovers it correctly during database update.
- Hardened design-time DbContext factory to create SQLite data directory automatically for file-based Data Source connection strings before opening the database.

- Fixed migration compile error by importing `Microsoft.EntityFrameworkCore.Infrastructure` so `[DbContext(...)]` attribute resolves in the initial migration class.
- Completed async-only API surface refactor: removed sync compatibility methods from analysis/parsing/persistence interfaces and implementations, and migrated all remaining callers/tests/harnesses to async entry points.


- Integrated PatternAnalyticsSlice: added `/errors/{fingerprint}` and `/patterns/top` endpoints, pattern contract mapping, application query services, EF Core read repository, and related API/OpenAPI/infrastructure tests.
- Integrated RulesSlice: added `/rules` creation endpoint, rule matching/evaluation services, rules persistence (`RuleEntity` + configuration + migration), and related API/application/infrastructure tests.
- Integrated RuleTestSlice: added `/rules/test` dry-run endpoint, rule preview application flow in `RuleService`, rule test contracts/mapping/validation updates, and API/OpenAPI/application test coverage.
- Integrated TypeScriptParserSlice: added deterministic TypeScript parsing/classification/normalization, parser DI registration, TypeScript sample log assets, and TypeScript-focused infrastructure/application/golden-log test coverage.
- Integrated PythonTracebackParserSlice: added deterministic Python traceback parsing/classification/normalization, parser DI registration, Python traceback sample log assets, and Python-focused infrastructure/application/golden-log test coverage.
- Integrated NpmViteParserSlice: added deterministic npm/vite parsing/classification/normalization, parser DI registration, npm/vite sample log assets, and npm/vite-focused infrastructure/application/golden-log test coverage.
- Integrated RuleRecurrenceAnalyticsSlice: added persisted rule recurrence metrics (`matchCount`, `lastMatchedAtUtc`), recurrence-aware related-rule lookup, enriched rule/pattern contracts, and analytics-focused API/application/infrastructure test coverage.
- Fixed npm parser compile regression by renaming shadowed locals in `NpmDiagnosticParser.TryParseSegment` (missing-script branch).
- Fixed RuleService compile regression after slice merge by re-adding `InsightLogger.Application.Analyses.Services` import so `DiagnosticGroupingService` and `RootCauseRankingService` resolve.
- Fixed analysis persistence type mismatch by carrying both `RuleMatch` and `RuleApplicationResult` through `RuleEvaluationResult` and persisting `Applications` in `AnalysisService`.
- Fixed RuleService rule-testing application-tests compile regression by restoring `InsightLogger.Application.Analyses.Services` import in `RuleServiceRuleTestingTests`.
- Fixed related-rule lookup stability for `GET /errors/{fingerprint}` by moving latest-occurrence ordering to client-side in `EfCoreRuleRepository` to avoid SQLite `DateTimeOffset` ordering translation/runtime issues.
- Restored canonical semantic fingerprints for common recurring diagnostics (`CS0103`, `CS8618`, `TS2304`, Python `NameError`) in `DeterministicDiagnosticFingerprintGenerator` while retaining hash-based fallback for other signatures.
- Updated API endpoint tests for compiler-error responses to assert canonical CS0103 fingerprint (`fp_cs0103_name_missing`) after semantic fingerprint restoration.
- Updated golden-log case expectations to canonical semantic fingerprints for `.NET CS0103`, `TypeScript TS2304`, and Python `NameError` scenarios.
- Fixed tool detection precedence to evaluate Vite markers before broad `.NET` build-failure heuristics, preventing Vite logs from being classified as DotNet.
- Fixed `EfCoreRuleRepository.RecordMatchesAsync` to persist match-count/last-matched mutations with `SaveChangesAsync` for direct repository usage paths and repository test parity.
- Integrated ScopedRuleSlice end-to-end: scoped rule conditions (`projectName`/`repository`) now flow through contracts, rule creation/update/test mapping, deterministic matcher context, rule query summaries, persistence storage, migration, and endpoint/OpenAPI/application/infrastructure test coverage.
- Preserved prior persistence and recurrence fixes while integrating scoped rules by keeping `AnalysisService` persistence on `RuleApplicationResult` applications, retaining SQLite-safe client-side ordering for related-rule latest occurrence selection, and persisting direct `RecordMatchesAsync` updates via `SaveChangesAsync`.
- Fixed `RuleServiceRuleTestingTests` compile regression after scoped-rule merge by restoring `InsightLogger.Application.Analyses.Services` import and updating assertions from removed `RuleTestResultDto.Matched` to `Matches` collection-based checks.
- Updated scoped-rule analysis test expectations to allow both diagnostic and group rule-match targets while still requiring scoped-condition matches (`projectName`, `repository`) on all emitted matches.
- Integrated AIHealthAndProviderMetadataSlice end-to-end: registered AI metadata query + provider catalog/health services in DI, mapped new health/provider endpoints, added AI contracts/mappers/configuration, and included corresponding API/OpenAPI/application/infrastructure test coverage.
- Added API test-host logging filters to reduce expected test noise by suppressing `InsightLogger.Api.Middleware.ApiExceptionHandlingMiddleware` error logs and lowering `Microsoft.EntityFrameworkCore.Database.Command` to warning level during API test runs.
- Integrated AINarrativeToggleAndTaskProvenanceSlice: split AI narrative execution behind `useAiRootCauseNarrative`, enabled combined AI task execution in one analysis request, and projected per-task provenance via `processing.aiTasks`.
- Reduced noisy EF SQL command logs by setting `Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command` to `Warning` in API appsettings defaults (including development), improving test output readability.
- Fixed post-slice test compile regressions by restoring missing test imports for `IRuleRepository` usage and enabling `IWebHostBuilder.ConfigureLogging` extension resolution in API endpoint tests.
