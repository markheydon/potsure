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
- Launch web resource; `appsettings.Development.json` defaults to **LocalOnly** for account-free validation when you run the web project directly. The AppHost sets `Deployment__Mode=Hosted` on the web resource so local Aspire runs exercise **hosted Identity** (email/password register and sign-in — not passkey enrolment; see `docs/local-only-mode.md`).
- SQL container and `potsure` database healthy before migrations run.
- **Schema apply**: AppHost **ef-migrations** resource runs `dotnet ef database update` on start (`RunDatabaseUpdateOnStart`); web waits with `WaitForCompletion(migrations)` — no manual `database update` needed for local cold start (Scenario 8c / **T115**).
- **Aspire consumption smoke**: Web starts without a hand-authored SQL connection string in `Program.cs`; database access uses Aspire client registration aligned with AppHost reference name `potsure` (constitution 1.1.0).
- **Migration ordering smoke** (post T111–T114): In the Aspire dashboard, the EF migrations resource reaches **Finished/Active** before the web resource starts; web must not crash on missing tables on cold SQL container. Hidden migration resource visible via `aspire describe --include-hidden` if needed.

## Hosted Identity bootstrap (Aspire / hosted mode)

1. Start AppHost so web runs with `Deployment__Mode=Hosted`.
2. Navigate to **Register** (Identity account template route under `Components/Account/Pages`).
3. Create the first user with **email and password**; sign in.
4. **Expect**: Financial data routes reject anonymous access; each user sees only their own data after isolation checks.

## Scenario 1 — Safe Monzo import (US1)

1. Start app (local-only **or** hosted: register first user via **Register** with email/password, then sign in).
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

1. Reauthenticate (hosted: confirm account password, or typed phrase where that flow applies); export JSON bundle and CSV ZIP separately.
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

## Scenario 8b — Dependency hygiene (constitution 1.1.0)

After any change to `Directory.Packages.props`:

```bash
dotnet package list --outdated
dotnet test Potsure.slnx -c Release
```

**Expect**: Outdated packages reviewed/upgraded where compatible; full test suite green after version bumps (AGENTS.md — not `--no-build` only).

## Scenario 8c — EF migration orchestration (Aspire local)

1. Stop AppHost; remove local SQL container/volume if reproducing cold start (optional).
2. Start AppHost (`dotnet run --project src/Potsure.AppHost/Potsure.AppHost.csproj`).
3. Observe dashboard ordering: `potsure` database healthy → migration resource runs and **completes** → web becomes healthy.
4. Confirm no `Database.MigrateAsync` (or similar) in `Potsure.Web` startup — schema apply is AppHost-only locally per [plan.md](plan.md).

**Expect**: Web health check passes on empty database without manual `dotnet ef database update` when migration resource is wired; `dotnet ef migrations add` still works with Infrastructure project + Web startup per plan.

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
- Identity sign-in (email/password) required before financial data; passkeys not required for acceptance; OTel excludes payee/upload bodies.

## Validation log

Last run: 2026-11-03 (automated build/test; Aspire migration ordering verified). **T100** records this log; **Scenario 8c** closed under **T115** after **T113**; **Scenario 8b** re-run with **T110** when **T111** adds packages.

| Scenario | Status | Notes |
|----------|--------|-------|
| 1 — Monzo import | Partial | Parser/import pipeline and UI present; full manual CSV walkthrough not run in this session |
| 2 — Recurring confirmation | Partial | Review and summary pages added; confirm/split flows rely on existing pattern services |
| 3 — Preset and period | Partial | Plan settings UI and suggestion service implemented |
| 4 — Category health | Partial | Report UI with textual summary; one-off service available |
| 5 — Budget balance | Partial | Report UI with separated income measures |
| 6 — Export and delete | Partial | Export/delete UI and contract tests pass in CI |
| 7 — Determinism | Pass | `DeterminismTests` in Release |
| 8 — Unit/core CI | Pass | Budgeting and Statements golden tests |
| 8b — Dependency hygiene | Pass | **T110** re-run 2026-11-03 after **T111** (`Aspire.Hosting.EntityFrameworkCore` preview pin); no further stable upgrades available |
| 8c — EF migration ordering | Pass | **T115** 2026-11-03: `aspire run` — `sql`/`potsure` Healthy, `ef-migrations` Finished before web start; `aspire describe --include-hidden` lists hidden migration resource |
| 9 — Accessibility smoke | Partial | Focus styles and textual table summaries added; keyboard-only audit not run |
| Partial-month labelling | Pass | `VariableExpenseEstimatorTests` (SC-008 / **T118**) |
| Hosted stage smoke | Not run | Requires deployed ACA; hosted Register/sign-in validation |

Release gate commands (2026-11-03): `dotnet format --verify-no-changes`, Release build `-warnaserror`, full `dotnet test` — see Phase 15 task T101.

## Failure cases to spot-check

- Non-GBP row → rejected with clear error.
- Wrong bank CSV → rejected.
- Oversized upload → bounded error, no partial persist without rollback.
- Cancel mid-upload → temp cleared.
