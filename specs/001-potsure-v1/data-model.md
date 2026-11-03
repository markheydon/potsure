# Data Model: Potsure v1

**Feature**: `001-potsure-v1`  
**Date**: 2026-11-02 (schema orchestration note: 2026-11-03)

Conceptual model aligned with spec key entities, planning prompt, and FR-M001–M037. Physical EF mapping is an implementation concern; invariants below are normative for v1.

## Schema evolution

- Physical tables map from aggregates below via EF Core in `Potsure.Infrastructure`.
- Schema changes ship as **versioned EF migrations** in source control (not ad hoc DDL at runtime).
- **Applying** migrations is an orchestration/deployment step (Aspire `AddEFMigrations` locally; controlled job when hosted) — see [plan.md](plan.md) and [research.md](research.md). Runtime services seed **application** data (e.g. default budget categories on profile create) separately from schema migration.

## Ownership and scope

- Every user-owned aggregate carries **`UserId`** (hosted) or a single local profile key (local-only mode).
- **One Monzo account association** per user/plan; all imports and transactions bind to it.
- **One working budget period** per plan (FR-M031); edit dates and budgeted income in place.
- **GBP only** for stored monetary amounts (FR-M026).

## Enumerations (conceptual)

| Name | Values | Notes |
|------|--------|-------|
| `BudgetCategoryKind` | Needs, Wants, SavingsAndDebt | Fixed trio; FR-M012 |
| `AllocationPreset` | FiftyThirtyTwenty (default), SeventyFiveFifteenTen | Category-level only |
| `IncomeCadence` | Weekly, Fortnightly, Monthly, Variable, AdHoc | Detection + display |
| `ExpenseCadence` | Weekly, Fortnightly, Monthly, Variable, AnnualRare | Annual detection not guaranteed complete |
| `PaymentType` | DirectDebit, Card, FasterPayment, Other | From Monzo `Type` mapping |
| `ExpenseAmountKind` | Fixed, Variable | Variable uses FR-M034 averaging |
| `TransactionKind` | ExpenseDebit, IncomeCredit, RefundCredit, InternalTransfer, PotMovement, Unknown | Drives detection exclusion and actuals |
| `PatternConfirmationState` | Suggested, Confirmed, Rejected | Suggestions never drive commitments |
| `PotMappingMode` | None, CategoryNamedPots, ExpenseSpecificPots | UX configuration; maths at category |

## Core aggregates

### BudgetProfile

Root planning aggregate for a user (1:1 with plan in v1).

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | |
| UserId | string? | Required hosted |
| SelectedPreset | AllocationPreset | Default FiftyThirtyTwenty |
| Currency | ISO | Fixed GBP |
| MonzoAccount | MonzoAccountAssociation | Required before import persist |
| WorkingPeriod | BudgetPeriod | Embedded or 1:1 child |
| Categories | BudgetCategory[3] | Exactly three fixed kinds |
| OptionalPots | BankPot[] | Each maps to exactly one category |
| ForecastHorizonMonths | int | Default 3; user override FR-M035 |
| RememberedColumnMapping | MonzoColumnMapping? | Reused when layout unchanged |

**Invariants**: Cannot remove/replace categories; preset change recalculates **allocations only** from current budgeted income (FR-M011).

### BudgetCategory (fixed ×3)

| Field | Type | Rules |
|-------|------|-------|
| Kind | BudgetCategoryKind | Immutable kind |
| AllocationAmount | Money | Preset × budgeted income for period |
| RecordedFundingDirect | Money | User-declared; not from imports |
| PlannedFutureFundingDirect | Money | Optional user intent |

Derived (not stored as source of truth in core; may be cached): **actual activity**, **under/over**, pot rollups.

### BankPot (optional)

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | |
| Name | string | User label (e.g. "Needs - 75 Pot") |
| CategoryId | Guid | Exactly one parent category |
| AllocationToPot | Money? | Optional slice of category allocation |
| RecordedFunding | Money | User-declared |
| PlannedFutureFunding | Money? | |

**Invariants**: No parent pot; no preset ratios; pot amounts roll into category **once** for totals, warnings, and runway (FR-M013–M017).

### BudgetPeriod

| Field | Type | Rules |
|-------|------|-------|
| StartDate | DateOnly | Inclusive FR-M031 |
| EndDate | DateOnly | Inclusive |
| BudgetedIncome | Money | User-confirmed; optional suggestion from income patterns not auto-applied |

### MonzoAccountAssociation

| Field | Type | Rules |
|-------|------|-------|
| DisplayName | string? | User label |
| AccountFingerprint | string? | From metadata when present |
| ConfirmedByUser | bool | Required when metadata ambiguous |

### IncomeSource (confirmed recurring income pattern)

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | |
| SourceLabel | string | e.g. payee name |
| AmountKind | Fixed / Estimate | Fixed amount or confirmed estimate |
| FixedAmount | Money? | |
| EstimateAmount | Money? | |
| Cadence | IncomeCadence | |
| ExpectedDates | DateOnly[]? | Optional receipt hints |
| SplitNeedsPercent | decimal | Display-only splits |
| SplitWantsPercent | decimal | Must sum to 100% |
| SplitSavingsDebtPercent | decimal | FR-M010 |
| State | PatternConfirmationState | |

**Invariants**: Splits **must not** drive category allocations or FR-M018 (display-only).

### RecurringExpense (confirmed recurring expense pattern)

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | |
| VendorLabel | string | |
| CategoryId | Guid | One of three |
| MappedPotId | Guid? | Optional |
| Cadence | ExpenseCadence | |
| PaymentType | PaymentType | |
| AmountKind | ExpenseAmountKind | |
| FixedAmount | Money? | |
| MonthlyEstimate | Money? | From FR-M034 window |
| EstimateCoverage | CoverageWindow | Months/days covered; labelled |
| Tag | string? | Free text; no calc effect FR-M009 |
| State | PatternConfirmationState | |

**Invariants**: Contributes to **period commitments** when confirmed and projected (FR-M018). Merge/split requires user resolution of category/pot/tag (FR-M008).

### Transaction (imported statement row)

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | Internal |
| UserId | string? | |
| MonzoTransactionId | string? | Preferred uniqueness |
| TransactionDate | DateOnly | |
| TransactionTime | TimeOnly? | |
| Amount | Money | Signed or separate in/out |
| Currency | string | Must be GBP |
| Type | string | Raw Monzo type |
| Name | string | |
| Description | string? | Memo |
| Kind | TransactionKind | Classification |
| UploadBatchId | Guid | |
| DedupeKey | string | Computed for fallback uniqueness |

**Invariants**: Imports do not alter recorded funding (FR-M016). Internal transfers excluded from actuals/uncategorised unless one-off categorised (FR-M017).

### Assignment (user-confirmed attribution memory)

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | |
| TransactionId | Guid? | Per-transaction override |
| RecurringExpenseId | Guid? | Pattern-level default |
| CategoryId | Guid | |
| MappedPotId | Guid? | Compatible with category |
| Tag | string? | Display |
| ConfirmedAt | DateTimeOffset | |

**Invariants**: Auto-attribution from confirmed expense patterns unless overridden (FR-M010a); persists across re-upload.

### StatementUpload

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | |
| FileName | string | |
| Status | enum | Pending, Completed, Failed, RolledBack |
| RowCounts | ImportStatistics | Imported, skipped, deduped, transfer excluded |
| ErrorMessages | string[] | No silent drops FR-M003 |

### PeriodSummary (computed report DTO)

Not a durable aggregate root; output of budgeting core + reporting layer.

| Field | Description |
|-------|-------------|
| Period | Start/end |
| BudgetedIncome | Planned |
| ExpectedIncomeInPeriod | From income patterns (separate from budgeted) |
| ActualIncomeInPeriod | From statements |
| PerCategory | Allocation, recorded funding (direct + pots once), actual activity (net attributed), under/over, over-allocation flag |
| PerPot | Optional funding/activity detail |
| UncategorisedSpendTotal | Unattributed debits |
| CommitmentsTotal | Cadence-projected recurring expenses |
| RequiredIncomePerCategory | FR-M018 |
| RequiredIncomeTotal | |
| Shortfall | |
| LimitingCategory | With tie rules |
| BudgetBalanceConclusion | Yes/no + monetary gap |
| UncertaintyFlags | History gaps, estimate coverage |

**Invariants**: Under/over = allocation − actual activity (recorded funding excluded). Count each pound once in category totals.

## Relationships (ER sketch)

```text
User 1──1 BudgetProfile
BudgetProfile 1──1 BudgetPeriod
BudgetProfile 1──3 BudgetCategory
BudgetProfile 0──* BankPot ──*──1 BudgetCategory
BudgetProfile 1──1 MonzoAccountAssociation
User 1──* Transaction
User 1──* IncomeSource
User 1──* RecurringExpense
RecurringExpense *──0..1 BankPot
Transaction 0──* Assignment
RecurringExpense 0──* Assignment (pattern defaults)
```

## Validation rules (summary)

| Rule | Source |
|------|--------|
| Income split percents sum to 100% | FR-M010 |
| Non-GBP rows rejected | FR-M026 |
| Pot + direct recorded funding ≤ allocation → warning if exceeded | FR-M017a |
| Category assignment required on confirm/split children | FR-M008 |
| De-dupe key per FR-M004 | FR-M004 |
| One-off → actuals only | FR-M037 |
| Deterministic reports for unchanged inputs | FR-M036 |

## Deletion

Transactional wipe of user financial rows, assignments, patterns, transactions, pots, period, profile; Identity/passkey data per policy; retain non-financial audit marker only (user prompt reliability/deletion).
