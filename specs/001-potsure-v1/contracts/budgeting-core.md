# Contract: Budgeting core (pure)

**Feature**: `001-potsure-v1`  
**Version**: 1.1.0 (2026-11-09 standing plan + proration)  
**Module**: `Potsure.Budgeting` (library, no I/O)

## Purpose

Deterministic calculations for preset allocations, **range proration**, category health, **max-of-ratios** required income, budget balance, and runway warnings. All golden tests MUST assert against this contract (SC-004, Constitution II).

## Constants

| Name | Value | Use |
|------|-------|-----|
| `AverageMonthDays` | **30.4375** | All day-based proration (**FR-M017**, **FR-M018**, **FR-M020**) |
| `MoneyScale` | 2 | GBP fraction digits at emission |
| `RoundingMode` | Banker's (half to even) | At each report emission boundary (**FR-M036**) |

## Input DTOs (records)

### `BudgetingContext`

| Field | Description |
|-------|-------------|
| `Preset` | FiftyThirtyTwenty \| SeventyFiveFifteenTen |
| `PlanIncome` | Optional `EstimatedAmount` + `Cadence` (weekly…yearly); null ⇒ **not set** outputs |
| `ReportRange` | Inclusive `Start`, `End` for actuals and **FR-M018** income side |
| `RunwayWindow` | Inclusive `Start` (= report start), `End` (report end + N months, clamped) |
| `Categories[3]` | Kind, `RecordedFundingDirect`, optional `PlannedFutureFundingDirect` |
| `Pots[]` | `CategoryKind`, `RecordedFunding`, `AllocationToPot` optional |
| `Commitments[]` | Per confirmed recurring **expense**: category, optional pot, cadence, fixed amount + anchor or stored monthly estimate, amount kind |
| `Actuals[]` | Per category: net attributed spend in **ReportRange** only |
| `UncategorisedSpend` | Total unattributed debits in **ReportRange** |
| `ForecastHorizonMonths` | N for runway end derivation (**FR-M035**) |

### `Money`

- `Amount`: decimal, scale 2 at emission, GBP.

### `PlanIncomeCadence` factors (monthly equivalent)

Weekly × 52÷12; Fortnightly × 26÷12; Monthly × 1; Quarterly ÷ 3; Yearly ÷ 12 (**FR-M031**).

## Proration

```
ProrateMonthlyEquivalent(monthly, inclusiveStart, inclusiveEnd) =
  monthly × (inclusiveDayCount ÷ 30.4375)
```

Round with banker's rule at report line emission.

## Commitment projection (expense, recurring only)

- **Fixed**: `AmountPerOccurrence × OccurrenceCountInRange` from stored **payment anchor** and cadence stepping (monthly: calendar month steps + last-day clamp; weekly/fortnightly: day intervals; annual: yearly steps).
- **Variable**: `StoredMonthlyEstimate × (inclusiveDayCount ÷ 30.4375)` for the projection window (report range for **FR-M018**; full runway window for **FR-M020** expense side).
- **No** calendar-month overlap apportionment for variable patterns.

## Output DTOs

### `CategoryHealthLine`

| Field | Formula / rule |
|-------|----------------|
| `Allocation` | Monthly-equivalent category allocation **prorated** to **ReportRange** |
| `RecordedFunding` | Direct + sum(pot recorded) once |
| `ActualActivity` | Net attributed spend in **ReportRange** |
| `UnderOver` | `Allocation − ActualActivity` (funding **not** in formula) |
| `OverAllocationWarning` | Recorded/pot slices vs **prorated** allocation (**FR-M017a**) |
| `NotSet` | When `PlanIncome` incomplete |

### `RequiredIncomeResult`

| Field | Rule |
|-------|------|
| `RequiredByCategory[c]` | `Commitments[c] / presetPercent[c]` (0% ⇒ 0) |
| `RequiredTotal` | **Maximum** of the three per-category values (**FR-M018**) |
| `LimitingCategory` | Argmax; tie → Needs, Wants, Savings & Debt |
| `EstimatedIncomeInRange` | Monthly-equivalent from plan income **prorated** to **ReportRange** |
| `Shortfall` | `max(0, RequiredTotal − EstimatedIncomeInRange)` |
| `Conclusion` | Yes if `EstimatedIncomeInRange >= RequiredTotal` else No + gap; **not set** if plan income incomplete |

### `RunwayWarning`

For each category:

- `ProjectedSpend` = cadence-projected **expense** commitments over **RunwayWindow** (variable uses stored estimate proration).
- `AllocationRunway` = monthly-equivalent category allocation prorated over **RunwayWindow**.
- `ActualInReportRange` = actual activity in **ReportRange** only.
- Warn if `ProjectedSpend > (AllocationRunway − ActualInReportRange + RecordedFunding)` (**FR-M020**).

## Invariants

- FR-M036: same inputs → bit-identical outputs (including 30.4375 and banker's emission).
- No I/O, no clocks (pass dates as parameters).
- Pots never introduce custom preset percentages.
- One-offs and income patterns **excluded** from commitment totals.

## Test catalogue (minimum golden scenarios)

1. Category-only budget, 50/30/20, fixed monthly anchor stepping with month-end clamp.
2. Report range not one calendar month: allocation and income proration match 30.4375.
3. Max-of-ratios required income; limiting category tie-break.
4. Variable expense: stored monthly estimate prorated; not live average.
5. Three pots and several pots; funding roll-up without double count.
6. Plan income incomplete → not set on allocations and balance.
7. Runway window: actuals in report range only; commitments over full runway.
8. Banker's rounding edge cases (half to even) at emission.
9. Preset switch: monthly-equivalent allocations change; prorated report outputs consistent.
