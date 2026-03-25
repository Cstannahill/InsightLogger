# Hybrid Knowledge Retrieval Pilot

This slice extends knowledge references with deterministic-first hybrid retrieval while keeping the existing contracts stable.

## Goals

- keep parser/grouping/ranking behavior deterministic
- enrich responses with higher-signal references from prior persisted analyses
- favor exact matches first, then bounded fuzzy similarity
- avoid introducing vector storage or embedding dependencies in this phase

## Retrieval strategy

1. Exact deterministic references
- matched rules from the current analysis context
- recurring fingerprint summaries from persisted pattern history
- official documentation links for known signatures

2. Similar persisted history (hybrid layer)
- searches persisted diagnostics in the same tool family
- scores candidates by fingerprint match, diagnostic-code match, category, and normalized-message token overlap
- returns bounded top results only

## Where references are attached

- `POST /analyze/build-log`
- `POST /analyze/compiler-error`
- `GET /errors/{fingerprint}`
- `GET /analyses/{analysisId}`
- `GET /analyses/{analysisId}/narrative`

## Notes

- this pilot is additive and non-breaking for existing contracts
- embeddings can be added later behind the same knowledge-source/repository seams
