## Unreleased

### Added
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

### Fixed
- Resolved `NpmDiagnosticParser` compile failure (`CS0136`) by renaming shadowed local variables in the missing-script parsing branch.
- Resolved `RuleService` compile failure (`CS0246`) by restoring `InsightLogger.Application.Analyses.Services` import for `DiagnosticGroupingService` and `RootCauseRankingService`.
- Resolved `AnalysisService` compile failure (`CS1503`) by passing persisted rule applications (`RuleApplicationResult`) into `AnalysisPersistenceService` and extending `RuleEvaluationResult` to carry both `Matches` and `Applications`.
- Resolved `RuleServiceRuleTestingTests` compile failure (`CS0246`) by restoring `InsightLogger.Application.Analyses.Services` import for `DiagnosticGroupingService` and `RootCauseRankingService`.
- Preserved prior rule-persistence/recurrence fixes during scoped-rule integration by keeping `AnalysisService` persistence wired to `RuleEvaluationResult.Applications`, retaining SQLite-safe client-side ordering in related-rule context lookup, and persisting direct `RecordMatchesAsync` updates with `SaveChangesAsync`.
- Resolved `RuleServiceRuleTestingTests` compile errors after scoped-rule integration by restoring analysis-service imports and replacing removed `RuleTestResultDto.Matched` assertions with `Matches`-based assertions.
- Adjusted `AnalysisServiceRuleMatchingTests.AnalyzeAsync_Respects_Project_And_Repository_Scope_From_Context` assertions to accept both diagnostic and group `RuleMatch` targets while enforcing scoped-condition match metadata on each result.
