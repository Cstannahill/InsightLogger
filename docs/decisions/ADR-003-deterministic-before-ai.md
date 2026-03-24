# ADR-003: Prefer Deterministic Analysis Before AI Enrichment

- Status: Accepted
- Date: 2026-03-23
- Deciders: Christian Tannahill
- Technical Story: Build analysis pipeline design philosophy

## Context

InsightLogger is intended to analyze build logs and compiler/runtime errors and return:

- normalized diagnostics
- grouped and deduplicated patterns
- likely root-cause candidates
- clear explanations
- suggested next steps

A major design choice is how much of this workflow should rely on AI versus deterministic code.

Many parts of the pipeline are strongly structured and should be handled by normal software engineering:

- parsing Roslyn or TypeScript diagnostics
- extracting codes, file paths, line numbers, severity
- generating stable fingerprints
- deduplicating repeated errors
- applying known rules
- ranking likely root causes with explainable heuristics

Using AI too early for these responsibilities would introduce avoidable inconsistency, lower explainability, and make testing much weaker.

## Decision

InsightLogger will use a **deterministic-first analysis pipeline**.

AI will be optional and used only for bounded enrichment tasks after deterministic parsing, normalization, grouping, and rule matching have already occurred.

## Rationale

Deterministic logic should own the parts of the workflow that are:

- structured
- testable
- repeatable
- safety-critical for correctness

AI is best reserved for tasks like:

- rewriting explanations into clearer prose
- generating more human-friendly fix guidance
- summarizing relationships between grouped diagnostics
- adapting explanations to a particular developer audience or stack context

This approach improves:

- trustworthiness
- reproducibility
- cost control
- latency control
- debuggability
- offline/local capability

## Consequences

### Positive

- Easier to test with golden logs
- Better explainability for why a result was produced
- Lower risk of hallucinated compiler facts
- Core system still works offline or without configured AI providers
- Lower operational cost for common cases

### Negative

- More engineering work upfront for parser and rule coverage
- Initial explanations may feel less polished until enrichment is added
- Some complex multi-error narratives may remain basic in v1

## Rejected Alternatives

### AI-first analysis pipeline

Rejected because:

- too inconsistent for structured compiler output
- harder to test and trust
- vulnerable to hallucination and formatting drift
- creates unnecessary provider dependence

### AI-only fallback with minimal deterministic structure

Rejected because:

- weak foundation for fingerprinting and pattern analytics
- poor reproducibility
- not acceptable for a tool meant to build a reusable knowledge base

## Implementation Notes

Deterministic logic should handle:

- tool detection
- segmentation
- parser selection
- normalization
- severity/category assignment
- fingerprint generation
- grouping/deduplication
- rule matching
- root-cause ranking heuristics

AI may handle:

- explanation rewriting
- improved suggested fixes
- grouped narrative summary
- optional developer-friendly phrasing

AI outputs must be labeled as enrichment when returned.

AI failures must not fail the whole request when deterministic analysis succeeds.

## Review Trigger

Revisit this decision if:

- supported tools become too varied for practical deterministic coverage
- AI models become necessary for a specific bounded parsing case that cannot be handled reliably otherwise
- user testing shows enrichment is essential in the base path for usability
