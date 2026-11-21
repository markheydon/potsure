# Contract: Monzo CSV import

**Feature**: `001-potsure-v1`  
**Version**: 1.0.0  
**Scope**: Monzo mobile/app export CSV only (FR-M001, FR-M005).

## Required headers (canonical Monzo export)

The parser MUST recognise this header set (order may vary; matching is by header name, case-sensitive as exported):

| Header | Required | Maps to |
|--------|----------|---------|
| Transaction ID | Recommended | `MonzoTransactionId` |
| Date | Yes | `TransactionDate` (`dd/MM/yyyy`) |
| Time | Optional | `TransactionTime` |
| Type | Yes | Raw type + classification hints |
| Name | Yes | Counterparty / merchant |
| Emoji | No | Ignored |
| Category | No | Monzo category (informational; not Potsure budget category) |
| Amount | Yes | Signed amount or cross-check with Money Out/In |
| Currency | Yes | Must be `GBP` |
| Local amount | No | Ignored v1 |
| Local currency | No | Ignored v1 |
| Notes and #tags | No | Optional memo |
| Address | No | Ignored v1 |
| Receipt | No | Ignored v1 |
| Description | Optional | Extended memo |
| Category split | No | Ignored v1 |
| Money Out | Optional | Debit magnitude |
| Money In | Optional | Credit magnitude |

## Normalised row (staging)

```json
{
  "monzoTransactionId": "tx_…",
  "date": "2026-10-01",
  "time": "03:09:07",
  "type": "Pot transfer",
  "name": "Needs - 75 Pot",
  "amount": 14.95,
  "currency": "GBP",
  "description": "…",
  "moneyOut": 14.95,
  "moneyIn": null
}
```

## Classification rules (v1)

| Monzo `Type` (examples) | Default `TransactionKind` | Detection input | Category actuals / uncategorised |
|-------------------------|----------------------------|-----------------|----------------------------------|
| Pot transfer | PotMovement or InternalTransfer | Excluded | Excluded unless one-off categorised |
| Flex (early payment) | InternalTransfer or ExpenseDebit | Excluded from detection if transfer | Per FR-M017 transfer rules |
| Card payment | ExpenseDebit | Included | Attribution required |
| Direct Debit | ExpenseDebit | Included | Attribution required |
| Faster payment (out) | ExpenseDebit | Included | Attribution required |
| Faster payment (in, Income category) | IncomeCredit | Included for income detection | Not expense actual |
| monzo_paid | ExpenseDebit | Included | Attribution required |
| Monzo-to-Monzo | InternalTransfer (default) | Excluded | Excluded unless one-off categorised |

Exact rules live in parsing service with golden files; user may one-off override classification to spend.

## Account validation

- **Reject** if file metadata or row patterns indicate a **different** account than the user’s confirmed association.
- **Prompt** for explicit confirmation when identity absent (FR-M006).
- **Reject** non-Monzo layouts (FR-M002, FR-M005).

## De-duplication

1. Same `MonzoTransactionId` → duplicate.
2. Else same `date` + `amount` + **normalised** `description` (FR-M004).

## Import response

```json
{
  "uploadId": "guid",
  "importedCount": 0,
  "deduplicatedCount": 0,
  "skippedCount": 0,
  "transferExcludedCount": 0,
  "errors": [{ "row": 12, "reason": "Non-GBP currency" }],
  "dateRange": { "from": "2026-10-01", "to": "2026-10-31" }
}
```

## Limits (implementation)

Documented in ops/quickstart: max file size, max rows, request timeout; stream parse; temp storage cleared on all exit paths.
