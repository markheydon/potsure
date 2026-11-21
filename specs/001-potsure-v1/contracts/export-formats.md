# Contract: Export formats

**Feature**: `001-potsure-v1`  
**Version**: 1.1.0 (standing plan fields 2026-11-09)  
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
    "potMappingMode": "CategoryOnly",
    "forecastHorizonMonths": 3,
    "actualIncomeWindowMonths": 3,
    "estimatedIncome": { "amount": 2500.00, "cadence": "Monthly" },
    "categories": [
      { "kind": "Needs", "monthlyAllocation": 1250.00, "recordedFundingDirect": 0, "plannedFutureFundingDirect": 0 }
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

Headers: `Section,Key,Value`  -  rows for preset, pot mapping mode, estimated income amount and cadence, forecast horizon, actual-income window, monthly category allocations, pot funding, confirmed pattern summaries (including payment anchors and stored variable estimates). **Report date range is not exported** (in-session only, FR-X006).

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
