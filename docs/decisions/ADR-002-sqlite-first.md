# ADR-002: Start with SQLite for MVP Persistence

- Status: Accepted
- Date: 2026-03-23
- Deciders: Christian Tannahill
- Technical Story: MVP persistence strategy for analyses, diagnostics, rules, and patterns

## Context

InsightLogger needs persistence for:

- analysis records
- normalized diagnostics
- grouped/fingerprinted patterns
- rule definitions
- future recurring-pattern analytics

The project is being built as a local-first developer API. Early priorities are:

- fast setup
- zero-friction local development
- easy testing
- low operational burden
- preserving the ability to evolve the schema safely

A full PostgreSQL setup would be reasonable later, especially if the app becomes multi-user, is deployed remotely, or grows into a richer analytics product. However, introducing Postgres immediately increases setup cost and slows early iteration.

## Decision

We will use **SQLite as the initial persistence provider for MVP**.

Entity Framework Core will be used so the persistence layer can later be migrated to PostgreSQL with limited application-layer impact.

## Rationale

SQLite is the right fit for the first phase because it:

- requires almost no setup
- works well for local-first tools
- is sufficient for expected MVP data volume
- supports rapid iteration and testing
- keeps the project easy to clone, run, and demo

This aligns with the immediate goal: ship a useful parser-analysis API quickly rather than optimize for production scale before there is real usage.

## Consequences

### Positive

- Very fast local startup
- No external database dependency for MVP
- Easier contributor onboarding and demo setup
- Simple test configuration
- Lower friction while schema and domain are still evolving

### Negative

- Limited concurrency compared to PostgreSQL
- Less suitable for larger analytics workloads
- Some SQL/provider behaviors may differ when migrating later
- Will eventually need migration planning if multi-user hosted usage grows

## Rejected Alternatives

### PostgreSQL from day one

Rejected because:

- adds infrastructure and setup overhead too early
- does not materially improve the first working version
- optimizes for future scale before the core model is proven

### In-memory only / no persistence for MVP

Rejected because:

- blocks pattern tracking and recurrence analytics
- makes rule and fingerprint behavior harder to validate over time
- reduces real-world usefulness

### Store everything as raw JSON files

Rejected because:

- weaker querying story
- harder to support recurring-pattern analytics cleanly
- more painful evolution once relationships matter

## Implementation Notes

- Use EF Core.
- Keep database access behind repositories or persistence abstractions where needed.
- Avoid SQLite-specific hacks in application and domain layers.
- Keep migrations clean and reviewed.
- Be explicit about fields that may grow into heavier analytics queries later.

## Migration Plan

When SQLite becomes limiting:

1. add PostgreSQL provider support
2. test schema compatibility and query behavior
3. migrate connection/configuration handling
4. validate integration tests against both providers if worthwhile
5. move hosted deployments to PostgreSQL while retaining SQLite for local dev if desired

## Review Trigger

Revisit this decision if:

- concurrent writes become common
- pattern analytics become heavy
- the app becomes multi-user or remotely hosted
- SQLite performance or locking becomes a recurring issue
