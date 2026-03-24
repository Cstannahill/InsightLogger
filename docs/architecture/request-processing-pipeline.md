# Request Processing Pipeline

## Purpose

This document describes how InsightLogger processes incoming requests from raw text input to final analysis response.

The pipeline is designed around a deterministic-first approach. Each stage has a clear responsibility, explicit inputs and outputs, and bounded failure behavior.

## Supported Request Types

Initial pipeline entry points:

- full build or tool log analysis
- single compiler/runtime diagnostic analysis

These are exposed via:

- `POST /analyze/build-log`
- `POST /analyze/compiler-error`

## Pipeline Stages

```text
Request
  -> Validation
  -> Request Mapping
  -> Tool Detection
  -> Segmentation
  -> Parsing
  -> Normalization
  -> Classification
  -> Fingerprinting
  -> Grouping / Dedupe
  -> Root Cause Ranking
  -> Rule Matching
  -> Optional AI Enrichment
  -> Persistence
  -> Response Projection
```

## Stage-by-Stage Details

### 1. Validation

#### Purpose
Reject malformed or unsafe requests early.

#### Inputs
- raw request DTO
- request metadata
- configured size/feature limits

#### Checks
- required fields present
- content is non-empty
- content length under configured limit
- tool value valid if supplied
- options valid

#### Output
A validated request model or a 4xx response.

#### Failure behavior
Stop processing. Return validation errors.

---

### 2. Request Mapping

#### Purpose
Translate transport DTOs into application-layer commands.

#### Inputs
- external request contract

#### Output
- application command/query object

#### Notes
This stage keeps API contracts independent from application internals.

---

### 3. Tool Detection

#### Purpose
Determine which tool/parser family should handle the input when the tool is not explicitly provided.

#### Inputs
- raw content
- optional tool hint

#### Signals
- compiler codes like `CS0103`, `CS8618`, `TS2304`
- traceback structure
- npm/vite formatting patterns
- known prefixes and line shapes

#### Output
- detected `ToolKind`
- detection confidence
- fallback state if unknown

#### Failure behavior
If the tool is unknown, continue with best-effort generic parsing when possible.

---

### 4. Segmentation

#### Purpose
Split a raw input blob into candidate diagnostic blocks or events.

#### Why it matters
A build log usually contains repeated lines, stack traces, summaries, warnings, and noise. Parsing works better if the system first isolates probable diagnostic units.

#### Inputs
- raw content
- detected tool kind

#### Output
- ordered list of candidate segments

#### Examples
- `.NET`: one compiler diagnostic plus continuation lines
- Python: traceback block
- `tsc`: one diagnostic line or grouped block
- generic: line windows around error-like markers

---

### 5. Parsing

#### Purpose
Convert candidate segments into normalized internal diagnostic objects.

#### Inputs
- candidate segments
- selected parser

#### Output
- zero or more `DiagnosticRecord` instances
- parser metadata such as parser name and parse confidence

#### Notes
A parser should extract structured fields whenever possible:

- code
- severity
- message
- file path
- line/column
- raw snippet

#### Failure behavior
If a segment cannot be parsed by a specific parser, the pipeline may:

- fall back to a generic parser
- preserve the segment as unparsed metadata
- continue processing the rest of the request

---

### 6. Normalization

#### Purpose
Convert parser-specific output into stable, consistent internal representation.

#### Tasks
- normalize severity labels
- normalize path separators and path formatting
- trim repeated whitespace/noise
- extract or standardize message templates
- clean tool-specific prefix noise

#### Output
- normalized `DiagnosticRecord` set

#### Importance
Normalization is the foundation for fingerprinting, grouping, and rules.

---

### 7. Classification

#### Purpose
Assign domain-level categories to diagnostics.

#### Example categories
- syntax
- missing-symbol
- type-mismatch
- nullable-safety
- dependency
- configuration
- build-system
- runtime-environment
- serialization
- test-failure

#### Output
- diagnostics with category and optional subcategory

#### Notes
Classification may combine parser-derived facts, code mappings, regex rules, and message templates.

---

### 8. Fingerprinting

#### Purpose
Generate a stable pattern signature for recurring diagnostics.

#### Inputs
- normalized message/template
- tool kind
- code
- category

#### Output
- fingerprint string/hash

#### Example
Raw messages:
- `The name 'builderz' does not exist in the current context`
- `The name 'foo' does not exist in the current context`

Normalized template:
- `The name '{identifier}' does not exist in the current context`

These should map to the same fingerprint.

#### Importance
Fingerprinting supports:
- dedupe
- recurrence tracking
- pattern lookup
- analytics
- rule reuse

---

### 9. Grouping / Deduplication

#### Purpose
Collapse repeated or cascade-related diagnostics into more useful analysis groups.

#### Inputs
- ordered diagnostics with fingerprints and categories

#### Strategies
- exact fingerprint dedupe
- repeated file/code/message collapse
- cascade grouping based on heuristics
- prioritize earliest/highest-signal representative in a group

#### Output
- `DiagnosticGroup` collection
- representative diagnostic selection

#### Example
A single broken import causing many downstream “cannot find type” errors should not be treated as many unrelated root causes.

---

### 10. Root Cause Ranking

#### Purpose
Estimate which diagnostics or groups best explain the overall failure.

#### Inputs
- normalized diagnostics
- groups
- parser metadata
- rules

#### Signals
- severity
- occurrence order
- known code priority
- cascade relationships
- repeated downstream consequences
- parser confidence
- rule boosts

#### Output
- ranked root-cause candidates with confidence

#### Design note
This should be heuristic and explainable, not magic.

---

### 11. Rule Matching

#### Purpose
Attach deterministic project- or stack-specific guidance.

#### Inputs
- diagnostics
- groups
- request metadata
- rule set

#### Possible rule conditions
- tool
- code
- severity
- category
- message regex
- file path regex
- project/repository context

#### Possible rule actions
- override/add title
- add explanation
- add suggested fixes
- boost confidence
- mark primary candidate
- attach tags

#### Output
- matched rules
- enriched findings

---

### 12. Optional AI Enrichment

#### Purpose
Improve explanation readability and practical guidance after deterministic analysis is complete.

#### Allowed uses
- clearer plain-English explanation
- better ordered next steps
- short narrative summary of grouped issues

#### Not allowed as core responsibility
- primary parsing
- extracting deterministic fields that code should extract
- inventing unsupported causal claims

#### Failure behavior
AI failure must not fail the analysis request if deterministic processing succeeded.

---

### 13. Persistence

#### Purpose
Store useful analysis artifacts for lookup, recurrence tracking, and analytics.

#### Candidate persisted artifacts
- analysis summary
- diagnostics
- groups
- matched rules
- pattern occurrences
- optional raw content hash

#### Notes
Raw content storage should be configurable due to privacy concerns.

---

### 14. Response Projection

#### Purpose
Convert internal analysis objects into stable public response contracts.

#### Output
- endpoint response DTO

#### Notes
Responses should include both human-friendly summaries and machine-readable structure.

## Pipeline Behavior Rules

### Partial success is acceptable
If part of a log cannot be parsed, the system should still return useful structured output for what it could process.

### Deterministic steps own the core truth
The system should prefer tested parser/rule output over AI-generated interpretation.

### Uncertainty should be explicit
Unknown or weakly inferred fields should be labeled as such.

### Provenance matters
Whenever practical, the response should be able to indicate whether a finding came from:
- parser extraction
- rule matching
- heuristic ranking
- AI enrichment

## Failure Modes

### Validation failure
Return 4xx.

### Unsupported format
Return best-effort analysis with low-confidence/unknown tool state when possible.

### Parser miss
Keep processing through fallback/generic paths.

### Persistence failure
Prefer returning analysis result if safe, with internal error logging.

### AI provider failure
Log the issue, omit AI enrichment, return deterministic result.

## Observability Across the Pipeline

Track at minimum:
- request count
- validation failure rate
- tool detection rate
- parser success/failure rate
- unknown segment ratio
- average diagnostics per request
- grouping ratio
- top fingerprints
- root-cause ranking distribution
- AI usage and failure rate
- end-to-end latency

## Summary

The InsightLogger pipeline is designed to turn noisy raw input into trustworthy structured output through a sequence of explicit, testable stages. The most important principle is that deterministic parsing and reasoning own the core result, while AI remains optional enrichment around a solid foundation.
