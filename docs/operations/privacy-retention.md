# Privacy and retention controls

This slice adds bounded privacy controls for stored raw content and persisted analysis history.

## What is controlled

- whether raw content may be stored at all
- whether stored raw content is redacted before persistence
- how long stored raw content is retained
- how long persisted analysis history is retained
- manual purge of raw content for one analysis
- manual deletion of one persisted analysis

## Configuration

```json
"Privacy": {
  "RawContentStorageEnabled": true,
  "RedactRawContentOnWrite": true,
  "RawContentRetentionDays": 7,
  "AnalysisRetentionDays": 90
}
```

## Request-level raw content storage

`AnalyzeRequestOptionsContract` now supports `persistRawContent`.

Raw content is still **not** stored by default.

A request must explicitly set:
- `persist: true`
- `persistRawContent: true`

If raw-content storage is globally disabled, the request still succeeds, but no raw content is stored.

## Endpoints

- `GET /privacy/settings`
- `POST /privacy/retention/apply`
- `DELETE /analyses/{analysisId}/raw-content`
- `DELETE /analyses/{analysisId}`

## Notes

- retention purges only stored raw content first; it does not delete analyses unless `AnalysisRetentionDays` is configured
- persisted analysis replay overlays current raw-content state from the normalized analysis row, so manual purge/retention does not require rewriting historical snapshots
