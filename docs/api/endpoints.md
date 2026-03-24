# API Endpoints

## Purpose

This document defines the HTTP API surface for InsightLogger.

The API is designed for local-first use but should remain clean and stable enough to support future consumers such as:

- CLI tools
- IDE/editor integrations
- dashboards
- CI adapters

The API favors explicit request/response contracts and deterministic behavior. AI enrichment is optional and should be clearly represented in responses when used.

## Versioning Strategy

Initial versioning can be path-based or omitted while the API is still pre-1.0. Once clients outside local/manual usage depend on the API, explicit versioning should be introduced.

Recommended future path:

- `/api/v1/...`

Examples in this document use versionless paths for simplicity.

## Content Type

Requests and responses use:

- `application/json`

## Conventions

### Correlation ID

Clients may send a correlation identifier via header.

Suggested header:

- `X-Correlation-Id`

### Timestamp format

All timestamps should be UTC ISO 8601.

### Identifiers

Use opaque identifiers for persisted entities such as analyses and rules.

### Error shape

Use a consistent error response contract for validation, application, and provider-related failures.

---

## Endpoint Summary

### Analysis
- `POST /analyze/build-log`
- `POST /analyze/compiler-error`

### Patterns and Known Errors
- `GET /errors/{fingerprint}`
- `GET /patterns/top`

### Rules
- `POST /rules`
- `GET /rules`
- `GET /rules/{id}`
- `PUT /rules/{id}`
- `PATCH /rules/{id}/enabled`
- `POST /rules/test`

### Health and Metadata
- `GET /health`
- `GET /health/ai`
- `GET /providers/ai`

### Future candidates
- `GET /analyses/{id}`
- `GET /analyses`
- `POST /analyze/build-log/stream`

---

## Analysis Endpoints

## `POST /analyze/build-log`

### Purpose
Analyze a full build or tool log and return structured diagnostics, groups, likely root causes, and optional enrichment.

### Request body

```json
{
  "tool": "dotnet",
  "content": "Build started...
Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
  "projectName": "InsightLogger.Api",
  "repository": "InsightLogger",
  "branch": "main",
  "commitSha": "abc123",
  "environment": {
    "os": "windows",
    "ci": false,
    "machineName": "DEVBOX"
  },
  "options": {
    "persist": true,
    "useAiEnrichment": false,
    "includeRawDiagnostics": true,
    "includeGroups": true,
    "includeProcessingMetadata": true
  }
}
```

### Request fields

| Field | Type | Required | Notes |
|---|---|---:|---|
| `tool` | string | No | Optional hint such as `dotnet`, `typescript`, `python`, `vite`, `npm` |
| `content` | string | Yes | Raw build/tool log content |
| `projectName` | string | No | Optional project scope metadata |
| `repository` | string | No | Optional repository name |
| `branch` | string | No | Optional branch name |
| `commitSha` | string | No | Optional commit SHA |
| `environment` | object | No | Optional environment metadata |
| `options` | object | No | Controls persistence and output detail |

### Successful response

Status:
- `200 OK`

```json
{
  "analysisId": "anl_01HXYZ...",
  "toolDetected": "dotnet",
  "summary": {
    "totalDiagnostics": 3,
    "groupCount": 1,
    "primaryIssueCount": 1,
    "errorCount": 1,
    "warningCount": 2
  },
  "rootCauseCandidates": [
    {
      "fingerprint": "fp_cs0103_name_missing",
      "title": "Unknown symbol in current context",
      "explanation": "The compiler cannot resolve a referenced name in the current scope.",
      "confidence": 0.96,
      "signals": [
        "diagnostic-code:CS0103",
        "category:missing-symbol",
        "ranked-primary:true"
      ],
      "suggestedFixes": [
        "Check for a spelling mismatch between declaration and usage.",
        "Verify the symbol is declared before use.",
        "Confirm the intended variable or member exists in the current scope."
      ]
    }
  ],
  "groups": [
    {
      "fingerprint": "fp_cs0103_name_missing",
      "count": 1,
      "groupReason": "single-primary-diagnostic",
      "primaryDiagnosticId": "diag_01HXYZ..."
    }
  ],
  "diagnostics": [
    {
      "id": "diag_01HXYZ...",
      "tool": "dotnet",
      "code": "CS0103",
      "severity": "error",
      "message": "The name 'builderz' does not exist in the current context",
      "normalizedMessage": "The name '{identifier}' does not exist in the current context",
      "filePath": "Program.cs",
      "line": 14,
      "column": 9,
      "category": "missing-symbol",
      "fingerprint": "fp_cs0103_name_missing",
      "isPrimaryCandidate": true
    }
  ],
  "matchedRules": [],
  "processing": {
    "usedAi": false,
    "durationMs": 34,
    "parser": "dotnet-diagnostic-parser-v1",
    "correlationId": "corr_123"
  }
}
```

### Failure responses

#### Validation failure
Status:
- `400 Bad Request`

#### Payload too large
Status:
- `413 Payload Too Large`

#### Unsupported media type
Status:
- `415 Unsupported Media Type`

#### Internal failure
Status:
- `500 Internal Server Error`

---

## `POST /analyze/compiler-error`

### Purpose
Analyze a single compiler/runtime diagnostic or compact error block.

### Request body

```json
{
  "tool": "dotnet",
  "content": "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
  "context": {
    "projectName": "Demo.Api",
    "repository": "DemoRepo"
  },
  "options": {
    "persist": true,
    "useAiEnrichment": false
  }
}
```

### Successful response

Status:
- `200 OK`

```json
{
  "fingerprint": "fp_cs0103_name_missing",
  "toolDetected": "dotnet",
  "diagnostic": {
    "code": "CS0103",
    "severity": "error",
    "filePath": "Program.cs",
    "line": 14,
    "column": 9,
    "message": "The name 'builderz' does not exist in the current context",
    "normalizedMessage": "The name '{identifier}' does not exist in the current context",
    "category": "missing-symbol"
  },
  "explanation": "A symbol is being referenced that the compiler cannot resolve in the current scope.",
  "likelyCauses": [
    "Typo in variable or member name",
    "Symbol declared in a different scope",
    "Missing using, reference, or declaration"
  ],
  "suggestedFixes": [
    "Check for a spelling mismatch.",
    "Verify the variable or member exists before use.",
    "Confirm namespace/import/reference setup if the symbol should come from elsewhere."
  ],
  "confidence": 0.96,
  "matchedRules": [],
  "processing": {
    "usedAi": false,
    "durationMs": 11
  }
}
```

### Failure responses

- `400 Bad Request`
- `413 Payload Too Large`
- `500 Internal Server Error`

---

## Pattern and Known Error Endpoints

## `GET /errors/{fingerprint}`

### Purpose
Return details about a known diagnostic fingerprint or recurring pattern.

### Route parameters

| Name | Type | Required | Notes |
|---|---|---:|---|
| `fingerprint` | string | Yes | Stable diagnostic fingerprint |

### Successful response

Status:
- `200 OK`

```json
{
  "fingerprint": "fp_cs0103_name_missing",
  "title": "Unknown symbol in current context",
  "tool": "dotnet",
  "category": "missing-symbol",
  "canonicalMessage": "The name '{identifier}' does not exist in the current context",
  "occurrenceCount": 38,
  "firstSeenAt": "2026-03-23T10:00:00Z",
  "lastSeenAt": "2026-03-24T15:21:00Z",
  "knownFixes": [
    "Check spelling of the symbol.",
    "Ensure the symbol exists and is in scope."
  ],
  "relatedRules": [
    {
      "id": "rule_01HXYZ...",
      "name": "Common missing symbol guidance"
    }
  ]
}
```

### Failure responses

#### Not found
Status:
- `404 Not Found`

---

## `GET /patterns/top`

### Purpose
Return the most frequent error patterns over a time range or filter set.

### Query parameters

| Name | Type | Required | Notes |
|---|---|---:|---|
| `tool` | string | No | Filter by tool kind |
| `category` | string | No | Filter by category |
| `from` | datetime | No | Inclusive UTC lower bound |
| `to` | datetime | No | Inclusive UTC upper bound |
| `limit` | int | No | Default 20, max should be constrained |

### Successful response

Status:
- `200 OK`

```json
{
  "items": [
    {
      "fingerprint": "fp_cs0103_name_missing",
      "title": "Unknown symbol in current context",
      "tool": "dotnet",
      "category": "missing-symbol",
      "occurrenceCount": 38,
      "lastSeenAt": "2026-03-24T15:21:00Z"
    },
    {
      "fingerprint": "fp_cs8618_non_nullable_uninitialized",
      "title": "Non-nullable member not initialized",
      "tool": "dotnet",
      "category": "nullable-safety",
      "occurrenceCount": 21,
      "lastSeenAt": "2026-03-24T12:01:00Z"
    }
  ]
}
```

---

## Rule Endpoints

## `POST /rules`

### Purpose
Create a new custom rule.

### Request body

```json
{
  "name": "Common CS8618 EF entity initialization",
  "description": "Explain CS8618 warnings for EF entities that are initialized by the ORM.",
  "priority": 100,
  "isEnabled": true,
  "conditions": {
    "tool": "dotnet",
    "code": "CS8618"
  },
  "actions": {
    "title": "Non-nullable member not initialized",
    "explanation": "This warning often appears when the compiler cannot see runtime initialization patterns.",
    "suggestedFixes": [
      "Initialize the property.",
      "Mark it nullable if appropriate.",
      "Use required/init patterns where supported."
    ]
  },
  "tags": ["dotnet", "nullable", "efcore"]
}
```

### Successful response

Status:
- `201 Created`

```json
{
  "id": "rule_01HXYZ...",
  "name": "Common CS8618 EF entity initialization",
  "isEnabled": true,
  "priority": 100,
  "createdAt": "2026-03-23T18:00:00Z"
}
```

### Failure responses

- `400 Bad Request`
- `409 Conflict` when duplicate rule semantics are disallowed

---

## `GET /rules`

### Purpose
List rules, optionally filtered.

### Query parameters

| Name | Type | Required | Notes |
|---|---|---:|---|
| `enabled` | bool | No | Filter enabled/disabled |
| `tool` | string | No | Filter by tool |
| `tag` | string | No | Filter by tag |
| `limit` | int | No | Default 50 |
| `offset` | int | No | Default 0 |

### Successful response

Status:
- `200 OK`

```json
{
  "items": [
    {
      "id": "rule_01HXYZ...",
      "name": "Common CS8618 EF entity initialization",
      "description": "Explain CS8618 warnings for EF entities that are initialized by the ORM.",
      "isEnabled": true,
      "priority": 100,
      "tags": ["dotnet", "nullable", "efcore"],
      "updatedAt": "2026-03-23T18:00:00Z"
    }
  ],
  "total": 1
}
```

---

## `GET /rules/{id}`

### Purpose
Return one rule by identifier.

### Successful response

Status:
- `200 OK`

```json
{
  "id": "rule_01HXYZ...",
  "name": "Common CS8618 EF entity initialization",
  "description": "Explain CS8618 warnings for EF entities that are initialized by the ORM.",
  "priority": 100,
  "isEnabled": true,
  "conditions": {
    "tool": "dotnet",
    "code": "CS8618"
  },
  "actions": {
    "title": "Non-nullable member not initialized",
    "explanation": "This warning often appears when the compiler cannot see runtime initialization patterns.",
    "suggestedFixes": [
      "Initialize the property.",
      "Mark it nullable if appropriate.",
      "Use required/init patterns where supported."
    ]
  },
  "tags": ["dotnet", "nullable", "efcore"],
  "createdAt": "2026-03-23T18:00:00Z",
  "updatedAt": "2026-03-23T18:00:00Z"
}
```

### Failure responses

- `404 Not Found`

---

## `PUT /rules/{id}`

### Purpose
Replace an existing rule definition.

### Successful response

Status:
- `200 OK`

### Failure responses

- `400 Bad Request`
- `404 Not Found`

---

## `PATCH /rules/{id}/enabled`

### Purpose
Enable or disable a rule without replacing the whole document.

### Request body

```json
{
  "isEnabled": false
}
```

### Successful response

Status:
- `200 OK`

```json
{
  "id": "rule_01HXYZ...",
  "isEnabled": false,
  "updatedAt": "2026-03-23T19:15:00Z"
}
```

---

## `POST /rules/test`

### Purpose
Dry-run a saved rule or inline draft rule against sample content without creating or mutating any persisted analysis records.

### Request body

```json
{
  "rule": {
    "name": "Common CS0103 missing symbol guidance",
    "description": "Explain common CS0103 failures.",
    "priority": 100,
    "isEnabled": true,
    "conditions": {
      "tool": "dotnet",
      "code": "CS0103",
      "fingerprint": "fp_cs0103_name_missing"
    },
    "actions": {
      "title": "Unknown symbol in current context",
      "explanation": "The identifier is missing or out of scope.",
      "suggestedFixes": ["Check spelling."]
    },
    "tags": ["dotnet", "compiler"]
  },
  "tool": "dotnet",
  "inputType": "compiler-error",
  "content": "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context"
}
```

`ruleId` may be sent instead of `rule`. Exactly one of them is required.

### Successful response

Status:
- `200 OK`

Response includes:
- whether the rule matched
- parsed diagnostics and groups
- root-cause candidates before and after rule application
- detailed rule match targets and matched conditions
- parsing/tool-detection processing metadata

### Failure responses

- `400 Bad Request`
- `404 Not Found` when `ruleId` does not resolve

---

## Health and Provider Endpoints

## `GET /health`

### Purpose
Return basic service health.

### Successful response

Status:
- `200 OK`

```json
{
  "status": "healthy",
  "service": "InsightLogger.Api",
  "version": "0.1.0",
  "timestamp": "2026-03-23T19:20:00Z"
}
```

---

## `GET /health/ai`

### Purpose
Return AI subsystem/provider health information.

### Successful response

Status:
- `200 OK`

```json
{
  "enabled": true,
  "providers": [
    {
      "name": "ollama",
      "status": "healthy",
      "defaultModel": "qwen3:8b",
      "reason": "Configuration is ready."
    },
    {
      "name": "openrouter",
      "status": "unconfigured",
      "defaultModel": "openai/gpt-5-mini",
      "reason": "API key is missing."
    }
  ]
}
```

---

## `GET /providers/ai`

### Purpose
Return configured AI providers and normalized capability metadata.

### Successful response

Status:
- `200 OK`

```json
{
  "items": [
    {
      "name": "ollama",
      "type": "Ollama",
      "enabled": true,
      "defaultModel": "qwen3:8b",
      "capabilities": {
        "supportsStreaming": true,
        "supportsToolCalling": true,
        "supportsJsonMode": true,
        "supportsOpenAiCompatibility": false
      }
    },
    {
      "name": "openrouter",
      "type": "OpenRouter",
      "enabled": true,
      "defaultModel": "openai/gpt-5-mini",
      "capabilities": {
        "supportsStreaming": true,
        "supportsToolCalling": true,
        "supportsJsonMode": true,
        "supportsOpenAiCompatibility": true
      }
    }
  ]
}
```

---

## Standard Error Response

### Shape

```json
{
  "error": {
    "code": "validation_failed",
    "message": "One or more validation errors occurred.",
    "details": [
      {
        "field": "content",
        "message": "Content is required."
      }
    ],
    "correlationId": "corr_123"
  }
}
```

### Suggested error codes
- `validation_failed`
- `not_found`
- `payload_too_large`
- `unsupported_media_type`
- `unsupported_tool`
- `ai_provider_unavailable`
- `internal_error`

## Response Design Notes

### Human + machine readable
Responses should balance immediate usability and structured downstream consumption.

### AI provenance
If AI enrichment is used, the response should indicate:
- whether AI was used
- provider name
- model name
- whether fallback occurred

### Partial success
The API should prefer partial structured success over total failure when deterministic analysis can still produce useful output.

## Future Endpoint Candidates

These are likely useful once the core API settles:

- `GET /analyses/{id}` for retrieval of persisted analyses
- `GET /analyses` for listing/searching prior analyses
- `DELETE /analyses/{id}` for retention control
- `POST /analyze/build-log/stream` for incremental analysis workflows

## Summary

The InsightLogger API should stay small, explicit, and stable. The core endpoints are analysis, rules, patterns, and health/provider metadata. That is enough to support immediate local use while still leaving room for editor, UI, and CI integrations later.
