# Contract: Budgeting core (pure)

**Feature**: `001-potsure-v1`  
**Version**: 1.0.0  
**Module**: `Potsure.Budgeting` (library, no I/O)

## Purpose

Deterministic calculations for preset allocations, category health, required income, budget balance, and runway warnings. All golden tests MUST assert against this contract (SC-004, Constitution II).

## Input DTOs (records)

### `BudgetingContext`

| Field | Description |
|-------|-------------|
| `Preset` | FiftyThirtyTwenty \| SeventyFiveFifteenTen |
| `Period` | Inclusive `Start`, `End` |
| `BudgetedIncome` | User-confirmed planned income |
| `Categories[3]` | Kind, `RecordedFundingDirect`, optional `PlannedFutureFundingDirect` |
| `Pots[]` | `CategoryKind`, `RecordedFunding`, `AllocationToPot` optional |
| `Commitments[]` | Per confirmed recurring expense: category, optional pot, cadence, fixed or monthly estimate, coverage metadata |
| `Actuals[]` | Per category: net attributed spend in period |
| `UncategorisedSpend` | Total unattributed debits |
| `ForecastHorizonEnd` | End date for runway (default period end + horizon months) |

### `Money`

- `Amount`: decimal, scale 2, GBP.
- Operations use banker's rounding at **report emission** unless a test documents otherwise.

## Output DTOs

### `CategoryHealthLine`

| Field | Formula / rule |
|-------|----------------|
| `Allocation` | `BudgetedIncome × presetPercent` |
| `RecordedFunding` | Direct + sum(pot recorded) once |
| `ActualActivity` | Net attributed spend |
| `UnderOver` | `Allocation − ActualActivity` (funding **not** in formula) |
| `OverAllocationWarning` | `(pot allocations + direct recorded slices) > Allocation` |

### `RequiredIncomeResult`

| Field | Rule |
|-------|------|
| `RequiredByCategory[c]` | `Commitments[c] / presetPercent[c]` (guard div-by-zero) |
| `RequiredTotal` | Max or sum per documented algorithm in implement (spec: per-category and overall requirement — use **maximum** of category required incomes as overall income needed to satisfy all category ratios simultaneously) |
| `LimitingCategory` | Category with highest `RequiredByCategory`; tie → Needs, then Wants, then Savings & Debt |
| `Shortfall` | `max(0, RequiredTotal − BudgetedIncome)` |
| `Conclusion` | Yes if `BudgetedIncome >= RequiredTotal` else No + monetary gap |

**Note**: Implement phase MUST codify overall required income formula in one ADR or XML doc and golden files; typical 50/30/20 check: income `I` must satisfy `I×50% ≥ needs commitments`, etc.; overall required `I* = max(N/0.5, W/0.3, S/0.2)`.

### `RunwayWarning`

For each category, compute `ProjectedRemainingSpend` over window = in-period actual to date + cadence-projected remaining commitments + variable estimates (FR-M034). Emit warning if:

`ProjectedRemainingSpend > (Allocation − ActualActivity + RecordedFunding)`

Optional pot-level breakdown in message payload.

## Invariants

- FR-M036: same inputs → bit-identical outputs.
- No I/O, no clocks (inject `IClock` or pass dates as parameters).
- Pots never introduce custom preset percentages.

## Test catalogue (minimum golden scenarios)

1. Category-only budget (no pots), 50/30/20, fixed commitments.
2. Three category-named pots; funding and activity without double count.
3. Several expense-specific pots mapped to one category.
4. Preset switch mid-period: allocations change; actuals unchanged.
5. Net refunds reduce actual activity.
6. Limiting category tie-break.
7. Runway with recorded funding covering gap vs not.
8. Variable expense estimate from 1-, 2-, and 3-month windows with coverage labels.
