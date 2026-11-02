# Quickstart: Potsure v1 validation

**Feature**: `001-potsure-v1`  
**Purpose**: Runnable scenarios to prove the feature end-to-end after implementation. References [data-model.md](data-model.md) and [contracts/](contracts/) without duplicating full schemas.

## Prerequisites

- .NET 10 SDK (`global.json`).
- Docker (SQL Server container via Aspire AppHost locally).
- Anonymised Monzo CSV golden fixtures in `tests/` (never personal exports in repo).
- Optional: hosted stage credentials (not required for local validation).

## Local run (target state post-implement)

```bash
dotnet run --project src/Potsure.AppHost/Potsure.AppHost.csproj
```

- Open Aspire dashboard URL from console.
- Launch web resource; confirm HTTPS and local-only mode restrictions per docs.
- SQL container healthy via `WaitFor` reference.

## Scenario 1 — Safe Monzo import (US1)

1. Start app (local-only or hosted with passkey enrolment).
2. Confirm or set Monzo account association.
3. Upload anonymised valid Monzo CSV ([monzo-import.md](contracts/monzo-import.md)).
4. Confirm auto-detected column mapping; import.
5. **Expect**: Row counts match file minus reported skips; transfer rows excluded from uncategorised; dedupe message on second overlapping upload.

## Scenario 2 — Recurring confirmation (US2)

1. With ≥1 month of fixture data, open detection review.
2. Confirm one expense → assign Needs + optional pot; confirm income with splits totalling 100%.
3. Re-upload same period fixture.
4. **Expect**: Assignments remembered; matched debits auto-attributed; income splits visible but do not change category allocations.

## Scenario 3 — Preset and period (US3)

1. Default preset 50/30/20; set inclusive period covering fixture dates.
2. Enter budgeted income; verify allocations = preset × income.
3. Switch to 75/15/10.
4. **Expect**: Allocations update immediately; recorded funding and actuals unchanged.

## Scenario 4 — Category health (US6)

1. With confirmed patterns and one one-off categorised debit.
2. Open category health.
3. **Expect**: Four measures per category; under/over = allocation − actual; uncategorised warning for orphan debits; one-off not in forecast list.

## Scenario 5 — Budget balance (US7)

1. Set budgeted income below commitment-implied requirement.
2. Open budget balance report.
3. **Expect**: Limiting category, shortfall, no/not covered messaging; commitments from recurring expenses only ([budgeting-core.md](contracts/budgeting-core.md)).

## Scenario 6 — Export and delete (US10)

1. Reauthenticate; export JSON bundle and CSV ZIP separately.
2. Verify files match [export-formats.md](contracts/export-formats.md).
3. Permanent delete account data.
4. **Expect**: No retrievable transactions/plan; confirmation shown.

## Scenario 7 — Determinism (FR-M036)

1. Open category health and budget balance twice without changes.
2. **Expect**: Identical totals both times.

## Scenario 8 — Unit/core-only (CI)

```bash
dotnet test tests/Potsure.Budgeting.Tests -c Release
dotnet test tests/Potsure.Statements.Tests -c Release
```

**Expect**: Golden parsing files pass; budgeting golden scenarios match independently verified expected results.

## Scenario 9 — Accessibility smoke (US12)

1. Keyboard-only: import wizard → recurring summary → category health.
2. Narrow viewport: primary report columns without horizontal scroll on main content.
3. **Expect**: Focus order sane; charts have textual equivalents per UI implementation.

## Partial-month / coverage labelling (FR-M032, FR-M034)

1. Fixture with <3 months groceries + gap in calendar coverage.
2. **Expect**: Variable estimate shows averaging window; missing days not treated as £0; disclaimer on forecasts.

## Hosted stage smoke (optional)

After deploy to dev/test ACA + Azure SQL free tier:

- Cold start acceptable; health probe does not hammer DB (auto-pause).
- Passkey required before data; OTel excludes payee/upload bodies.

## Failure cases to spot-check

- Non-GBP row → rejected with clear error.
- Wrong bank CSV → rejected.
- Oversized upload → bounded error, no partial persist without rollback.
- Cancel mid-upload → temp cleared.
