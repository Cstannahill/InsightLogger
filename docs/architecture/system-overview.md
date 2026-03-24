# System Overview

## Purpose

InsightLogger is a developer-focused Build Error Translation API that accepts raw build and compiler output, normalizes it into structured diagnostics, identifies likely root causes, deduplicates noise, and returns human-readable guidance.

The system is designed to help a developer answer the real question behind a failed build:

- What actually went wrong?
- Which diagnostics matter most?
- What should I try next?
- Have I seen this problem before?

## Core Product Shape

InsightLogger is a **local-first modular monolith** with optional AI enrichment.

The core of the system is deterministic:

- tool detection
- log segmentation
- parser selection
- normalization
- classification
- fingerprinting
- grouping/deduplication
- root-cause ranking
- rule matching

AI is optional and only used after deterministic analysis to improve explanation quality.

## Primary Responsibilities

The system is responsible for:

- receiving raw logs or single diagnostics
- parsing known tool output formats
- producing normalized diagnostic records
- grouping repeated/cascading errors
- ranking likely primary issues
- attaching rules-based explanations and fix guidance
- persisting analyses, patterns, and rules
- exposing structured HTTP endpoints for external consumers

The system is not responsible in v1 for:

- directly fixing source code
- replacing compiler/static-analysis tooling
- acting as a full CI platform
- deeply understanding every language/tooling ecosystem from day one

## Main Consumers

Initial consumers:

- local CLI tools
- manual curl/Postman/API testing
- future local web UI

Likely future consumers:

- VS Code extension
- CI pipeline adapters
- build hooks
- dashboard/reporting tools

## High-Level Architecture

```text
Clients
  - CLI
  - IDE integration
  - CI pipeline
  - local UI
        |
        v
InsightLogger.Api
  - endpoints
  - middleware
  - transport contracts
        |
        v
InsightLogger.Application
  - use-case orchestration
  - validation
  - coordination of analysis pipeline
        |
        v
InsightLogger.Domain
  - diagnostics
  - analyses
  - rules
  - patterns
  - core invariants
        |
        v
InsightLogger.Infrastructure
  - parsers
  - persistence
  - fingerprinting
  - rule execution
  - AI enrichment providers
  - telemetry
```

## Request Flow Summary

A typical request flows through the system like this:

1. client submits raw build log or single diagnostic
2. API validates and maps request into an application command
3. application coordinates tool detection and parsing
4. normalized diagnostics are classified and fingerprinted
5. related/repeated diagnostics are grouped
6. likely primary issues are ranked
7. matching rules are applied
8. optional AI enrichment runs
9. results are optionally persisted
10. response is projected into external contract DTOs

## Key Internal Concepts

### DiagnosticRecord
A normalized representation of one compiler/build/runtime diagnostic.

### DiagnosticGroup
A grouping of repeated or strongly related diagnostics.

### AnalysisResult
The full result of analyzing a log or single error input.

### Fingerprint
A stable signature representing a recurring diagnostic pattern.

### Rule
A declarative pattern match that adds project- or stack-specific guidance.

### Pattern
An aggregated historical view of a recurring fingerprint over time.

## Design Principles

### Deterministic before AI
Known structured data should be handled with software, not guesswork.

### Explainable results
The system should be able to explain why it produced a result.

### Local-first usability
The core pipeline should work without external AI providers.

### Extensible parser model
New tool/language parsers should be addable without destabilizing the core model.

### Stable contracts
Public request/response models should be explicit and versionable.

## Deployment Shape

### MVP deployment

Single process:

- ASP.NET Core API
- EF Core
- SQLite database
- optional AI provider configuration

### Future deployment options

- hosted API with PostgreSQL
- separate web UI
- CLI distribution
- editor plugin integration
- CI adapters

## Cross-Cutting Concerns

### Persistence
Used for analyses, diagnostics, groups, patterns, and rules.

### Logging and tracing
Used for request correlation, debugging, and parser reliability metrics.

### Configuration
Used for persistence provider selection, AI provider configuration, limits, and feature flags.

### Security and privacy
Logs may contain file paths, URLs, or secrets; redaction and retention policy must be considered.

## Architectural Boundaries

### API Layer
Owns transport and HTTP concerns only.

### Application Layer
Owns use-case orchestration and workflow coordination.

### Domain Layer
Owns the language of the problem and stable internal concepts.

### Infrastructure Layer
Owns implementations for parsing, storage, AI, telemetry, and external integrations.

## Future Growth Areas

The current design intentionally leaves room for:

- broader parser coverage
- richer rule authoring
- pattern analytics dashboards
- project-specific pattern models
- AI-assisted explanation tuning
- repository-aware or code-context-aware diagnostics

## Summary

InsightLogger is deliberately shaped as a pragmatic developer tool first: deterministic, locally useful, testable, and extensible. The architecture is designed to make parsing and diagnosis trustworthy now, while leaving room for richer integrations and AI-assisted workflows later.
