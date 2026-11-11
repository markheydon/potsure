# Research: Potsure v1

**Feature**: `001-potsure-v1`  
**Date**: 2026-11-03 (identity auth amended 2026-11-04; household plan model 2026-11-09)  
**Status**: Complete  -  all Technical Context items resolved from spec, constitution v1.2.0, AGENTS.md, and planning prompt. Amended 2026-11-03 for Aspire integration consumption, central package versioning, **EF migration orchestration**. Amended 2026-11-04 for **password-primary hosted Identity**. Amended 2026-11-09 for **standing plan**, **report date range**, **pot mapping modes**, **proration**, and recurring lifecycle (**FR-M008c**/**FR-M008d**).

## Monzo CSV parsing

**Decision**: Use **CsvHelper** (maintained .NET CSV library) with an explicit, validated column map for Monzo export headers; golden-file tests use anonymised real-layout fixtures (never commit personal rows from production exports).

**Rationale**: Monzo exports are RFC4180-style CSV with a stable header row (`Transaction ID`, `Date`, `Time`, `Type`, `Name`, …). CsvHelper supports header-based mapping, type conversion, and streaming readers for upload-size limits. Constitution requires golden-file tests for parsing.

**Alternatives considered**:
- Hand-rolled `string.Split`  -  rejected; brittle on quoted fields and `Notes and #tags`.
- Generic “bring your own bank” mapper  -  out of v1 scope (FR-M005).

**Monzo column contract** (see `contracts/monzo-import.md`): map `Transaction ID`, `Date`, `Time`, `Type`, `Name`, `Amount`, `Currency`, `Description`, `Money Out`, `Money In`; derive debit/credit sign consistently with Monzo conventions; classify `Pot transfer`, inter-pot moves, and identifiable non-spend transfers per import classification rules (**FR-M003**, **FR-M017** transaction exclusion).

## Account identity and de-duplication

**Decision**: Persist `MonzoAccountAssociation` per user; reject files with identifiable different-account metadata; require explicit confirmation when account identity is absent. De-duplicate by `Transaction ID` when mapped and present; else same **date + amount + normalised description** (trim, collapse whitespace, strip non-alphanumeric, case-insensitive compare per FR-M004).

**Rationale**: Spec clarifications and FR-M004/M005/M006; prevents silent cross-account merges.

**Alternatives considered**:
- Hash of full row  -  rejected; duplicates legitimately differ in immaterial columns.
- Fuzzy description matching  -  rejected; false merges hurt trust.

## Recurrence detection and categorisation

**Decision**: Separate **detection engine** (I/O at boundary) on normalised `Transaction` rows excluding internal transfers/pot movements; cadences weekly, fortnightly, monthly, with **annual/rare** manual confirm/edit; user confirm/merge/split/reject/**edit/delete** (**FR-M008c**); **reject does not permanently suppress** re-suggestion for the same merchant (**FR-M008**). Infer **fixed vs variable** amount behaviour in suggestions; user override authoritative at confirm/merge/split/edit (**FR-M008d**). **Fixed** expenses store **payment anchor** (default latest attributed then vendor-matched debit; user override; re-import may advance non-overridden anchor). **Variable** expenses store **user-confirmed monthly estimate** (initial from suggestion only).

**Rationale**: Constitution module boundary; spec FR-M007/M008/M008c/M008d and Session 2026-11-09.

**Alternatives considered**:
- ML classifier  -  rejected for v1 complexity and explainability.
- Auto-confirm all suggestions  -  rejected; violates confirmation workflow.
- Permanent suppress on reject  -  rejected; spec requires re-suggestion allowed.

## Standing plan vs report date range

**Decision**: Persist **one standing budget plan** per user: preset, **estimated income** amount, **income cadence** (weekly through yearly), confirmed recurring items, **forecast horizon N** (1–12 months, default 3), **actual-income rolling window N** (default 3 months, max 12), pot mapping mode, mapped pots, recorded funding. **No** plan start/end dates. Until estimated income **and** cadence are saved, allocations and **FR-M018** show explicit **not set** (not £0 maths). Reports use user-selected **report date range** (inclusive calendar days): default **current calendar month** on first open in a tab; **one shared range per browser tab** across report views; **not** persisted across visits; **not** synced across tabs (**FR-M031a**, **FR-X006**). Monthly-equivalent income = pay-period factors on estimated amount (**FR-M031**).

**Rationale**: Session 2026-11-09 clarifications; household mental model.

**Alternatives considered**:
- Single working budget period entity  -  superseded 2026-11-09.
- Persisted report presets  -  rejected (**FR-X006**).

## Pot mapping modes

**Decision**: Three explicit modes: **category-only** (no mapped-pot CRUD; no **FR-M013b** on expense flows), **three pots** (≤3 pots, ≤1 per category, one-to-one), **several pots** (unlimited pots, multiple per category). Mode switches blocked until invariants satisfied; switching to category-only requires deleting all mapped pots with **FR-M013c** funding roll/discard prompt. **FR-M013b** pre-selection rules per mode.

**Rationale**: FR-M013, FR-M014, Monzo user workflows.

**Alternatives considered**:
- Implicit “zero pots” without category-only mode  -  rejected; spec requires first-class category-only.

## Proration and money rounding

**Decision**: Report-scoped **allocation** and **estimated income** for **FR-M018**: prorate monthly-equivalent figures with **inclusive day count ÷ 30.4375** (365.25÷12). **Variable** commitments in a range: stored monthly estimate × (inclusive days ÷ 30.4375). **Fixed** commitments: occurrence count × amount per occurrence from **payment anchor** with cadence-specific stepping (monthly = calendar months + last-day clamp; annual = yearly step). **Runway window**: report range start through report end + **N** calendar months (clamped end date), then same proration rules; **actual activity** in runway compare uses **report range only**. Emit money at report boundaries with **banker's rounding** (half to even) to 2dp (**FR-M036**).

**Rationale**: FR-M017, FR-M018, FR-M020, SC-004.

**Alternatives considered**:
- Calendar-month overlap for variable commitments  -  rejected (spec Session 2026-11-09).
- 30-day month approximation  -  rejected; spec mandates 30.4375.

## Variable recurring expense forecasting

**Decision**: **FR-M034** computes last **three whole calendar months** of vendor-matched spend ending on **today** when user confirms/merges/splits/edits  -  to **suggest** monthly estimate only; report range changes MUST NOT recompute suggestions. **FR-M018** / **FR-M020** use **stored** monthly estimate until user save or explicit acceptance. Label coverage when history shorter than three months (**FR-M032**).

**Rationale**: FR-M034, FR-M008c, Session 2026-11-09 (stable commitments vs live averages).

**Alternatives considered**:
- Auto-update stored estimate on every import  -  rejected; breaks user trust and spec.
- Report-range-ended suggestion window  -  rejected; spec ends on today.

## One-off spend and commitments

**Decision**: Genuine one-offs affect **actual activity only** (FR-M037). **FR-M018** commitments = **confirmed recurring expense patterns only**, projected into the selected **report date range** (or full **runway window** for **FR-M020**). **Required income**: per category = commitments ÷ preset fraction; **overall** = **max** of three; limiting category tie **Needs → Wants → Savings & Debt**.

**Rationale**: Spec 2026-11-09; release gate for one-offs in v1.0.0.

## Budgeting core (pure library)

**Decision**: `Potsure.Budgeting`  -  pure functions/records: monthly-equivalent preset allocations; **range proration**; net actual activity; under/over; recorded funding rollup; over-allocation warnings; **occurrence-based** fixed commitment projection from anchors; **day-prorated** variable commitments; **max-of-ratios** required income / limiting category / shortfall / yes-no vs **range-prorated estimated income**; runway comparison per FR-M020. No clocks inside library  -  pass `ReportRange`, `RunwayWindow`, and anchor dates as inputs. **Banker's** 2dp at emission (**FR-M036**).

**Rationale**: Constitution II and III; SC-004 golden scenarios.

**Alternatives considered**:
- Integer pence internally  -  acceptable; emission rule still banker's 2dp.
- Sum-of-category required incomes  -  rejected; spec max-of-ratios.

## Persistence and tenancy

**Decision**: EF Core 10 + SQL Server (Azure SQL hosted, SQL container local via Aspire `RunAsContainer`); `UserId` on user-owned entities; global query filters + explicit ownership checks on mutations; migrations in source control; **idempotent reviewed SQL** or controlled migration job before public traffic (no concurrent migrate-all-replicas). Data Protection key ring in SQL via EF; reconcile with Aspire-generated DP resources to avoid duplicate stores.

**Rationale**: User prompt and FR-M025–M027; constitution IV.

**Alternatives considered**:
- SQLite for v1 local  -  deferred per prompt (future work).

## EF Core migration ownership and orchestration

**Decision (current solution shape)**: Single `PotsureDbContext` in **`src/Potsure.Data/`** with migrations and model snapshot under `Migrations/PotsureDbContext/`. **No** separate migrations-only project. `Potsure.Web` is the EF **startup project** (Aspire SQL client + Identity registration); `Potsure.Data` is the migrations **project** for `dotnet ef` and AppHost `WithMigrationsProject`. Repositories and seeders remain in `Potsure.Infrastructure`.

**Migration execution**:
- **Reject** `Database.MigrateAsync()` (or `EnsureCreated`) on normal `Potsure.Web` startup  -  schema apply is deployment/orchestration, not request pipeline.
- **Local Aspire**: Use built-in **`AddEFMigrations`** (Aspire **13.6.4** line) on the web project resource: `WithMigrationsProject` → **`Potsure.Data`**, fully qualified context `Potsure.Data.PotsureDbContext` (match AppHost registration), `WithReference(database)`, `WaitFor(database)`, `RunDatabaseUpdateOnStart()`. Web uses `WaitForCompletion(migrations)` so Blazor starts only after migrations finish successfully (finite `dotnet ef database update`, not a long-lived service). **T158** tracks Aspire `ef-tool-*` URL substitution defects separately from project choice.
- **Hosted / publish**: `RunDatabaseUpdateOnStart` is local-run only; continue **controlled single-runner** apply via CI or `dotnet ef database update` / published migration script per `docs/deployment-database.md`.
- **Application seeding** (`BudgetProfileSeeder`, fixed category rows on profile create) remains **runtime** application logic  -  not executed inside the migration resource.

**Design-time**: Keep `IDesignTimeDbContextFactory` in `Potsure.Data/DesignTime/` with `UseSqlServer` from config/localdb fallback for standalone `dotnet ef` when AppHost is not running (constitution 1.2.0  -  not a runtime bypass).

**Rationale**: EF migrations review prompt; [Aspire EF Core migrations integration](https://aspire.dev/integrations/databases/efcore/migrations/); avoids custom migration worker boilerplate while giving AppHost an explicit migration dependency; matches constitution Aspire consumption (web still uses `AddSqlServerDbContext` at runtime).

**Alternatives considered**:
- Dedicated `Potsure.Migrations` executable  -  viable if `AddEFMigrations` fails with this project layout; prefer built-in integration first on 13.6.4.
- Web `WaitFor(database)` only (current AppHost)  -  **insufficient**; web can start before schema exists.
- Migrations in Web project  -  rejected; EF model in **`Potsure.Data`** keeps module boundary clear (constitution III).

## Upload and export pipeline

**Decision**: No Blob Storage for raw statements in v1; stream parse with bounded buffers/temp files; atomic commit of normalised rows; cleanup on success/failure/cancel. Export: user chooses versioned JSON bundle or CSV ZIP (transactions + plan/metadata CSV) per spec clarification.

**Rationale**: User prompt upload policy; FR-M022.

## Identity

**Decision**: **ASP.NET Core Identity** on SQL Server using the existing `PotsureDbContext` / `ApplicationUser`, **cookie authentication**, and standard Identity password hashing (no plaintext passwords). Hosted financial data access remains gated by **`ICurrentOwnerAccessor`**, EF **global query filters**, and **`HostedFinancialDataAuthorizationMiddleware`**. **Primary v1 sign-in**: email + password (`PasswordSignInAsync`). **UX**: adopt the .NET 10 Blazor Server Identity **account template** structure (`Components/Account/Pages` for Login, Register, ForgotPassword, Manage, etc.; `MapAdditionalIdentityEndpoints`; `IdentityRedirectManager`; `IdentityRevalidatingAuthenticationStateProvider`), with behaviour aligned to [penny-for-your-stocks](https://github.com/markheydon/penny-for-your-stocks) and adapted for Potsure deployment modes. **Presentation**: explicit implement task to convert template markup to **BlazorBlueprint v4** (`BbCard`, `BbButton`, `BbInput`, `BbAlert`, Lucide icons per house patterns). **Passkeys**: deferred beyond v1.0.0 acceptance; when added later, optional on login/register/manage (template-style passkey submit), **not** mandatory and **not** passkey-only hosted login. Remove v1 path dependence on custom passkey-only endpoints and passkey-only enrolment UI. **Reauthentication** before export, permanent deletion, and sensitive manage actions: confirm current **password** (preferred) or typed-phrase pattern per amended spec  -  **no passkey step-up** for v1 acceptance. **Local-only** mode unchanged (no Identity pages; not network-exposed) per FR-M024.

**Email (`IEmailSender<ApplicationUser>`)**: Register a development/self-host **no-op or logging sender** for v1 (acceptable for local Aspire and Penny-style self-host without SMTP). Production hosted deployments document operator configuration of a real mail provider later; email confirmation and password reset flows may be limited until then  -  **does not block** v1 plan when `RequireConfirmedAccount` remains disabled for self-host parity.

**Rationale**: Constitution IV (v1.2.0) mandates Identity with password credentials and forbids passkeys as the only sign-in method for hosted v1; spec Session 2026-11-04 and FR-M025/M022/M023.

**Alternatives considered**:
- Hosted passkey-only login (prior plan)  -  **rejected**; incompatible with constitution 1.2.0 and amended spec.
- Mandatory passkeys at v1  -  **rejected**; optional enhancement post–v1.0.0 acceptance only.
- Stock template HTML/Tailwind without BlazorBlueprint  -  **rejected**; product UI standards require Bb* conversion.

## UI stack

**Decision**: Blazor Server **InteractiveServer**, **BlazorBlueprint.Components** v4 (matching Primitives), `AddBlazorBlueprintComponents()`, `blazorblueprint.css` + `theme-init.js` in `App.razor`, `BbPortalHost` in interactive root layout, Lucide icons and built-in chart components only.

**Rationale**: House standards; official setup docs (blazorblueprintui.com llms).

**Alternatives considered**:
- Plain Tailwind-only UI  -  rejected by product standards.
- Separate chart library  -  rejected; use BlazorBlueprint charts.

## Aspire AppHost and deployment stages

**Decision**: Single AppHost modelling web project, ACA environment, Azure SQL (`AddAzureSqlServer`, `RunAsContainer` locally, `AddDatabase`, `WithReference`, `WaitFor`, `PublishAsAzureContainerApp`); explicit SKU/replica/CPU/memory per stage (dev/test min 0 max 1; first public min 1; commercial 2+ replicas after approval). UK South; validate pricing/free offers at deploy time; no ACA Express; no App Insights by default on private dev.

**Runtime consumption (constitution 1.2.0)**: Web project registers SQL through the Aspire SQL Server client integration  -  `AddSqlServerDbContext<PotsureDbContext>("potsure")` matching the AppHost database reference name. Application startup MUST NOT bypass integration DI with manual connection strings when AppHost models SQL. EF Core design-time factory may read `appsettings`/environment for migration commands only.

**Migration resource (review prompt)**: AppHost models `database → AddEFMigrations → web` with `WaitForCompletion`; inspect with `aspire describe --include-hidden` after implement. Do not upgrade Aspire major versions solely to gain `AddEFMigrations`  -  already available on pinned 13.6.4 hosting line.

**Rationale**: User prompt Azure/Aspire policy; constitution deployment governance and Aspire resource consumption mandate; AGENTS.md operational steps (`aspire` / `aspireify` skills for AppHost changes).

**Alternatives considered**:
- AKS / Functions / Redis / SignalR / Blob / Key Vault  -  deferred per release gates until measured need.
- Manual `UseSqlServer(connectionString)` in `Program.cs` from configuration  -  rejected at runtime; breaks AppHost parity and violates constitution 1.2.0.

## OpenTelemetry

**Decision**: Public stages → Application Insights / Log Analytics via OpenTelemetry; sample successful traces; retain errors; ~0.1 GB/day cap, 30–31 day retention; **exclude** bodies, uploads, payee text, account refs, exports, sensitive SQL parameters.

**Rationale**: User prompt network/telemetry section; constitution IV (no raw statements in logs).

## CSV library note (package governance)

**Decision**: Add **CsvHelper** (and Aspire/EF/BlazorBlueprint packages) via central `Directory.Packages.props` when implementing  -  not done in plan phase per AGENTS.md package rule; recorded here as the chosen parser. Each new package version MUST be the latest stable at introduction time (constitution 1.2.0).

**Completion gate (constitution 1.2.0 / AGENTS.md)**: Before dependency-related implementation is considered complete, run `dotnet package list --outdated`, upgrade centrally managed packages where compatible, then run the full solution test suite (`dotnet test Potsure.slnx -c Release`) after any version change.

**Rationale**: Agent constraint on unprompted package adds; constitution central package versioning reduces vulnerability drift and restore skew.
