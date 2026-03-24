# Request / Response Examples

## Purpose

This document contains concrete request and response examples for the main InsightLogger API workflows.

These examples are meant to help with:

- implementation
- testing
- contract review
- frontend/client integration
- sample payload generation

## Example 1: Analyze a .NET Build Log

### Request

```http
POST /analyze/build-log
Content-Type: application/json
X-Correlation-Id: corr_demo_001
```

```json
{
  "tool": "dotnet",
  "content": "Build started...
Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context
Build FAILED.",
  "projectName": "InsightLogger.Api",
  "repository": "InsightLogger",
  "branch": "main",
  "options": {
    "persist": true,
    "useAiEnrichment": false,
    "includeRawDiagnostics": true,
    "includeGroups": true,
    "includeProcessingMetadata": true
  }
}
```

### Response

```json
{
  "analysisId": "anl_01HXYZ001",
  "toolDetected": "dotnet",
  "summary": {
    "totalDiagnostics": 1,
    "groupCount": 1,
    "primaryIssueCount": 1,
    "errorCount": 1,
    "warningCount": 0
  },
  "rootCauseCandidates": [
    {
      "fingerprint": "fp_cs0103_name_missing",
      "title": "Unknown symbol in current context",
      "explanation": "The compiler cannot resolve a referenced name in the current scope.",
      "confidence": 0.96,
      "signals": [
        "diagnostic-code:CS0103",
        "category:missing-symbol"
      ],
      "likelyCauses": [
        "Typo in variable or member name",
        "Missing declaration",
        "Wrong scope or missing using/reference"
      ],
      "suggestedFixes": [
        "Check the symbol name for a typo.",
        "Verify the symbol is declared before use.",
        "Confirm the intended variable or member is available in this scope."
      ]
    }
  ],
  "groups": [
    {
      "fingerprint": "fp_cs0103_name_missing",
      "count": 1,
      "groupReason": "single-primary-diagnostic",
      "primaryDiagnosticId": "diag_01HXYZ001"
    }
  ],
  "diagnostics": [
    {
      "id": "diag_01HXYZ001",
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
    "durationMs": 32,
    "parser": "dotnet-diagnostic-parser-v1",
    "correlationId": "corr_demo_001"
  }
}
```

---

## Example 2: Analyze a Single Compiler Error

### Request

```http
POST /analyze/compiler-error
Content-Type: application/json
```

```json
{
  "tool": "dotnet",
  "content": "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
  "context": {
    "projectName": "Demo.Api"
  },
  "options": {
    "persist": true,
    "useAiEnrichment": false
  }
}
```

### Response

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
    "Missing declaration",
    "Wrong scope"
  ],
  "suggestedFixes": [
    "Check the symbol spelling.",
    "Verify the symbol is declared before use.",
    "Confirm the symbol is available in the current context."
  ],
  "confidence": 0.96,
  "matchedRules": [],
  "processing": {
    "usedAi": false,
    "durationMs": 10
  }
}
```

---

## Example 3: Analyze a TypeScript Error

### Request

```http
POST /analyze/compiler-error
Content-Type: application/json
```

```json
{
  "tool": "typescript",
  "content": "src/app.ts:5:13 - error TS2304: Cannot find name 'usre'.",
  "context": {
    "projectName": "client-app"
  },
  "options": {
    "persist": true,
    "useAiEnrichment": false
  }
}
```

### Response

```json
{
  "fingerprint": "fp_ts2304_name_missing",
  "toolDetected": "typescript",
  "diagnostic": {
    "code": "TS2304",
    "severity": "error",
    "filePath": "src/app.ts",
    "line": 5,
    "column": 13,
    "message": "Cannot find name 'usre'.",
    "normalizedMessage": "Cannot find name '{identifier}'.",
    "category": "missing-symbol"
  },
  "explanation": "TypeScript cannot resolve the referenced identifier.",
  "likelyCauses": [
    "Typo in identifier name",
    "Variable not declared",
    "Import missing or incorrect"
  ],
  "suggestedFixes": [
    "Check the identifier spelling.",
    "Verify the symbol is declared or imported.",
    "Confirm the file/module exports the expected name."
  ],
  "confidence": 0.95,
  "matchedRules": [],
  "processing": {
    "usedAi": false,
    "durationMs": 9
  }
}
```

---

## Example 4: Analyze a Python Traceback

### Request

```http
POST /analyze/build-log
Content-Type: application/json
```

```json
{
  "tool": "python",
  "content": "Traceback (most recent call last):
  File \"main.py\", line 10, in <module>
    print(value)
NameError: name 'value' is not defined",
  "projectName": "py-sample",
  "options": {
    "persist": true,
    "useAiEnrichment": false,
    "includeRawDiagnostics": true
  }
}
```

### Response

```json
{
  "analysisId": "anl_01HXYZ004",
  "toolDetected": "python",
  "summary": {
    "totalDiagnostics": 1,
    "groupCount": 1,
    "primaryIssueCount": 1,
    "errorCount": 1,
    "warningCount": 0
  },
  "rootCauseCandidates": [
    {
      "fingerprint": "fp_python_nameerror_not_defined",
      "title": "Undefined name at runtime",
      "explanation": "Python attempted to use a name that does not exist in the current scope at runtime.",
      "confidence": 0.97,
      "signals": [
        "exception-type:NameError",
        "category:missing-symbol"
      ],
      "suggestedFixes": [
        "Declare the variable before use.",
        "Check for a typo in the variable name.",
        "Verify the expected value is imported or assigned earlier."
      ]
    }
  ],
  "groups": [
    {
      "fingerprint": "fp_python_nameerror_not_defined",
      "count": 1,
      "groupReason": "single-primary-diagnostic",
      "primaryDiagnosticId": "diag_01HXYZ004"
    }
  ],
  "diagnostics": [
    {
      "id": "diag_01HXYZ004",
      "tool": "python",
      "code": "NameError",
      "severity": "error",
      "message": "name 'value' is not defined",
      "normalizedMessage": "name '{identifier}' is not defined",
      "filePath": "main.py",
      "line": 10,
      "category": "missing-symbol",
      "fingerprint": "fp_python_nameerror_not_defined",
      "isPrimaryCandidate": true
    }
  ],
  "matchedRules": [],
  "processing": {
    "usedAi": false,
    "durationMs": 18,
    "parser": "python-traceback-parser-v1"
  }
}
```

---

## Example 5: Known Fingerprint Lookup

### Request

```http
GET /errors/fp_cs0103_name_missing
```

### Response

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
    "Ensure the symbol is declared and in scope."
  ],
  "relatedRules": [
    {
      "id": "rule_01HXYZ101",
      "name": "Common missing symbol guidance"
    }
  ]
}
```

---

## Example 6: Top Patterns Query

### Request

```http
GET /patterns/top?tool=dotnet&limit=2
```

### Response

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

## Example 7: Create a Rule

### Request

```http
POST /rules
Content-Type: application/json
```

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

### Response

```json
{
  "id": "rule_01HXYZ101",
  "name": "Common CS8618 EF entity initialization",
  "isEnabled": true,
  "priority": 100,
  "createdAt": "2026-03-23T18:00:00Z"
}
```

---

## Example 8: AI-Enriched Analysis Response

### Request

```http
POST /analyze/compiler-error
Content-Type: application/json
```

```json
{
  "tool": "dotnet",
  "content": "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context",
  "options": {
    "persist": true,
    "useAiEnrichment": true
  }
}
```

### Response

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
  "explanation": "This error means the compiler found a name in your code that it cannot match to any variable, method, property, field, or type in the current scope.",
  "likelyCauses": [
    "Typo in variable or member name",
    "Missing declaration",
    "Wrong scope or missing using/reference"
  ],
  "suggestedFixes": [
    "Check whether 'builderz' was meant to be 'builder'.",
    "Verify the variable or member is declared before use.",
    "If the symbol belongs to another type or namespace, confirm the correct reference/import is present."
  ],
  "confidence": 0.96,
  "matchedRules": [],
  "processing": {
    "usedAi": true,
    "durationMs": 85,
    "ai": {
      "requested": true,
      "provider": "openrouter",
      "model": "openai/gpt-5-mini",
      "status": "completed",
      "fallbackUsed": false
    }
  }
}
```

---

## Example 9: Validation Error Response

### Request

```http
POST /analyze/build-log
Content-Type: application/json
```

```json
{
  "tool": "dotnet",
  "content": ""
}
```

### Response

Status:
- `400 Bad Request`

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
    "correlationId": "corr_demo_validation"
  }
}
```

---

## Example 10: AI Provider Degraded but Deterministic Analysis Still Succeeds

### Request

```http
POST /analyze/compiler-error
Content-Type: application/json
```

```json
{
  "tool": "typescript",
  "content": "src/app.ts:5:13 - error TS2304: Cannot find name 'usre'.",
  "options": {
    "persist": true,
    "useAiEnrichment": true
  }
}
```

### Response

```json
{
  "fingerprint": "fp_ts2304_name_missing",
  "toolDetected": "typescript",
  "diagnostic": {
    "code": "TS2304",
    "severity": "error",
    "filePath": "src/app.ts",
    "line": 5,
    "column": 13,
    "message": "Cannot find name 'usre'.",
    "normalizedMessage": "Cannot find name '{identifier}'.",
    "category": "missing-symbol"
  },
  "explanation": "TypeScript cannot resolve the referenced identifier.",
  "likelyCauses": [
    "Typo in identifier name",
    "Variable not declared",
    "Import missing or incorrect"
  ],
  "suggestedFixes": [
    "Check the identifier spelling.",
    "Verify the symbol is declared or imported.",
    "Confirm the file/module exports the expected name."
  ],
  "confidence": 0.95,
  "matchedRules": [],
  "processing": {
    "usedAi": false,
    "durationMs": 21,
    "ai": {
      "requested": true,
      "provider": "openrouter",
      "status": "degraded",
      "reason": "timeout"
    }
  },
  "warnings": [
    "AI explanation enrichment was requested but could not be completed. Deterministic analysis was returned instead."
  ]
}
```

---

## Example 11: Toggle Rule Enabled State

### Request

```http
PATCH /rules/rule_01HXYZ101/enabled
Content-Type: application/json
```

```json
{
  "isEnabled": false
}
```

### Response

```json
{
  "id": "rule_01HXYZ101",
  "isEnabled": false,
  "updatedAt": "2026-03-23T19:15:00Z"
}
```


---

## Example 12: Dry-Run an Inline Rule Against Sample Content

### Request

```http
POST /rules/test
Content-Type: application/json
```

```json
{
  "rule": {
    "name": "Common CS0103 missing symbol guidance",
    "description": "Explain common missing symbol failures.",
    "priority": 100,
    "isEnabled": true,
    "conditions": {
      "tool": "dotnet",
      "code": "CS0103",
      "category": "missing-symbol",
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

### Response

```json
{
  "matched": true,
  "rule": {
    "id": null,
    "name": "Common CS0103 missing symbol guidance",
    "isEnabled": true,
    "priority": 100,
    "isPersisted": false
  },
  "toolDetected": "dotnet",
  "diagnosticCount": 1,
  "groupCount": 1,
  "diagnostics": [
    {
      "id": "diag_01",
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
  "groups": [
    {
      "fingerprint": "fp_cs0103_name_missing",
      "count": 1,
      "groupReason": "matching fingerprint",
      "primaryDiagnosticId": "diag_01",
      "relatedDiagnosticIds": ["diag_01"]
    }
  ],
  "rootCauseCandidatesBefore": [
    {
      "fingerprint": "fp_cs0103_name_missing",
      "title": "Missing symbol or identifier",
      "explanation": "A referenced symbol could not be resolved in the current scope.",
      "confidence": 0.82,
      "signals": ["severity:error", "category:missing-symbol"],
      "suggestedFixes": []
    }
  ],
  "rootCauseCandidatesAfter": [
    {
      "fingerprint": "fp_cs0103_name_missing",
      "title": "Unknown symbol in current context",
      "explanation": "The identifier is missing or out of scope.",
      "confidence": 0.97,
      "signals": ["severity:error", "category:missing-symbol", "rule:Common CS0103 missing symbol guidance"],
      "suggestedFixes": ["Check spelling."]
    }
  ],
  "matches": [
    {
      "ruleId": "rule_test_inline",
      "ruleName": "Common CS0103 missing symbol guidance",
      "targetType": "diagnostic",
      "targetId": "diag_01",
      "matchedFingerprint": "fp_cs0103_name_missing",
      "matchedConditions": ["tool", "code", "category", "fingerprint"],
      "appliedActions": ["title", "explanation", "suggestedFixes"]
    }
  ],
  "processing": {
    "usedAi": false,
    "durationMs": 9,
    "parser": "DotNetDiagnosticParser",
    "correlationId": null,
    "toolDetectionConfidence": 0.99,
    "parseConfidence": 0.97,
    "unparsedSegmentCount": 0,
    "notes": null
  }
}
```

## Summary

These examples represent the intended shape of the API, not just happy-path payloads. They also show the expected behavior around optional AI enrichment, partial success, deterministic fallback, rules, and recurring pattern lookup.


---

## Example 14: Basic Service Health

### Request

```http
GET /health
```

### Response

```json
{
  "status": "healthy",
  "service": "InsightLogger.Api",
  "version": "0.1.0",
  "timestamp": "2026-03-24T04:10:00Z"
}
```

---

## Example 15: AI Provider Health Summary

### Request

```http
GET /health/ai
```

### Response

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

## Example 16: AI Provider Catalog

### Request

```http
GET /providers/ai
```

### Response

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

## Example 13: AI-Enriched Multi-Diagnostic Build Narrative

This example uses the dedicated `useAiRootCauseNarrative` toggle rather than the primary-candidate `useAiEnrichment` flag.

### Request

```http
POST /analyze/build-log
Content-Type: application/json
```

```json
{
  "tool": "dotnet",
  "content": "Program.cs(14,9): error CS0103: The name 'builderz' does not exist in the current context\nProgram.cs(15,9): error CS0103: The name 'servicez' does not exist in the current context",
  "options": {
    "persist": true,
    "useAiEnrichment": false,
    "useAiRootCauseNarrative": true,
    "includeRawDiagnostics": true,
    "includeGroups": true,
    "includeProcessingMetadata": true
  }
}
```

### Response

```json
{
  "analysisId": "anl_01HXYZ012",
  "toolDetected": "dotnet",
  "summary": {
    "totalDiagnostics": 2,
    "groupCount": 1,
    "primaryIssueCount": 1,
    "errorCount": 2,
    "warningCount": 0
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
        "group-count:2"
      ],
      "likelyCauses": [
        "Typo in variable or member name",
        "Missing declaration",
        "Wrong scope or missing using/reference"
      ],
      "suggestedFixes": [
        "Check the symbol spelling.",
        "Verify the symbol is declared before use."
      ]
    }
  ],
  "groups": [
    {
      "fingerprint": "fp_cs0103_name_missing",
      "count": 2,
      "groupReason": "same-fingerprint",
      "primaryDiagnosticId": "diag_01HXYZ012"
    }
  ],
  "diagnostics": [],
  "matchedRules": [],
  "narrative": {
    "summary": "The build is failing because repeated missing-symbol errors point to one unresolved identifier pattern.",
    "groupSummaries": [
      "Missing symbol issues are clustered into one repeated fingerprint group.",
      "The first group should be fixed before chasing downstream repeats."
    ],
    "recommendedNextSteps": [
      "Fix the first unresolved identifier and rebuild.",
      "Check for a typo or missing declaration in the earliest failing file."
    ],
    "source": "ai",
    "provider": "ollama",
    "model": "qwen3:8b",
    "status": "completed",
    "fallbackUsed": false
  },
  "processing": {
    "usedAi": true,
    "durationMs": 71,
    "parser": "dotnet-diagnostic-parser-v1",
    "ai": {
      "requested": true,
      "provider": "ollama",
      "model": "qwen3:8b",
      "status": "completed",
      "fallbackUsed": false,
      "feature": "root-cause-narrative"
    },
    "aiTasks": [
      {
        "requested": true,
        "provider": "ollama",
        "model": "qwen3:8b",
        "status": "completed",
        "fallbackUsed": false,
        "feature": "root-cause-narrative"
      }
    ]
  }
}
```


---

## Example: query persisted narrative history

### Request

```http
GET /analyses/narratives?repository=InsightLogger&text=nullable&limit=2
```

### Response

```json
{
  "items": [
    {
      "analysisId": "anl_7d64f4f1a2d148f5b4f9f9d49cc9cb55",
      "toolDetected": "dotnet",
      "createdAtUtc": "2026-03-24T06:58:21.0000000+00:00",
      "summary": {
        "totalDiagnostics": 4,
        "groupCount": 2,
        "primaryIssueCount": 2,
        "errorCount": 3,
        "warningCount": 1
      },
      "summaryText": "The .NET log contains 4 diagnostics, grouped into 2 likely issue clusters. The strongest starting point is unknown symbol in current context.",
      "source": "deterministic",
      "provider": null,
      "model": null,
      "status": "completed",
      "fallbackUsed": false,
      "projectName": "InsightLogger.Api",
      "repository": "InsightLogger",
      "matchedFields": [
        "summary",
        "groupSummaries",
        "recommendedNextSteps"
      ],
      "matchSnippet": "...contains a nullable warning cluster. Start with the nullable cluster before cleanup..."
    }
  ]
}
```

## Example: query a persisted narrative by analysis id

### Request

```http
GET /analyses/anl_7d64f4f1a2d148f5b4f9f9d49cc9cb55/narrative
```

### Response

```json
{
  "analysisId": "anl_7d64f4f1a2d148f5b4f9f9d49cc9cb55",
  "inputType": "build-log",
  "toolDetected": "dotnet",
  "createdAtUtc": "2026-03-24T06:58:21.0000000+00:00",
  "summary": {
    "totalDiagnostics": 4,
    "groupCount": 2,
    "primaryIssueCount": 2,
    "errorCount": 3,
    "warningCount": 1
  },
  "narrative": {
    "summary": "The .NET log contains 4 diagnostics, grouped into 2 likely issue clusters. The strongest starting point is unknown symbol in current context.",
    "groupSummaries": [
      "Unknown symbol in current context: 2 related diagnostics matched fingerprint fp_cs0103_name_missing.",
      "Non-nullable member not initialized: 1 related diagnostic matched fingerprint fp_cs8618_non_nullable_uninitialized."
    ],
    "recommendedNextSteps": [
      "Check the symbol spelling.",
      "Verify the symbol is declared before use.",
      "Initialize the non-nullable member before constructor exit."
    ],
    "source": "deterministic",
    "provider": null,
    "model": null,
    "status": "completed",
    "fallbackUsed": false,
    "reason": null
  },
  "projectName": "InsightLogger.Api",
  "repository": "InsightLogger"
}
```


## Persisted analysis retrieval

### Request

```http
GET /analyses/anl_01HXYZ004
```

### Response

```json
{
  "analysisId": "anl_01HXYZ004",
  "inputType": "build-log",
  "toolDetected": "dotnet",
  "createdAtUtc": "2026-03-24T08:45:00Z",
  "summary": {
    "totalDiagnostics": 3,
    "groupCount": 2,
    "primaryIssueCount": 2,
    "errorCount": 2,
    "warningCount": 1
  },
  "rootCauseCandidates": [
    {
      "fingerprint": "fp_cs0103_name_missing",
      "title": "Unknown symbol in current context",
      "explanation": "The compiler cannot resolve a referenced name in the current scope.",
      "confidence": 0.96,
      "signals": ["diagnostic-code:CS0103"],
      "likelyCauses": ["Typo in variable or member name"],
      "suggestedFixes": ["Check the symbol spelling."]
    }
  ],
  "groups": [
    {
      "fingerprint": "fp_cs0103_name_missing",
      "count": 2,
      "groupReason": "same-fingerprint",
      "primaryDiagnosticId": "diag_1",
      "relatedDiagnosticIds": ["diag_1", "diag_2"]
    }
  ],
  "diagnostics": [
    {
      "id": "diag_1",
      "tool": "dotnet",
      "code": "CS0103",
      "severity": "error",
      "message": "The name 'builderz' does not exist in the current context",
      "normalizedMessage": "The name '{identifier}' does not exist in the current context",
      "filePath": "Program.cs",
      "line": 14,
      "column": 9,
      "endLine": null,
      "endColumn": null,
      "category": "missing-symbol",
      "subcategory": null,
      "fingerprint": "fp_cs0103_name_missing",
      "isPrimaryCandidate": true
    }
  ],
  "matchedRules": [],
  "narrative": {
    "summary": "The .NET log contains 3 diagnostics grouped into 2 likely issue clusters.",
    "groupSummaries": ["Unknown symbol cluster."],
    "recommendedNextSteps": ["Check the symbol spelling."],
    "source": "deterministic",
    "provider": null,
    "model": null,
    "status": null,
    "fallbackUsed": false,
    "reason": null
  },
  "processing": {
    "usedAi": false,
    "durationMs": 24,
    "parser": "dotnet-diagnostic-parser-v1",
    "correlationId": "corr_01HXYZ004",
    "toolDetectionConfidence": 1.0,
    "parseConfidence": 0.98,
    "unparsedSegmentCount": 0,
    "notes": null,
    "ai": null,
    "aiTasks": []
  },
  "warnings": [],
  "context": {
    "projectName": "InsightLogger.Api",
    "repository": "InsightLogger"
  },
  "projectName": "InsightLogger.Api",
  "repository": "InsightLogger",
  "rawContentHash": "9b6f5f7d0d6d9f6bcf9e6e9f9e8d1234567890abcdef1234567890abcdef1234",
  "rawContentStored": false,
  "rawContent": null
}
```
