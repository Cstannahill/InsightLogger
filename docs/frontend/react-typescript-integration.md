# React TypeScript Frontend Integration Reference

## Purpose

This document defines a practical frontend integration shape for a **React + TypeScript** client for InsightLogger.

It is intentionally aimed at the current backend surface that already exists or has been defined by recent slices:

- `POST /analyze/build-log`
- `POST /analyze/compiler-error`
- `GET /analyses/narratives`
- `GET /analyses/{analysisId}`
- `GET /analyses/{analysisId}/narrative`
- `GET /health`
- `GET /health/ai`
- `GET /providers/ai`

This is **not** meant to be a giant enterprise frontend plan. The right first UI is a lean product/demo client that makes the backend legible and useful.

## Recommended frontend shape

Build a small, strongly-typed React app rather than a large SPA framework stack.

Recommended baseline:

- **Vite**
- **React**
- **TypeScript**
- **TanStack Router** for route structure
- **TanStack Query** for server-state fetching, caching, and invalidation
- **Zod** for runtime contract validation at API boundaries
- optional lightweight UI layer such as shadcn/ui or your own component kit

Avoid adding Redux, Zustand, or a heavy event architecture unless the UI later proves it needs it. Right now the app is mostly:

- form submission
- query/filter state
- detail-page navigation
- rendering structured backend responses

That is mostly **server state**, not global client business logic.

## Product goal for the first frontend

The first frontend should make four things easy:

1. paste a build log and analyze it
2. inspect grouped results and root-cause candidates
3. browse/search persisted narrative history
4. open a prior analysis and inspect its full stored result

That is enough to make the backend feel like a real product rather than just an API.

## Recommended route map

Use a route structure like this:

- `/`
- `/analyze`
- `/history`
- `/analyses/:analysisId`
- `/analyses/:analysisId/narrative`
- `/settings/ai` (optional but useful)

### Route purposes

#### `/`
A simple landing/dashboard page.

Show:
- quick link to analyze a log
- recent narrative history preview
- health status summary
- AI provider availability summary

#### `/analyze`
Primary working page for submitting a build log.

Show:
- build log input form
- request options
- structured result panes after submit
- action links to open persisted analysis detail when `analysisId` is returned

#### `/history`
Searchable narrative history page.

Show:
- filters
- search input
- result cards/table
- links to narrative detail and full analysis detail

#### `/analyses/:analysisId`
Full persisted analysis detail page.

Show:
- summary metrics
- narrative section if present
- root-cause candidates
- grouped diagnostics
- matched rules
- processing metadata
- warnings
- raw context/request metadata

This page should be the main “deep inspection” page.

#### `/analyses/:analysisId/narrative`
Focused narrative detail page.

Show:
- narrative summary
- grouped summaries
- recommended next steps
- provenance/source metadata
- link back to full analysis detail

This route is useful even if the full detail page also contains the narrative. It gives the history list a clean landing target.

#### `/settings/ai`
Optional settings/visibility page for local use.

Show:
- AI health response
- provider catalog
- configured defaults
- whether AI narrative and explanation paths are currently available

## Recommended page flows

## Flow 1: Analyze a build log

1. User opens `/analyze`
2. User pastes raw log content
3. User optionally sets:
   - tool hint
   - persist
   - use AI explanation enrichment
   - use AI root-cause narrative
4. User submits request to `POST /analyze/build-log`
5. UI shows a pending state
6. UI renders result sections as one analysis result page-in-place
7. If `analysisId` is present, show actions:
   - open full detail
   - open narrative detail
   - copy analysis id

### Result layout recommendation

Use a stacked layout with clear section boundaries:

1. summary banner
2. narrative panel (if present)
3. top root-cause candidates
4. grouped issues
5. raw diagnostics
6. matched rules
7. processing/provenance
8. warnings

That order matches how people actually triage failures: overview first, raw noise later.

## Flow 2: Browse/search prior narratives

1. User opens `/history`
2. Frontend loads `GET /analyses/narratives?limit=20`
3. User optionally filters by:
   - text
   - tool
   - source
   - project name
   - repository
4. Frontend refetches when filter state changes
5. User selects a result
6. Frontend navigates to either:
   - `/analyses/:analysisId/narrative` for the focused narrative page, or
   - `/analyses/:analysisId` for the full deep-dive page

### Search UX recommendation

Do not wait for a separate “search endpoint.” The current history endpoint already supports this well enough.

Use:
- debounced text input
- immediate filter changes for selects/dropdowns
- a clear “reset filters” action

Render these fields on each history row/card:
- created date/time
- tool
- summary metrics
- narrative summary text
- source badge (`deterministic` or `ai`)
- project/repository labels
- `matchedFields` when present
- `matchSnippet` when present

## Flow 3: Open full persisted analysis detail

1. User navigates from history or analyze result into `/analyses/:analysisId`
2. Frontend calls `GET /analyses/{analysisId}`
3. UI renders progressively:
   - skeleton first
   - header summary next
   - tabbed or stacked detail sections after data load

### Detail-page recommendation

Do not force everything into one giant endless dump.

Use either:
- tabs, or
- a stacked page with an in-page section nav

Recommended sections:
- Overview
- Narrative
- Root Causes
- Groups
- Diagnostics
- Rules
- Processing
- Context

## Client-side route-to-endpoint mapping

| Route | Primary API call | Secondary API call(s) |
|---|---|---|
| `/` | `GET /health` | `GET /health/ai`, `GET /analyses/narratives?limit=5` |
| `/analyze` | `POST /analyze/build-log` | `GET /providers/ai` optional |
| `/history` | `GET /analyses/narratives` | none |
| `/analyses/:analysisId` | `GET /analyses/{analysisId}` | none |
| `/analyses/:analysisId/narrative` | `GET /analyses/{analysisId}/narrative` | optional `GET /analyses/{analysisId}` link-out only |
| `/settings/ai` | `GET /health/ai` | `GET /providers/ai`, `GET /health` |

## Recommended frontend folders

A clean first-pass folder shape:

```text
src/
  app/
    router.tsx
    providers.tsx
  components/
    layout/
    common/
    analysis/
    narrative/
    history/
    health/
  features/
    analyze/
      api/
      components/
      hooks/
      types/
      utils/
    analysis-detail/
      api/
      components/
      hooks/
      types/
      utils/
    narrative-history/
      api/
      components/
      hooks/
      types/
      utils/
    narrative-detail/
      api/
      components/
      hooks/
      types/
      utils/
    ai-status/
      api/
      components/
      hooks/
      types/
  lib/
    api/
    env/
    format/
    zod/
  routes/
    index.tsx
    analyze.tsx
    history.tsx
    analyses.$analysisId.tsx
    analyses.$analysisId.narrative.tsx
    settings.ai.tsx
```

## Component/data boundaries

The main frontend mistake to avoid is mixing transport models, page-level state, and presentational rendering all together.

Keep four layers separate:

1. **transport models**: exact API response/request shapes
2. **query hooks**: fetch/cache/retry behavior
3. **view models/selectors**: page-friendly derived data
4. **presentational components**: dumb rendering pieces

## Recommended feature boundaries

### `features/analyze`
Responsible for:
- build-log request form
- submit action
- in-page result rendering for fresh analyses

Should own:
- request DTO typing
- submit mutation hook
- result-to-view-model transformation

Should not own:
- global app navigation
- narrative history search
- shared AI settings state

### `features/narrative-history`
Responsible for:
- history filters
- query-string sync
- search result list
- match snippet rendering

Should own:
- history query params type
- history query hook
- history item card/table components

### `features/analysis-detail`
Responsible for:
- full persisted analysis retrieval
- section organization
- long-form inspection layout

Should own:
- detail query hook
- section-level selectors
- tabs/section navigation state

### `features/narrative-detail`
Responsible for:
- focused grouped narrative rendering
- narrative provenance display
- narrative timeline-style presentation

Should not try to re-render the entire full analysis page.

### `features/ai-status`
Responsible for:
- provider health/status display
- provider capability metadata rendering

## Query keys

Use stable TanStack Query keys from day one.

```ts
export const queryKeys = {
  health: ['health'] as const,
  aiHealth: ['health', 'ai'] as const,
  aiProviders: ['providers', 'ai'] as const,
  narrativeHistory: (params: NarrativeHistorySearchParams) => ['analyses', 'narratives', params] as const,
  analysisDetail: (analysisId: string) => ['analyses', analysisId] as const,
  analysisNarrative: (analysisId: string) => ['analyses', analysisId, 'narrative'] as const,
};
```

Do not hide query params in ad hoc string concatenation. Keep them typed.

## Query shapes

## Build-log analysis request

Use a frontend request type like:

```ts
export type AnalyzeBuildLogRequest = {
  tool?: 'dotnet' | 'typescript' | 'python' | 'vite' | 'npm' | 'generic';
  content: string;
  projectName?: string;
  repository?: string;
  branch?: string;
  commitSha?: string;
  environment?: {
    os?: string;
    ci?: boolean;
    machineName?: string;
  };
  options?: {
    persist?: boolean;
    useAiEnrichment?: boolean;
    useAiRootCauseNarrative?: boolean;
    includeRawDiagnostics?: boolean;
    includeGroups?: boolean;
    includeProcessingMetadata?: boolean;
  };
};
```

## Narrative history query

```ts
export type NarrativeHistorySearchParams = {
  text?: string;
  tool?: 'dotnet' | 'typescript' | 'python' | 'vite' | 'npm' | 'generic';
  source?: 'deterministic' | 'ai';
  projectName?: string;
  repository?: string;
  limit?: number;
};
```

### Query-string mapping recommendation

Sync these to the URL on `/history`:
- `text`
- `tool`
- `source`
- `projectName`
- `repository`
- `limit`

That makes the page shareable and bookmarkable.

## Minimal transport types worth modeling explicitly

You do not need to model every single backend type immediately, but you should model these explicitly:

- `AnalysisSummary`
- `RootCauseCandidate`
- `DiagnosticGroup`
- `Diagnostic`
- `RuleMatch`
- `AnalysisNarrative`
- `AiProcessingTask`
- `BuildLogAnalysisResponse`
- `AnalysisNarrativeHistoryItem`
- `AnalysisNarrativeDetailResponse`
- `PersistedAnalysisDetailResponse`

## View-model transformations

Do not pass raw API objects directly into complex components.

Examples of derived frontend selectors:

- `selectPrimaryCandidate(response)`
- `selectNarrativeStatusBadge(narrative)`
- `selectTopRecommendedNextStep(narrative)`
- `selectErrorDiagnostics(diagnostics)`
- `selectWarningDiagnostics(diagnostics)`
- `selectPrimaryGroups(groups)`
- `selectAiTaskSummary(processing)`
- `selectTimelineItems(analysisDetail)`

## Timeline rendering recommendation

For history/detail rendering, a timeline can work well, but it should be a **semantic timeline**, not a fake chronological animation gimmick.

### Where a timeline makes sense

A timeline is useful on:
- narrative detail page
- full analysis detail page overview
- future historical comparison views

### Good timeline items

Build timeline items from meaning, not from every raw field.

Recommended order:

1. analysis created
2. grouped narrative source chosen
3. primary fingerprint group(s)
4. matched rules applied
5. AI task results, if any
6. recommended next steps

### Example timeline item model

```ts
export type AnalysisTimelineItem = {
  id: string;
  kind:
    | 'analysis-created'
    | 'group-summary'
    | 'primary-candidate'
    | 'rule-match'
    | 'ai-task'
    | 'recommended-step'
    | 'warning';
  title: string;
  body?: string;
  badge?: string;
  severity?: 'info' | 'warning' | 'error' | 'success';
  metadata?: Record<string, string | number | boolean | null>;
};
```

### Timeline rendering rule

Keep the timeline concise. Use it as an overview, not a replacement for the detailed sections underneath it.

## Recommended UI states

Every data page should define explicit states rather than relying on ad hoc conditionals.

## Analyze page states

- idle
- editing
- submitting
- submit-failed
- submit-succeeded-no-narrative
- submit-succeeded-with-narrative

### Analyze page empty state

Show:
- supported tools
- what gets extracted
- note that AI is optional and deterministic output still works without it

### Analyze page pending state

Show:
- disabled submit button
- progress copy like “Analyzing log”
- keep existing textarea content intact

### Analyze page error state

Separate:
- validation error
- transport/network error
- server error

Do not collapse all of these into “something went wrong.”

## History page states

- initial-loading
- loaded-empty-no-filters
- loaded-empty-with-filters
- loaded-with-results
- loading-next-filter-change
- failed

### Empty state rules

If there are no results with no filters, say there is no persisted narrative history yet.

If there are no results with filters, say no narratives matched the current filters and show a reset action.

## Detail page states

- loading
- loaded
- not-found
- failed
- loaded-from-older-fallback-data

That last state is worth exposing in the future if older persisted analyses are reconstructed from partial normalized data rather than a full snapshot.

## Request retry behavior

Recommended defaults:

- `GET` requests: retry 1–2 times on transient network failures
- `POST /analyze/build-log`: do **not** blindly auto-retry, because the user may submit large logs and cause duplicate persisted records

For analysis submission, require user intent to retry.

## Mutation handling

### After successful analysis submission

If the response includes `analysisId` and `options.persist` was true:
- show a success toast/banner
- prefetch `GET /analyses/{analysisId}`
- prefetch `GET /analyses/{analysisId}/narrative` when a narrative exists

That makes navigation feel instant.

## Health/provider surfaces

The frontend should surface AI availability honestly.

Good display blocks:
- overall health
- AI health
- enabled providers
- default model per provider
- capability badges

This is useful both for debugging and for demo credibility.

## Recommended component list

## Common/shared

- `PageShell`
- `PageHeader`
- `SectionCard`
- `EmptyState`
- `ErrorState`
- `LoadingState`
- `KeyValueList`
- `StatusBadge`
- `CopyButton`

## Analyze feature

- `AnalyzeBuildLogForm`
- `AnalyzeOptionsPanel`
- `AnalyzeResultSummary`
- `RootCauseCandidateCard`
- `DiagnosticGroupsPanel`
- `DiagnosticsTable`
- `ProcessingMetadataPanel`
- `AnalysisWarningsPanel`
- `PersistedAnalysisActions`

## Narrative history feature

- `NarrativeHistoryFilters`
- `NarrativeHistorySearchBar`
- `NarrativeHistoryList`
- `NarrativeHistoryCard`
- `NarrativeMatchMetadata`

## Narrative detail feature

- `NarrativeSummaryCard`
- `NarrativeGroupSummaryList`
- `RecommendedNextStepsList`
- `NarrativeSourcePanel`
- `AnalysisNarrativeTimeline`

## Analysis detail feature

- `AnalysisDetailHeader`
- `AnalysisDetailTabs`
- `AnalysisOverviewPanel`
- `AnalysisContextPanel`
- `AnalysisGroupsPanel`
- `AnalysisRulesPanel`
- `AnalysisAiTasksPanel`
- `AnalysisRawMetadataPanel`

## API client recommendations

Create one small API client layer in `lib/api`.

Example functions:

```ts
export async function analyzeBuildLog(input: AnalyzeBuildLogRequest): Promise<BuildLogAnalysisResponse>;
export async function getNarrativeHistory(params: NarrativeHistorySearchParams): Promise<GetAnalysisNarrativesResponse>;
export async function getAnalysisDetail(analysisId: string): Promise<PersistedAnalysisDetailResponse>;
export async function getAnalysisNarrative(analysisId: string): Promise<AnalysisNarrativeDetailResponse>;
export async function getHealth(): Promise<HealthResponse>;
export async function getAiHealth(): Promise<AiHealthResponse>;
export async function getAiProviders(): Promise<AiProvidersResponse>;
```

Keep fetch logic centralized so you can later add:
- correlation id generation
- shared error mapping
- auth headers if needed later
- logging/dev tracing

## Error mapping recommendation

Normalize backend failures into a frontend-safe error type.

```ts
export type AppApiError = {
  status: number;
  code?: string;
  message: string;
  correlationId?: string;
  details?: unknown;
};
```

That gives you better UI than random thrown fetch errors.

## UX rules worth following

- show narrative first, raw diagnostics later
- always expose `analysisId` on persisted responses
- keep search filters in the URL
- never hide whether output was deterministic or AI-generated
- never hide fallback behavior when AI failed and deterministic output was used
- prefer badges and compact metadata rows over giant prose dumps
- let the user copy fingerprints, analysis ids, and rule ids easily

## What not to build yet

Do **not** start with:
- live collaborative editing
- websocket/event-stream infrastructure
- complicated dashboard analytics
- global stores for everything
- drag-and-drop workflow builders
- a huge design system effort before the core pages exist

That is bloat right now.

## Best next implementation order

1. build `/analyze`
2. build `/history`
3. build `/analyses/:analysisId`
4. build `/analyses/:analysisId/narrative`
5. build `/settings/ai`
6. polish shared rendering and timeline presentation

That sequence matches the backend maturity and gives you a useful product almost immediately.

## Final recommendation

A React frontend is **not** overkill here.

At this point it is the right move because the backend finally has enough usable surface area to justify a real client:

- submission
- persistence
- history
- search
- detail retrieval
- AI provenance

Just keep the first version narrow, typed, and server-state driven.
