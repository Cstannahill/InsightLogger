# Domain Model

## Purpose

This document defines the core domain concepts used inside InsightLogger.

The domain model exists to give the system a stable internal language independent of:

- HTTP transport details
- database schema details
- parser-specific formatting quirks
- AI provider behavior

The goal is that all supported tool outputs can be translated into a common set of concepts that the rest of the system can reason about consistently.

## Core Concepts

### DiagnosticRecord

A `DiagnosticRecord` is the normalized internal representation of a single diagnostic extracted from raw input.

Examples of source inputs:
- a Roslyn compiler error
- a TypeScript compile error
- a Python traceback-derived failure point
- a Vite or npm toolchain error

#### Typical fields
- `Id`
- `ToolKind`
- `Source`
- `Code`
- `Severity`
- `Message`
- `NormalizedMessage`
- `FilePath`
- `Line`
- `Column`
- `EndLine`
- `EndColumn`
- `RawSnippet`
- `Category`
- `Subcategory`
- `Fingerprint`
- `IsPrimaryCandidate`
- `Metadata`

#### Notes
This is the core unit that downstream grouping, ranking, and pattern analysis operate on.

---

### DiagnosticGroup

A `DiagnosticGroup` represents a set of diagnostics that should be treated together for analysis purposes.

This is useful for:
- exact repeats
- strong near-duplicates
- cascades caused by one underlying issue

#### Typical fields
- `Fingerprint`
- `Count`
- `PrimaryDiagnosticId`
- `RelatedDiagnosticIds`
- `GroupReason`

#### Example
A missing reference causing many “type not found” errors across files may become one group with one representative root-cause candidate.

---

### AnalysisResult

An `AnalysisResult` represents the full outcome of processing one user-submitted request.

#### Typical fields
- `AnalysisId`
- `InputType`
- `ToolDetected`
- `Summary`
- `Diagnostics`
- `Groups`
- `RootCauseCandidates`
- `MatchedRules`
- `Explanations`
- `SuggestedFixes`
- `Confidence`
- `ProcessingMetadata`

#### Notes
This is the main aggregate returned by analysis workflows.

---

### RootCauseCandidate

A `RootCauseCandidate` is a ranked explanation target that the system believes is one of the best explanations for the overall failure.

#### Typical fields
- `Fingerprint`
- `DiagnosticId` or `GroupId`
- `Title`
- `Explanation`
- `Confidence`
- `SuggestedFixes`
- `Signals`

#### Notes
A request may yield more than one plausible root cause, especially for noisy or partial logs.

---

### Rule

A `Rule` is a declarative description of a known pattern and the guidance that should be attached when it matches.

#### Typical fields
- `Id`
- `Name`
- `Description`
- `IsEnabled`
- `Priority`
- `Scope`
- `Conditions`
- `Actions`
- `Tags`
- `CreatedAt`
- `UpdatedAt`

#### Notes
Rules allow the system to encode known stack-specific behavior without relying on AI.

---

### RuleMatch

A `RuleMatch` records the fact that a rule matched a diagnostic, group, or analysis context.

#### Typical fields
- `RuleId`
- `TargetType`
- `TargetId`
- `MatchedConditions`
- `AppliedActions`
- `AppliedAt`

---

### ErrorPattern

An `ErrorPattern` is the historical aggregate view of a recurring fingerprint.

#### Typical fields
- `Fingerprint`
- `Title`
- `CanonicalMessage`
- `ToolKind`
- `Category`
- `FirstSeenAt`
- `LastSeenAt`
- `OccurrenceCount`
- `LastSuggestedFix`

#### Notes
A pattern is not just one occurrence. It represents recurring knowledge built over time.

---

### PatternOccurrence

A `PatternOccurrence` links a specific analysis or diagnostic instance to a broader recurring pattern.

#### Typical fields
- `Id`
- `Fingerprint`
- `AnalysisId`
- `DiagnosticId`
- `SeenAt`

## Supporting Value Types

### Severity
Represents how serious a diagnostic is.

Suggested values:
- `Info`
- `Warning`
- `Error`
- `Fatal`
- `Unknown`

### ToolKind
Represents the tool family that produced or best matches the input.

Suggested values:
- `Unknown`
- `DotNet`
- `TypeScript`
- `Npm`
- `Vite`
- `Python`
- `Generic`

### DiagnosticCategory
Represents the semantic category of the issue.

Suggested values:
- `Unknown`
- `Syntax`
- `MissingSymbol`
- `TypeMismatch`
- `NullableSafety`
- `Dependency`
- `Configuration`
- `BuildSystem`
- `RuntimeEnvironment`
- `Serialization`
- `TestFailure`

### InputType
Represents what kind of submission the analysis processed.

Suggested values:
- `BuildLog`
- `SingleDiagnostic`

## Relationships Between Concepts

```text
AnalysisResult
  contains many DiagnosticRecord
  contains many DiagnosticGroup
  contains many RootCauseCandidate
  may contain many RuleMatch

DiagnosticRecord
  has one Fingerprint
  may belong to one DiagnosticGroup
  may contribute to one ErrorPattern over time

ErrorPattern
  has many PatternOccurrence

Rule
  may produce many RuleMatch
```

## Conceptual Lifecycle

### Diagnostic lifecycle
1. raw text segment is parsed
2. parser produces a `DiagnosticRecord`
3. normalization updates message/path/severity consistency
4. classification assigns category
5. fingerprinting assigns stable signature
6. grouping may attach the record to a `DiagnosticGroup`
7. ranking may mark it as a root-cause candidate
8. persistence may link it to an `ErrorPattern`

## Invariants and Constraints

### DiagnosticRecord invariants
- must have a tool kind, even if `Unknown`
- must have a message or raw snippet
- severity should never be null
- normalized message should be derived if possible before fingerprinting

### DiagnosticGroup invariants
- must have at least one related diagnostic
- should have one representative/primary diagnostic

### Rule invariants
- must have at least one condition or explicit scope trigger
- must define at least one action to be useful
- disabled rules must not apply

### ErrorPattern invariants
- occurrence count must be non-negative
- first seen cannot be after last seen

## Domain Modeling Principles

### Tool-specific input, tool-agnostic core
The system may parse many formats, but they must converge into one internal model.

### Fingerprint is first-class
Fingerprinting is central because it powers recurrence tracking, dedupe, and rule reuse.

### The domain should stay clean
Do not distort the model just to match one transport format or one database shortcut.

### Uncertainty is allowed
The domain should allow unknown or low-confidence states without pretending certainty.

## Example Mapping

### Example input
```text
Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context
```

### Example domain interpretation
- `ToolKind`: `DotNet`
- `Severity`: `Error`
- `Code`: `CS0103`
- `FilePath`: `Program.cs`
- `Line`: `14`
- `Column`: `9`
- `Category`: `MissingSymbol`
- `NormalizedMessage`: `The name '{identifier}' does not exist in the current context`
- `Fingerprint`: stable hash over tool + code + normalized template + category

## Summary

The domain model is the backbone of InsightLogger. Everything else—HTTP contracts, parser implementations, persistence schema, AI enrichment—should adapt to it rather than define it. If the domain model remains clean and consistent, the rest of the system will stay much easier to evolve.
