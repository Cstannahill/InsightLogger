## Unreleased

### Added
- Integrated `ObservabilityTelemetrySlice`:
- Added OpenTelemetry tracing/metrics registration for ASP.NET Core, outgoing `HttpClient`, and custom `InsightLogger.Analysis` activities.
- Added in-memory telemetry aggregation for analysis and HTTP activity, exposed through `GET /health/telemetry`.
- Added telemetry contracts/mapping/configuration plus API/infrastructure test coverage and an operations guide (`docs/operations/observability-telemetry.md`).
- Added `docs/frontend/react-typescript-integration.md`, a React + TypeScript frontend integration/reference spec covering route/page flows, endpoint/query mapping, typed query shapes, UI states, timeline rendering guidance, and recommended component/data boundaries for analyze/history/detail/AI status views.
- Updated `docs/README.md` to index frontend integration guidance for the separate UI workspace.
- Integrated `TypeScriptParserSlice`:
- Deterministic TypeScript parsing support via `TypeScriptDiagnosticParser`, `TypeScriptDiagnosticClassifier`, and `TypeScriptDiagnosticNormalizer`.
- TypeScript parser registration in infrastructure DI (`AddInsightLoggerInfrastructureParsing`).
- TypeScript sample assets (`samples/logs/tsc/ts2304-single-diagnostic.log`, docs example update, and golden-log case/tests).
- TypeScript-focused test coverage across infrastructure parsing/detection and application analysis flow.
- Integrated `PythonTracebackParserSlice`:
- Deterministic Python traceback parsing support via `PythonTracebackParser`, `PythonDiagnosticClassifier`, and `PythonDiagnosticNormalizer`.
- Python traceback parser registration in infrastructure DI (`AddInsightLoggerInfrastructureParsing`).
- Python traceback sample assets (`samples/logs/python/nameerror-traceback.log`, docs example update, and golden-log case/tests).
- Python-focused test coverage across infrastructure parsing/detection and application analysis flow.
- Integrated `NpmViteParserSlice`:
- Deterministic npm/vite parsing support via `NpmDiagnosticParser`, `ViteDiagnosticParser`, `JavaScriptDiagnosticClassifier`, and `JavaScriptDiagnosticNormalizer`.
- npm and Vite parser registration in infrastructure DI (`AddInsightLoggerInfrastructureParsing`).
- npm/vite sample assets (`samples/logs/npm/npm-missing-script.log`, `samples/logs/vite/vite-resolve-import.log`, docs examples, and golden-log case/tests).
- npm/vite-focused test coverage across infrastructure parsing/detection and application analysis flow.
- Integrated `RuleRecurrenceAnalyticsSlice`:
- Added persisted rule recurrence analytics (`matchCount`, `lastMatchedAtUtc`) and rule-hit recording during analysis persistence.
- Expanded rule recurrence surfaces in contracts/mappers and enriched related-rule lookup for pattern analytics.
- Added recurrence-focused migration (`20260324011000_AddRuleMatchStats`) and API/application/infrastructure test coverage updates.
- Integrated `ScopedRuleSlice`:
- Added scoped rule conditions (`projectName`, `repository`) across rule contracts, domain condition model, application commands/DTOs, API mappings/validation, persistence model/repository logic, and deterministic matcher context.
- Added scoped-rule persistence migration (`20260324021500_AddRuleScopes`) and updated API/OpenAPI/application/infrastructure/golden-log tests.
- Integrated `AIHealthAndProviderMetadataSlice`:
- Added health/provider metadata endpoints (`GET /health`, `GET /health/ai`, `GET /providers/ai`) with API mapping and OpenAPI coverage.
- Added AI metadata query services and DTOs in application layer.
- Added configuration-driven AI provider catalog/health services and `Ai` options binding in infrastructure DI.
- Added AI contracts and API/infrastructure/application test coverage for provider catalog and health status behavior.

- Integrated `AIExplanationEnrichmentSlice`:
- Added optional AI explanation enrichment in `AnalysisService` via `IAiExplanationEnricher` and provider-routed `ConfiguredAiExplanationEnricher` implementation.
- Added AI processing metadata (`AiProcessingMetadata`) and analysis warnings to domain/contracts/API mapping and analysis responses.
- Added `Ai.Features.ExplanationEnrichment` options + appsettings defaults for provider/model/timeout/token/temperature/fallback controls.
- Added API/OpenAPI/application/infrastructure tests covering enrichment success/failure metadata projection, warnings, and provider response parsing.
- Integrated `AIFixesAndLikelyCausesSlice`:
- Expanded deterministic likely-cause and suggested-fix coverage across root-cause insights for .NET/TypeScript/Python/npm/Vite focused signatures.
- Extended AI explanation enricher payload handling to include likely causes and suggested fixes, with deterministic fallbacks when AI responses are partial.
- Updated root-cause domain/contracts/mapping projections and associated API/OpenAPI/application/infrastructure tests.
- Updated API request/response examples and endpoint docs for likely-cause behavior consistency.
- Integrated `AIRootCauseNarrativeSlice`:
- Added deterministic grouped build-log narrative generation (`AnalysisNarrative`, `AnalysisNarrativeFactory`) for multi-diagnostic logs.
- Added provider-routed AI root-cause narrative generation via `IAiRootCauseNarrativeGenerator` and `ConfiguredAiRootCauseNarrativeGenerator`.
- Extended build-log contracts/mappers/appsettings/OpenAPI coverage with `narrative` output and AI `feature` metadata for per-request provenance.
- Added application/API/OpenAPI/infrastructure tests for deterministic narrative projection and AI narrative success/failure fallback behavior.
- Integrated `AINarrativeToggleAndTaskProvenanceSlice`:
- Split grouped build-log narrative generation behind a dedicated `useAiRootCauseNarrative` request option instead of piggybacking on `useAiEnrichment`.
- Allowed build-log analysis to request primary-candidate AI enrichment and grouped narrative generation independently within the same request.
- Extended processing metadata/contracts/OpenAPI with `aiTasks` so multi-task AI provenance is explicit instead of overloading a single AI status object.
- Updated API/application tests and docs to cover separate AI toggles, combined-task requests, and per-task provenance projection.
- Integrated `PersistedAnalysisRetrievalSlice`:
- Added persisted full-analysis retrieval endpoint (`GET /analyses/{analysisId}`) backed by an internal stored analysis snapshot plus normalized-row fallback for older records.
- Added full persisted-analysis contract/query/repository flow covering diagnostics, groups, root-cause candidates, matched rules, narrative, processing metadata, warnings, context, and raw-content metadata.
- Added snapshot persistence migration (`20260324084500_AddAnalysisResultSnapshot`) and API/infrastructure/integration test coverage for snapshot-backed and legacy fallback retrieval.
- Integrated `HistoricalNarrativeSearchSlice`:
- Extended `GET /analyses/narratives` with optional `text` filtering for historical narrative search across persisted summary/group/next-step/reason/project/repository/provider/model fields.
- Added deterministic search-result match metadata (`matchedFields`, `matchSnippet`) for frontend result previews and detail-page navigation.
- Added API/OpenAPI/infrastructure test coverage and updated narrative history docs/examples for text-matching behavior.
- Integrated `KnowledgeReferencesRetrievalSlice`:
- Added knowledge-reference retrieval and projection support so analysis and narrative-history responses can include related prior analyses and known-pattern references.
- Added knowledge-reference domain/contracts/mapping/query/repository integration and supporting API/application/infrastructure coverage.
- Integrated `StructuredRedactedLoggingSlice`:
- Added structured JSON logging with request/analysis/persistence/AI correlation fields and consistent request-scope enrichment.
- Added deterministic sensitive-data redaction via `LogRedactor` and routed exception/provider error logging through redacted messages.
- Added `RequestLoggingMiddleware` and request tracing headers (`X-Correlation-Id`, `X-Request-Id`) plus operations documentation (`docs/operations/structured-redacted-logging.md`).
- Integrated `PrivacyRetentionControlsSlice`:
- Added request-level raw-content persistence control (`persistRawContent`) while keeping raw-content storage disabled unless explicitly requested.
- Added configurable `Privacy` policy options for raw-content storage enablement, write-time redaction, raw-content retention window, and persisted analysis retention window.
- Added privacy control endpoints (`GET /privacy/settings`, `POST /privacy/retention/apply`, `DELETE /analyses/{analysisId}/raw-content`, `DELETE /analyses/{analysisId}`) plus API/OpenAPI/application/infrastructure/integration test coverage.
- Added persisted raw-content redaction metadata (`rawContentRedacted`) and migration `20260324103000_AddAnalysisRawContentPrivacy`.

### Fixed
- Resolved `NpmDiagnosticParser` compile failure (`CS0136`) by renaming shadowed local variables in the missing-script parsing branch.
- Resolved `RuleService` compile failure (`CS0246`) by restoring `InsightLogger.Application.Analyses.Services` import for `DiagnosticGroupingService` and `RootCauseRankingService`.
- Resolved `AnalysisService` compile failure (`CS1503`) by passing persisted rule applications (`RuleApplicationResult`) into `AnalysisPersistenceService` and extending `RuleEvaluationResult` to carry both `Matches` and `Applications`.
- Resolved `RuleServiceRuleTestingTests` compile failure (`CS0246`) by restoring `InsightLogger.Application.Analyses.Services` import for `DiagnosticGroupingService` and `RootCauseRankingService`.
- Preserved prior rule-persistence/recurrence fixes during scoped-rule integration by keeping `AnalysisService` persistence wired to `RuleEvaluationResult.Applications`, retaining SQLite-safe client-side ordering in related-rule context lookup, and persisting direct `RecordMatchesAsync` updates with `SaveChangesAsync`.
- Resolved `RuleServiceRuleTestingTests` compile errors after scoped-rule integration by restoring analysis-service imports and replacing removed `RuleTestResultDto.Matched` assertions with `Matches`-based assertions.
- Adjusted `AnalysisServiceRuleMatchingTests.AnalyzeAsync_Respects_Project_And_Repository_Scope_From_Context` assertions to accept both diagnostic and group `RuleMatch` targets while enforcing scoped-condition match metadata on each result.




