# Contract: Export formats

**Feature**: `001-potsure-v1`  
**Version**: 1.0.0  
**Requirements**: FR-M022, reauthentication before export.

## User choice at export time

| Format | MIME / packaging | Contents |
|--------|------------------|----------|
| `json-bundle` | `application/zip` or single `application/json` | Versioned JSON schema below |
| `csv-zip` | `application/zip` | `transactions.csv`, `plan.csv`, `metadata.json` |

## JSON bundle schema (version 1)

```json
{
  "exportVersion": "1.0",
  "exportedAt": "2026-11-02T12:00:00Z",
  "currency": "GBP",
  "budgetProfile": {
    "preset": "FiftyThirtyTwenty",
    "forecastHorizonMonths": 3,
    "period": { "start": "2026-10-01", "end": "2026-10-31", "budgetedIncome": 2500.00 },
    "categories": [
      { "kind": "Needs", "allocation": 1250.00, "recordedFundingDirect": 0, "plannedFutureFundingDirect": 0 }
    ],
    "bankPots": [],
    "monzoAccount": { "displayName": "…", "fingerprint": "…" }
  },
  "incomeSources": [],
  "recurringExpenses": [],
  "assignments": [],
  "transactions": []
}
```

- Monetary fields: decimal numbers with 2 fraction digits, GBP implied.
- No secrets, passkeys, or Data Protection keys in export.
- Personal payee strings are user data (included by design for portability).

## CSV ZIP layout

### `transactions.csv`

Headers: `Id,MonzoTransactionId,Date,Time,Type,Name,Amount,Currency,Description,Kind,CategoryAssignment,MappedPot,Tag`

### `plan.csv`

Headers: `Section,Key,Value` — rows for preset, period, budgeted income, category allocations, pot funding, confirmed pattern summaries.

### `metadata.json`

```json
{
  "exportVersion": "1.0",
  "format": "csv-zip",
  "exportedAt": "2026-11-02T12:00:00Z",
  "schemaUrls": ["specs/001-potsure-v1/contracts/export-formats.md"]
}
```

## Import of exports

v1 export is **outbound-only** (no re-import of bundle required for MVP); schema stability supports future restore tooling.
