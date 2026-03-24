# Module Responsibilities

## Purpose

This document defines the responsibilities and boundaries of each major project/module in the InsightLogger solution.

The goal is to keep the modular monolith clean. Strong boundaries now will prevent the codebase from collapsing into a tightly coupled mess later.

## Solution Modules

```text
src/
  InsightLogger.Api/
  InsightLogger.Application/
  InsightLogger.Domain/
  InsightLogger.Infrastructure/
  InsightLogger.Contracts/
```

## Dependency Direction

The intended dependency direction is:

```text
Api -> Application -> Domain
                  -> Infrastructure (through DI/composition)
Contracts -> consumed by Api and clients
Infrastructure -> depends on Application abstractions and Domain
```

More concretely:

- `InsightLogger.Api` depends on `InsightLogger.Application` and `InsightLogger.Contracts`
- `InsightLogger.Application` depends on `InsightLogger.Domain`
- `InsightLogger.Infrastructure` depends on `InsightLogger.Application` abstractions and `InsightLogger.Domain`
- `InsightLogger.Domain` depends on nothing application-specific or infrastructure-specific
- `InsightLogger.Contracts` should not depend on domain or infrastructure

## Module-by-Module Responsibilities

### InsightLogger.Api

#### Owns
- HTTP endpoints
- route definitions
- middleware
- exception-to-response mapping
- request binding
- response formatting
- OpenAPI/Swagger exposure
- authentication/authorization later if added

#### Does not own
- parsing logic
- fingerprinting algorithms
- rule evaluation internals
- database access logic
- domain business rules

#### Design rules
- keep endpoints thin
- map request DTOs to application commands/queries
- map application results to response DTOs
- do not put business logic into endpoints

---

### InsightLogger.Application

#### Owns
- use-case orchestration
- analysis workflow coordination
- validation beyond simple transport validation where needed
- command/query handlers or equivalent services
- mapping between contracts and domain operations
- abstraction interfaces for infrastructure dependencies

#### Does not own
- HTTP concerns
- database provider specifics
- parser implementation details
- EF entities/configuration
- AI provider SDK details

#### Design rules
- application layer should express the workflow of the system
- it can coordinate many services, but should not contain low-level infrastructure code
- define interfaces for parser selection, repositories, AI enrichment, rule loading, etc.

---

### InsightLogger.Domain

#### Owns
- domain vocabulary
- entities and value objects
- enums and result types
- invariants for core concepts
- stable internal meaning of diagnostics, analyses, rules, and patterns

#### Does not own
- persistence attributes tied to infrastructure-only concerns when avoidable
- HTTP serialization concerns
- parser engine code
- database queries
- external SDK integration

#### Design rules
- keep it clean and boring
- prioritize clarity and correctness over cleverness
- domain types should remain usable even if transport or storage changes

---

### InsightLogger.Infrastructure

#### Owns
- persistence implementations
- EF Core DbContext and configurations
- parser implementations by tool family
- fingerprint generation implementation
- rule evaluation engine implementation
- AI provider integrations
- telemetry/logging integrations where appropriate
- caching or storage specifics if added

#### Does not own
- endpoint definitions
- public API contracts
- high-level use-case decisions that belong in application
- core domain meaning

#### Design rules
- infrastructure should implement interfaces defined by the application layer
- external systems and libraries are adapted here, not leaked upward
- parser families should be organized clearly by tool/language

---

### InsightLogger.Contracts

#### Owns
- public request DTOs
- public response DTOs
- shared transport enums if exposed externally
- stable contract types for generated clients or future SDK usage

#### Does not own
- internal domain entities
- endpoint routing
- persistence models
- parser-specific implementation detail

#### Design rules
- treat contract changes as public API changes
- do not casually reuse domain entities as public contracts
- keep contracts explicit and versionable

## Suggested Internal Organization by Module

### Api

```text
InsightLogger.Api/
  Endpoints/
  Middleware/
  DependencyInjection/
  Extensions/
  Program.cs
```

### Application

```text
InsightLogger.Application/
  Abstractions/
  Analyses/
    Commands/
    Queries/
    Services/
    DTOs/
  Diagnostics/
  Rules/
  Patterns/
  Common/
    Validation/
    Mapping/
```

### Domain

```text
InsightLogger.Domain/
  Diagnostics/
  Analyses/
  Rules/
  Patterns/
  Common/
```

### Infrastructure

```text
InsightLogger.Infrastructure/
  Persistence/
  Parsing/
    DotNet/
    TypeScript/
    JavaScript/
    Python/
    Detection/
  Fingerprinting/
  Rules/
  AI/
  Telemetry/
  DependencyInjection/
```

### Contracts

```text
InsightLogger.Contracts/
  Analyses/
  Diagnostics/
  Rules/
  Patterns/
  Common/
```

## Cross-Cutting Responsibilities

Some concerns span multiple modules but must still have clear ownership.

### Validation
- transport validation: API/contracts boundary
- use-case validation: application layer
- domain invariants: domain layer

### Mapping
- request DTO -> application command: API/application boundary
- domain result -> response DTO: API/application boundary
- infrastructure entity -> domain model: infrastructure boundary

### Logging
- request logging/middleware: API
- workflow logging: application
- provider/parser/storage details: infrastructure

### Configuration
- bind configuration at startup in API/composition root
- consume typed options in infrastructure/application services as needed

## Boundary Violations to Avoid

### Bad pattern: endpoint owns parser logic
This couples transport directly to low-level implementation.

### Bad pattern: domain entity returned directly as API response
This makes internal refactoring dangerous.

### Bad pattern: application layer calling EF-specific query APIs everywhere
This leaks persistence details upward.

### Bad pattern: infrastructure deciding business-level root-cause ranking policy
That belongs in the application/domain workflow.

## Practical Rules for This Project

1. Endpoints should mostly map input, call application, return output.
2. Parsers belong in infrastructure.
3. Root-cause orchestration belongs in application.
4. Core diagnostic concepts belong in domain.
5. Request/response DTOs belong in contracts.
6. EF Core should not shape the whole architecture.
7. AI provider details must stay behind abstractions.

## Summary

These module boundaries are meant to keep InsightLogger easy to change while it grows. The system should feel like one coherent application, but the code should still make it obvious where responsibilities live and where future additions belong.
