# ADR-004: Keep External API Contracts Separate from API Internals

- Status: Accepted
- Date: 2026-03-23
- Deciders: Christian Tannahill
- Technical Story: Stable request/response contract design

## Context

InsightLogger will expose HTTP endpoints for external consumers such as:

- local CLI tools
- future IDE/editor integrations
- potential web dashboard clients
- CI pipeline integrations

If request/response models live directly inside endpoint code and are reused as internal application/domain types, contract drift and accidental coupling become likely.

Examples of problems this can cause:

- leaking domain internals into public API responses
- breaking clients when internal refactors occur
- bloated or unstable DTOs
- application and infrastructure concerns influencing public contract shape

## Decision

We will keep **external API contracts in a separate contracts project/namespace**, distinct from API endpoint internals and distinct from domain models.

Public request/response DTOs will be treated as explicit external contracts.

## Rationale

This improves:

- separation of concerns
- future versioning discipline
- safer refactoring of internal domain models
- clearer API design
- easier client generation from stable OpenAPI contracts

This is especially important because the API is likely to power multiple consumers over time.

## Consequences

### Positive

- Cleaner separation between transport models and domain models
- Reduced accidental breaking changes
- Easier future API versioning
- Better long-term maintainability
- Easier reuse in typed clients or SDK generation

### Negative

- Some mapping code is required
- Slightly more up-front structure than putting DTOs directly in endpoints

## Rejected Alternatives

### Put DTOs directly in endpoint files

Rejected because:

- convenient at first, messy later
- encourages endpoint/domain coupling
- weakens contract discipline

### Reuse domain entities as API contracts

Rejected because:

- domain entities are not transport models
- internal invariants and public response shape should evolve independently
- encourages leaking implementation detail

## Implementation Notes

- Use `InsightLogger.Contracts` for public DTOs.
- Keep endpoint-specific binding models thin when needed.
- Map contracts to application commands/queries explicitly.
- Do not expose EF entities or domain entities directly in responses.
- Treat OpenAPI as a public surface, not an incidental byproduct.

## Review Trigger

Revisit this decision if:

- the project remains permanently tiny and single-consumer only
- the added mapping burden becomes disproportionate to the actual app complexity
- a different packaging strategy for contracts becomes more practical
