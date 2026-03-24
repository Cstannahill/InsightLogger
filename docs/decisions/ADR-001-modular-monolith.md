# ADR-001: Use Modular Monolith Architecture

- Status: Accepted
- Date: 2026-03-23
- Deciders: Christian Tannahill
- Technical Story: Build Error Translation API foundational architecture

## Context

InsightLogger needs to support multiple concerns:

- HTTP API exposure
- build-log and compiler-error analysis
- tool-specific parsing
- normalization and fingerprinting
- rule evaluation
- persistence and analytics
- future optional AI enrichment

The application is initially a solo-built, local-first developer tool. It needs strong internal boundaries, but it does not yet justify the operational complexity of distributed services.

If the system is split too early into multiple deployables, development speed will drop, local debugging will become worse, and cross-cutting workflows such as parsing -> grouping -> rule matching -> persistence will become harder to reason about.

## Decision

We will implement InsightLogger as a **modular monolith**.

The codebase will be split into clear internal modules/projects:

- `InsightLogger.Api`
- `InsightLogger.Application`
- `InsightLogger.Domain`
- `InsightLogger.Infrastructure`
- `InsightLogger.Contracts`

These boundaries will be enforced in code and dependency direction, but the application will run as a single deployable unit in v1.

## Rationale

This gives us:

- fast local development
- simpler debugging and tracing
- low deployment overhead
- clean separation of concerns
- easier refactoring while requirements are still changing
- a path to extract services later if one subsystem becomes independently scalable

It also matches the expected usage pattern of the first releases:

- local machine or small internal deployment
- moderate traffic
- heavy need for correctness and iteration speed over distributed scalability

## Consequences

### Positive

- Easier to build and maintain in early phases
- No distributed systems overhead in v1
- Easier end-to-end testing of the full analysis pipeline
- Cleaner local development experience
- Lower operational cost and complexity

### Negative

- All modules scale together
- Clear boundaries must be actively maintained to avoid turning into a ball of mud
- Later extraction into separate services will still require deliberate work if needed

## Rejected Alternatives

### Microservices from day one

Rejected because:

- no evidence of independent scaling needs yet
- too much operational complexity for a solo project
- hurts iteration speed
- makes local-first development worse

### Single project with all code mixed together

Rejected because:

- would quickly become hard to maintain
- parser, domain, persistence, and API concerns would couple too tightly
- future integrations and testing would become more painful

## Implementation Notes

- Keep the API layer thin.
- Keep domain types free from infrastructure concerns.
- Keep parser implementations in infrastructure.
- Keep use-case orchestration in application.
- Prefer feature folders within modules where helpful.
- Revisit architecture only if usage patterns demonstrate the need for service extraction.

## Review Trigger

Revisit this decision if:

- one subsystem needs independent scaling or deployment
- asynchronous ingestion becomes dominant
- separate teams or clients need different deployment boundaries
- analysis throughput grows beyond what a single deployable can handle comfortably
