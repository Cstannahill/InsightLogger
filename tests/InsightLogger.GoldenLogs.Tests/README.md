# Golden Logs Test Harness

Golden-log tests are the long-term regression backbone for InsightLogger.

The planning docs explicitly call them one of the most important test assets. The harness in this slice
uses **assertion-oriented case files** instead of full response snapshots.

That is the right tradeoff here:

- strict on fingerprints, categories, codes, and summary counts
- tolerant of harmless evolution like different IDs, extra notes, or better wording
- reusable across API, application, and parser improvements

## Case layout

```text
tests/InsightLogger.GoldenLogs.Tests/
  Cases/
    dotnet/
      cs0103-single-diagnostic.case.json
      build-failed-mixed.case.json
```

## Case schema

- `id`: stable case identifier
- `sampleLogPath`: path relative to the repo root
- `inputType`: `build-log` or `compiler-error`
- `toolHint`: optional explicit tool hint
- `expect`: assertions to apply against the resulting analysis

## What to assert

For v1, assert these aggressively:

- `toolDetected`
- summary counts
- primary fingerprint(s)
- required categories
- required diagnostic codes

Assert these more loosely:

- message fragments
- parser name
- root-cause titles

Do **not** snapshot volatile values like:

- generated IDs
- durationMs
- correlation IDs
- any future persistence timestamps

## Workflow for new cases

1. add a sample log under `samples/logs/...`
2. add a `.case.json` file that references it
3. run the golden tests
4. adjust the expectations until they reflect the intended stable deterministic output
5. keep the case once it catches a regression you care about
