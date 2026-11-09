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
aspire run --apphost src/Potsure.AppHost
```

- **Stable local ports**: AppHost `Properties/launchSettings.json` pins the dashboard (`https://localhost:17134` by default) and web HTTP (`http://localhost:5191` via AppHost endpoint config). Re-run without `--isolated` so ports stay fixed; `--isolated` randomises ports for parallel instances.
- Open the dashboard at the URL above (login token in the console may change each run; the host/port should not).
- **GitHub Codespaces / dev tunnels**: open the forwarded **web** URL from the Aspire dashboard (not `https://localhost:7049` from `launchSettings.json`). Hosted mode honours `X-Forwarded-*` headers in Development so post-login redirects stay on your forwarded host.
- Launch web resource; `appsettings.Development.json` defaults to **LocalOnly** for account-free validation when you run the web project directly. The AppHost sets `Deployment__Mode=Hosted` on the web resource so local Aspire runs exercise **hosted Identity** (email/password register and sign-in  -  not passkey enrolment; see `docs/local-only-mode.md`).
- SQL container and `potsure` database healthy before migrations run.
- **Local SQL persistence**: AppHost mounts a Docker data volume (`potsure-sql-data`) and keeps the SQL container **persistent** across normal Aspire stop/start, so registered users and imported data survive typical dev sessions. EF migrations still run on start but are no-ops when the schema is current.
- **Reset local database**: remove the volume when you need a clean slate, for example `docker volume rm potsure-sql-data` after stopping Aspire (also remove the SQL container if it still references the volume). If AppHost user-secrets are reset while the volume still holds an old SA password, SQL login may fail until you remove the volume and let Aspire recreate it.
- **Do not use `aspire stop --force` casually**: it permanently deletes persistent resource instances, including the local SQL data volume managed by Aspire.
- After a **manual** database wipe, antiforgery warnings in logs from old browser cookies are expected (see **BL-008** in [`plan/FEEDBACK.md`](../../plan/FEEDBACK.md); feedback notes only, not product direction).
- **Schema apply**: AppHost **ef-migrations** resource runs `dotnet ef database update` on start (`RunDatabaseUpdateOnStart`); web waits with `WaitForCompletion(migrations)`  -  no manual `database update` needed for local cold start (Scenario 8c / **T115**).
- **Aspire consumption smoke**: Web starts without a hand-authored SQL connection string in `Program.cs`; database access uses Aspire client registration aligned with AppHost reference name `potsure` (constitution 1.2.0). Re-run after **Phase 23** Identity wiring (**T144**–**T146**) changes `Program.cs`.
- **Migration ordering smoke** (post T111–T114): In the Aspire dashboard, the EF migrations resource reaches **Finished/Active** before the web resource starts; web must not crash on missing tables on cold SQL container. Hidden migration resource visible via `aspire describe --include-hidden` if needed.

## Hosted Identity bootstrap (Aspire / hosted mode)

Phase 23 (**T143**–**T148**) lands template Identity pages with BlazorBlueprint on Login, Register, Forgot password, and primary Manage flows. Automated smoke: `HostedIdentityConfigurationTests` (password policy DI) and `HostedIdentityFunctionalTests` (register + password sign-in check via `AddHostedIdentity`).

1. Start AppHost so web runs with `Deployment__Mode=Hosted`.
2. Navigate to **Register** (`/Account/Register`).
3. Create the first user with **email and password**; sign in at `/Account/Login`.
4. **Expect**: Financial data routes reject anonymous access; each user sees only their own data after isolation checks.

## Scenario 1  -  Safe Monzo import (US1)

1. Start app (local-only **or** hosted: register first user via **Register** with email/password, then sign in).
2. Confirm or set Monzo account association.
3. Upload anonymised valid Monzo CSV ([monzo-import.md](contracts/monzo-import.md)).
4. Confirm auto-detected column mapping; import.
5. **Expect**: Row counts match file minus reported skips; transfer rows excluded from uncategorised; dedupe message on second overlapping upload.

## Scenario 2  -  Recurring confirmation (US2)

1. With ≥1 month of fixture data, open detection review.
2. Confirm one expense → assign Needs + optional pot; override **fixed vs variable** if suggestion wrong (**FR-M008d**); confirm income with splits totalling 100%.
3. Edit confirmed rent amount; delete an obsolete recurring item; reject a suggestion then re-import and **Expect** re-suggestion allowed (**FR-M008**).
4. Re-upload same fixture.
5. **Expect**: Assignments remembered; matched debits auto-attributed; stored variable monthly estimate stable until user edit; income splits visible but do not change category allocations. (**SC-003** evidence: integration tests **T106** / **T140**–**T142**; release sign-off **T151**.)

## Scenario 4b  -  Pot mapping modes (US4)

1. Start in **category-only**; confirm no pot mapping UI on expense assignment.
2. Switch to **three pots**; map one pot per category at most.
3. Switch toward **several pots** with multiple pots on one category; attempt **three pots** switch **Expect** blocked until surplus removed.
4. Delete mapped pot with recorded funding **Expect** roll-or-discard prompt (**FR-M013c**).
5. On expense assign in three pots mode with mapped category pot **Expect** pre-selection per **FR-M013b**.

## Scenario 3  -  Preset and standing plan income (US3)

1. Default preset 50/30/20; open category health **before** saving plan income.
2. **Expect**: Allocations and budget balance show **not set**, not £0 planned maths (**FR-M031**).
3. Save **estimated income** and **cadence** (e.g. monthly £2500); verify monthly allocations = preset × monthly-equivalent income.
4. Set report date range to a non-month-long inclusive span; verify category health allocation line **prorates** with 30.4375 rule (**FR-M017**).
5. Switch to 75/15/10.
6. **Expect**: Monthly-equivalent allocations update immediately; recorded funding and report-scoped actuals unchanged.
7. Open a second report view in the **same tab** after changing range; **Expect**: same range (**FR-M031a**). Second browser tab may differ.

## Scenario 4  -  Category health (US6)

1. With confirmed patterns and one one-off categorised debit.
2. Open category health.
3. **Expect**: Four measures per category; under/over = allocation − actual; uncategorised warning for orphan debits; one-off not in forecast list.

## Scenario 5  -  Budget balance (US7)

1. With complete estimated income, choose report range covering fixture commitments.
2. Set estimated income (prorated to range) below max-of-ratios required income.
3. Open budget balance report.
4. **Expect**: Limiting category (tie → Needs, Wants, Savings & Debt), shortfall, yes/no gap; commitments from recurring expenses only; income side range-prorated ([budgeting-core.md](contracts/budgeting-core.md)).

## Scenario 6  -  Export and delete (US10)

1. Reauthenticate (hosted: confirm account password, or typed phrase where that flow applies); export JSON bundle and CSV ZIP separately.
2. Verify files match [export-formats.md](contracts/export-formats.md).
3. Permanent delete account data.
4. **Expect**: No retrievable transactions/plan; confirmation shown.

## Scenario 7  -  Determinism (FR-M036)

1. Fix report date range in tab; open category health and budget balance twice without data changes.
2. **Expect**: Identical totals (including banker's 2dp emission and 30.4375 proration).

## Scenario 8  -  Unit/core-only (CI)

```bash
dotnet test tests/Potsure.Budgeting.Tests -c Release
dotnet test tests/Potsure.Statements.Tests -c Release
```

**Expect**: Golden parsing files pass; budgeting golden scenarios match independently verified expected results.

## Scenario 8b  -  Dependency hygiene (constitution 1.2.0)

After any change to `Directory.Packages.props`:

```bash
dotnet package list --outdated
dotnet test Potsure.slnx -c Release
```

**Expect**: Outdated packages reviewed/upgraded where compatible; full test suite green after version bumps (AGENTS.md  -  not `--no-build` only).

## Scenario 8c  -  EF migration orchestration (Aspire local)

1. Stop AppHost; remove local SQL container/volume if reproducing cold start (optional).
2. Start AppHost (`dotnet run --project src/Potsure.AppHost/Potsure.AppHost.csproj`).
3. Observe dashboard ordering: `potsure` database healthy → migration resource runs and **completes** → web becomes healthy.
4. Confirm no `Database.MigrateAsync` (or similar) in `Potsure.Web` startup  -  schema apply is AppHost-only locally per [plan.md](plan.md).

**Expect**: Web health check passes on empty database without manual `dotnet ef database update` when migration resource is wired; `dotnet ef migrations add` still works with Infrastructure project + Web startup per plan.

**Known defect (2026-11-04 re-smoke, T154/T157)**: AppHost logs show `Could not perform substitution for environment variable ASPNETCORE_URLS` on `ef-tool-ef-migrations` with `portForServing "web"`  -  the migration executable does not produce the `/web` service endpoint. Until fixed upstream or in AppHost wiring, Scenario 8c remains **Partial** despite **T115** pass on 2026-11-03.

## Scenario 9  -  Accessibility smoke (US12)

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

Last run: 2026-11-04 (Release build/test; hosted Identity functional smoke in `Potsure.Web.Tests`; Scenario 8c re-smoke after Phase 23 Identity wiring  -  see table). **T100** records this log; **T148** / **T153** cite `HostedIdentityFunctionalTests`; **Scenario 8c** first closed under **T115** after **T113**; **Scenario 8b** re-run with **T110** when **T111** adds packages.

| Scenario | Status | Notes |
|----------|--------|-------|
| 1  -  Monzo import | Partial | Parser/import pipeline and UI present; full manual CSV walkthrough not run in this session |
| 2  -  Recurring confirmation | Partial | Review and summary pages added; confirm/split flows rely on existing pattern services |
| 3  -  Preset and standing plan income | Partial | Legacy **BudgetPeriod** UI/services; 2026-11-09 plan pivot pending converge tasks |
| 4b  -  Pot mapping modes | Not run | Spec **FR-M013**; verify after converge |
| 4  -  Category health | Partial | Report UI with textual summary; one-off service available |
| 5  -  Budget balance | Partial | Report UI with separated income measures |
| 6  -  Export and delete | Partial | Export/delete UI and contract tests pass in CI |
| 7  -  Determinism | Pass | `DeterminismTests` in Release |
| 8  -  Unit/core CI | Pass | Budgeting and Statements golden tests |
| 8b  -  Dependency hygiene | Pass | **T110** re-run 2026-11-03 after **T111** (`Aspire.Hosting.EntityFrameworkCore` preview pin); no further stable upgrades available |
| 8c  -  EF migration ordering | Partial | **T115** Pass 2026-11-03; **T154**/**T157** 2026-11-04 `aspire run`  -  `ef-tool-*` cannot substitute `ASPNETCORE_URLS` (`portForServing "web"`  -  service `/web` not produced by migration executable); AppHost switched to `AddProject<Projects.Potsure_Web>` (**T157** fix attempt)  -  re-smoke still blocked until Aspire EF tool wiring resolves |
| 9  -  Accessibility smoke | Partial | Focus styles and textual table summaries added; keyboard-only audit not run |
| Partial-month labelling | Pass | `VariableExpenseEstimatorTests` (SC-008 / **T118**) |
| Hosted Identity (Aspire local) | Pass | **T153** 2026-11-04: `HostedIdentityFunctionalTests` + manual path in bootstrap section (**SC-005** / **T148**) |
| Hosted stage smoke | Not run | Requires deployed ACA; hosted Register/sign-in validation |

Release gate commands (2026-11-03): `dotnet format --verify-no-changes`, Release build `-warnaserror`, full `dotnet test`  -  see Phase 15 task T101.

## Failure cases to spot-check

- Non-GBP row → rejected with clear error.
- Wrong bank CSV → rejected.
- Oversized upload → bounded error, no partial persist without rollback.
- Cancel mid-upload → temp cleared.
