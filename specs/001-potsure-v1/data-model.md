# Data Model: Potsure v1

**Feature**: `001-potsure-v1`  
**Date**: 2026-11-09 (standing plan pivot; schema orchestration note: 2026-11-03)

Conceptual model aligned with spec key entities, planning prompt, and FR-M001–M037 plus **FR-M008c**, **FR-M008d**, **FR-M013**–**FR-M013c**, **FR-M031a**. Physical EF mapping is an implementation concern; invariants below are normative for v1.

## Schema evolution

- Physical tables map from aggregates below via EF Core in `Potsure.Data` / `Potsure.Infrastructure` (implement layout).
- Schema changes ship as **versioned EF migrations** in source control (not ad hoc DDL at runtime).
- **Applying** migrations is an orchestration/deployment step (Aspire `AddEFMigrations` locally; controlled job when hosted)  -  see [plan.md](plan.md) and [research.md](research.md). Runtime services seed **application** data (e.g. default budget categories on profile create) separately from schema migration.

## Ownership and scope

- Every user-owned aggregate carries **`UserId`** (hosted) or a single local profile key (local-only mode).
- **One Monzo account association** per user/plan; all imports and transactions bind to it.
- **One standing budget plan** per user (**FR-M031**): preset, estimated income + cadence, confirmed recurring items, pot mode, horizon settings; **no** saved plan start/end dates or library of plan profiles.
- **Report date range** is **in-session per browser tab** for report views (**FR-M031a**); not persisted on the standing plan (**FR-X006**).
- **GBP only** for stored monetary amounts (FR-M026).

## Enumerations (conceptual)

| Name | Values | Notes |
|------|--------|-------|
| `BudgetCategoryKind` | Needs, Wants, SavingsAndDebt | Fixed trio; FR-M012 |
| `AllocationPreset` | FiftyThirtyTwenty (default), SeventyFiveFifteenTen | Category-level only |
| `PlanIncomeCadence` | Weekly, Fortnightly, Monthly, Quarterly, Yearly | Standing plan estimated income |
| `IncomeCadence` | Weekly, Fortnightly, Monthly, Variable, AdHoc, Quarterly, Yearly | Per confirmed income pattern |
| `ExpenseCadence` | Weekly, Fortnightly, Monthly, Variable, AnnualRare | Annual detection not guaranteed complete |
| `PaymentType` | DirectDebit, Card, FasterPayment, Other | From Monzo `Type` mapping |
| `ExpenseAmountKind` | Fixed, Variable | User authoritative per **FR-M008d** |
| `TransactionKind` | ExpenseDebit, IncomeCredit, RefundCredit, InternalTransfer, PotMovement, Unknown | Drives detection exclusion and actuals |
| `PatternConfirmationState` | Suggested, Confirmed, Rejected | Rejected may re-suggest |
| `PotMappingMode` | CategoryOnly, ThreePots, SeveralPots | **FR-M013**; default category-only acceptable |

## Core aggregates

### BudgetProfile (standing plan)

Root planning aggregate for a user (1:1 with plan in v1).

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | |
| UserId | string? | Required hosted |
| SelectedPreset | AllocationPreset | Default FiftyThirtyTwenty |
| Currency | ISO | Fixed GBP |
| MonzoAccount | MonzoAccountAssociation | Required before import persist |
| PotMappingMode | PotMappingMode | Default CategoryOnly |
| EstimatedIncomeAmount | Money? | Null until user saves |
| EstimatedIncomeCadence | PlanIncomeCadence? | Null until user saves; both required for allocations |
| EstimatedIncomeComplete | bool | Derived: both amount and cadence saved |
| Categories | BudgetCategory[3] | Exactly three fixed kinds |
| OptionalPots | BankPot[] | Empty in category-only; mode invariants otherwise |
| ForecastHorizonMonths | int | 1–12; default 3; **FR-M035** |
| ActualIncomeWindowMonths | int | 1–12; default 3; persisted **FR-M019** |
| RememberedColumnMapping | MonzoColumnMapping? | Reused when layout unchanged |

**Invariants**: Cannot remove/replace categories. Preset change recalculates **monthly-equivalent allocations only** from current estimated income (**FR-M011**). Incomplete estimated income → reports show **not set**, not £0 planned maths (**FR-M031**).

### BudgetCategory (fixed ×3)

| Field | Type | Rules |
|-------|------|-------|
| Kind | BudgetCategoryKind | Immutable kind |
| MonthlyAllocationAmount | Money? | Preset × monthly-equivalent income when complete |
| RecordedFundingDirect | Money | User-declared; not from imports |
| PlannedFutureFundingDirect | Money | Optional user intent |

Derived (not stored as source of truth in core; may be cached): **range-prorated allocation**, **actual activity**, **under/over**, pot rollups.

### BankPot (optional)

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | |
| Name | string | User label (Monzo-aligned) |
| CategoryId | Guid | Exactly one parent category |
| AllocationToPot | Money? | Optional slice of category allocation |
| RecordedFunding | Money | User-declared |
| PlannedFutureFunding | Money? | |

**Invariants**: No parent pot; no preset ratios; mode limits (**FR-M013**). Pot amounts roll into category **once** (FR-M013–M017). Delete with non-zero recorded funding → roll to category or discard (**FR-M013c**).

### ReportDateRange (session UI state, not plan aggregate)

| Field | Type | Rules |
|-------|------|-------|
| StartDate | DateOnly | Inclusive |
| EndDate | DateOnly | Inclusive |
| TabId | string? | Implementation hook for per-tab scope |

**Invariants**: Default current calendar month when unset in tab (**FR-M031a**). Shared across report views in same tab; independent across tabs. Not exported as a saved preset (**FR-X006**).

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
| AmountKind | ExpenseAmountKind | Fixed vs variable per **FR-M008d** |
| FixedAmount | Money? | |
| VariableEstimate | Money? | When variable |
| Cadence | IncomeCadence | |
| ExpectedDates | DateOnly[]? | Optional receipt hints |
| SplitNeedsPercent | decimal | Display-only splits |
| SplitWantsPercent | decimal | Must sum to 100% |
| SplitSavingsDebtPercent | decimal | FR-M010 |
| State | PatternConfirmationState | |

**Invariants**: Splits **must not** drive category allocations or FR-M018. Editable/deletable **FR-M008c**.

### RecurringExpense (confirmed recurring expense pattern)

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | |
| VendorLabel | string | |
| CategoryId | Guid | One of three |
| MappedPotId | Guid? | Optional; cleared when pot deleted |
| Cadence | ExpenseCadence | |
| PaymentType | PaymentType | |
| AmountKind | ExpenseAmountKind | User override **FR-M008d** |
| FixedAmountPerOccurrence | Money? | Fixed patterns |
| PaymentAnchorDate | DateOnly? | Fixed occurrence stepping **FR-M008c** |
| PaymentAnchorUserOverride | bool | Re-import may refresh only when false |
| StoredMonthlyEstimate | Money? | Variable; **FR-M018**/**FR-M020**; stable until user edit |
| EstimateCoverage | CoverageWindow | For FR-M032/034 labelling |
| Tag | string? | Free text; no calc effect FR-M009 |
| State | PatternConfirmationState | |

**Invariants**: Contributes to **range commitments** when confirmed. Merge/split/edit/delete per **FR-M008**/**FR-M008c**. Variable estimate not auto-updated on import.

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
| CategoryId | Guid? | Income attribution may omit category |
| IsIncomeAttribution | bool | FR-M010b |
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
| ReportRange | Inclusive start/end |
| RunwayWindow | Inclusive start (report start) through runway end **FR-M020** |
| EstimatedIncomeMonthlyEquivalent | From standing plan when complete |
| EstimatedIncomeInRange | Prorated for **FR-M018** |
| ActualIncomeMonthlyMean | **FR-M019** over N calendar months |
| PerCategory | Range allocation (prorated), recorded funding, actual activity, under/over, over-allocation flag |
| PerPot | Optional funding/activity detail |
| UncategorisedSpendTotal | Unattributed debits |
| CommitmentsTotal | Cadence-projected recurring expenses in range |
| RequiredIncomePerCategory | FR-M018 |
| RequiredIncomeTotal | Max of three |
| Shortfall | |
| LimitingCategory | With tie rules |
| BudgetBalanceConclusion | Yes/no + monetary gap; **not set** when plan income incomplete |
| UncertaintyFlags | History gaps, estimate coverage |

**Invariants**: Under/over = range allocation − range actual activity (recorded funding excluded). Count each pound once in category totals.

## Relationships (ER sketch)

```text
User 1──1 BudgetProfile
BudgetProfile 1──3 BudgetCategory
BudgetProfile 0──* BankPot ──*──1 BudgetCategory
BudgetProfile 1──1 MonzoAccountAssociation
User 1──* Transaction
User 1──* IncomeSource
User 1──* RecurringExpense
RecurringExpense *──0..1 BankPot
Transaction 0──* Assignment
RecurringExpense 0──* Assignment (pattern defaults)
(Session) ReportDateRange  -  not child of BudgetProfile
```

## Validation rules (summary)

| Rule | Source |
|------|--------|
| Income split percents sum to 100% | FR-M010 |
| Non-GBP rows rejected | FR-M026 |
| Pot mode invariants on map/switch | FR-M013 |
| Pot funding roll/discard on delete | FR-M013c |
| Estimated income + cadence both required for allocation maths | FR-M031 |
| Forecast horizon and actual-income window 1–12 | FR-M019, FR-M035 |
| Category assignment required on confirm/split children | FR-M008 |
| De-dupe key per FR-M004 | FR-M004 |
| One-off → actuals only | FR-M037 |
| Deterministic reports for unchanged inputs | FR-M036 |
| Banker's 2dp at report emission | FR-M036 |

## Deletion

Transactional wipe of user financial rows, assignments, patterns, transactions, pots, profile; hosted **Identity** account data per operator policy (email/password v1; optional passkeys post–v1.0.0); retain non-financial audit marker only (user prompt reliability/deletion).
