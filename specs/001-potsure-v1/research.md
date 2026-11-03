# Research: Potsure v1

**Feature**: `001-potsure-v1`  
**Date**: 2026-11-03  
**Status**: Complete — all Technical Context items resolved from spec, constitution v1.1.0, AGENTS.md, and planning prompt (Azure/Aspire baseline included in prompt; no separate attachment). Amended 2026-11-03 for Aspire integration consumption, central package versioning gates, and **EF migration orchestration** (EF migrations review prompt).

## Monzo CSV parsing

**Decision**: Use **CsvHelper** (maintained .NET CSV library) with an explicit, validated column map for Monzo export headers; golden-file tests use anonymised real-layout fixtures (never commit personal rows from production exports).

**Rationale**: Monzo exports are RFC4180-style CSV with a stable header row (`Transaction ID`, `Date`, `Time`, `Type`, `Name`, …). CsvHelper supports header-based mapping, type conversion, and streaming readers for upload-size limits. Constitution requires golden-file tests for parsing.

**Alternatives considered**:
- Hand-rolled `string.Split` — rejected; brittle on quoted fields and `Notes and #tags`.
- Generic “bring your own bank” mapper — out of v1 scope (FR-M005).

**Monzo column contract** (see `contracts/monzo-import.md`): map `Transaction ID`, `Date`, `Time`, `Type`, `Name`, `Amount`, `Currency`, `Description`, `Money Out`, `Money In`; derive debit/credit sign consistently with Monzo conventions; classify `Pot transfer`, inter-pot moves, and identifiable non-spend transfers per FR-M017.

## Account identity and de-duplication

**Decision**: Persist `MonzoAccountAssociation` per user; reject files with identifiable different-account metadata; require explicit confirmation when account identity is absent. De-duplicate by `Transaction ID` when mapped and present; else same **date + amount + normalised description** (trim, collapse whitespace, strip non-alphanumeric, case-insensitive compare per FR-M004).

**Rationale**: Spec clarifications and FR-M004/M005/M006; prevents silent cross-account merges.

**Alternatives considered**:
- Hash of full row — rejected; duplicates legitimately differ in immaterial columns.
- Fuzzy description matching — rejected; false merges hurt trust.

## Recurrence detection and categorisation

**Decision**: Separate **detection engine** (I/O at boundary) operating on normalised `Transaction` rows excluding classified internal transfers/pot movements; cadences weekly, fortnightly, monthly; user confirm/merge/split/reject; short history uses user-led confirmation without claiming complete annual detection.

**Rationale**: Constitution module boundary; spec FR-M007/M008 and edge cases on history gaps.

**Alternatives considered**:
- ML classifier — rejected for v1 complexity and explainability.
- Auto-confirm all suggestions — rejected; violates confirmation workflow.

## Variable recurring expense forecasting

**Decision**: For confirmed **variable** recurring expenses, monthly estimate = average of **calendar-month totals** over the last three complete or partial months with data, or fewer months when history is shorter; label coverage window and qualify incomplete months. **Partial months**: normalise by counting only days with statement coverage within each calendar month (missing days are **unknown**, not zero spend); disclose gaps in UI and FR-M032.

**Rationale**: FR-M034, user prompt on groceries and irregular timing; avoids treating upload gaps as £0 spend.

**Alternatives considered**:
- Daily rate × days in month — rejected when import gaps exist; understates uncertainty.
- Single last payment amount — rejected for variable groceries.

## One-off spend and commitments

**Decision**: Genuine one-offs affect **actual activity only** (FR-M037); never projected into FR-M018 commitments or runway recurring projections. FR-M018 commitments = **confirmed recurring expense patterns only**, cadence-projected into the selected inclusive period.

**Rationale**: Spec clarifications and release gate for one-offs in v1.0.0.

## Budgeting core (pure library)

**Decision**: `Potsure.Budgeting` (name TBD in implement) — pure functions/records: preset application, category allocation from budgeted income, net actual activity, under/over (= allocation − actual), recorded funding rollup (pots + direct, count once), over-allocation warnings, required income / limiting category / shortfall / yes-no balance, runway comparison `(allocation − actual + recorded funding)` vs projected remaining spend. Money as `decimal` with explicit **GBP 2dp rounding** at report boundaries; document tie-breaking for limiting category (e.g. highest required income, then category order Needs → Wants → Savings & Debt).

**Rationale**: Constitution II and III; SC-004 golden scenarios.

**Alternatives considered**:
- Integer pence internally — acceptable implementation detail; plan requires documented rounding at outputs.

## Persistence and tenancy

**Decision**: EF Core 10 + SQL Server (Azure SQL hosted, SQL container local via Aspire `RunAsContainer`); `UserId` on user-owned entities; global query filters + explicit ownership checks on mutations; migrations in source control; **idempotent reviewed SQL** or controlled migration job before public traffic (no concurrent migrate-all-replicas). Data Protection key ring in SQL via EF; reconcile with Aspire-generated DP resources to avoid duplicate stores.

**Rationale**: User prompt and FR-M025–M027; constitution IV.

**Alternatives considered**:
- SQLite for v1 local — deferred per prompt (future work).

## EF Core migration ownership and orchestration

**Decision (current solution shape)**: Single `PotsureDbContext` in `Potsure.Infrastructure` with migrations and model snapshot under `Persistence/Migrations/`. **No** separate migrations-only project — one database boundary, model and artefacts already co-located cleanly. `Potsure.Web` is the EF **startup project** (Aspire SQL client + Identity registration); Infrastructure holds the model assembly for `--project`.

**Migration execution**:
- **Reject** `Database.MigrateAsync()` (or `EnsureCreated`) on normal `Potsure.Web` startup — schema apply is deployment/orchestration, not request pipeline.
- **Local Aspire**: Use built-in **`AddEFMigrations`** (Aspire **13.6.4** line — add `Aspire.Hosting.EntityFrameworkCore` to AppHost at implement time) on the web project resource: `WithMigrationsProject<Projects.Potsure_Infrastructure>()`, fully qualified context `Potsure.Infrastructure.Persistence.PotsureDbContext`, `WithReference(database)`, `WaitFor(database)`, `RunDatabaseUpdateOnStart()`. Web uses `WaitForCompletion(migrations)` so Blazor starts only after migrations finish successfully (finite `dotnet ef database update`, not a long-lived service).
- **Hosted / publish**: `RunDatabaseUpdateOnStart` is local-run only; continue **controlled single-runner** apply via CI or `dotnet ef database update` / published migration script per `docs/deployment-database.md`.
- **Application seeding** (`BudgetProfileSeeder`, fixed category rows on profile create) remains **runtime** application logic — not executed inside the migration resource.

**Design-time**: Keep `IDesignTimeDbContextFactory` in Infrastructure with `UseSqlServer` from config/localdb fallback for standalone `dotnet ef` when AppHost is not running (constitution 1.1.0 — not a runtime bypass).

**Rationale**: EF migrations review prompt; [Aspire EF Core migrations integration](https://aspire.dev/integrations/databases/efcore/migrations/); avoids custom migration worker boilerplate while giving AppHost an explicit migration dependency; matches constitution Aspire consumption (web still uses `AddSqlServerDbContext` at runtime).

**Alternatives considered**:
- Dedicated `Potsure.Migrations` executable — viable if `AddEFMigrations` fails with this project layout; prefer built-in integration first on 13.6.4.
- Web `WaitFor(database)` only (current AppHost) — **insufficient**; web can start before schema exists.
- Migrations in Web project — rejected; keeps domain/EF model in Infrastructure module boundary (constitution III).

## Upload and export pipeline

**Decision**: No Blob Storage for raw statements in v1; stream parse with bounded buffers/temp files; atomic commit of normalised rows; cleanup on success/failure/cancel. Export: user chooses versioned JSON bundle or CSV ZIP (transactions + plan/metadata CSV) per spec clarification.

**Rationale**: User prompt upload policy; FR-M022.

## Identity

**Decision**: ASP.NET Core Identity **passkeys** with SQL persistence; hosted = passkey required (guidance only if unavailable); local-only mode without network auth per FR-M024. Reauthentication before export, deletion, recovery, credential removal.

**Rationale**: Constitution IV; spec clarifications.

## UI stack

**Decision**: Blazor Server **InteractiveServer**, **BlazorBlueprint.Components** v4 (matching Primitives), `AddBlazorBlueprintComponents()`, `blazorblueprint.css` + `theme-init.js` in `App.razor`, `BbPortalHost` in interactive root layout, Lucide icons and built-in chart components only.

**Rationale**: House standards; official setup docs (blazorblueprintui.com llms).

**Alternatives considered**:
- Plain Tailwind-only UI — rejected by product standards.
- Separate chart library — rejected; use BlazorBlueprint charts.

## Aspire AppHost and deployment stages

**Decision**: Single AppHost modelling web project, ACA environment, Azure SQL (`AddAzureSqlServer`, `RunAsContainer` locally, `AddDatabase`, `WithReference`, `WaitFor`, `PublishAsAzureContainerApp`); explicit SKU/replica/CPU/memory per stage (dev/test min 0 max 1; first public min 1; commercial 2+ replicas after approval). UK South; validate pricing/free offers at deploy time; no ACA Express; no App Insights by default on private dev.

**Runtime consumption (constitution 1.1.0)**: Web project registers SQL through the Aspire SQL Server client integration — `AddSqlServerDbContext<PotsureDbContext>("potsure")` matching the AppHost database reference name. Application startup MUST NOT bypass integration DI with manual connection strings when AppHost models SQL. EF Core design-time factory may read `appsettings`/environment for migration commands only.

**Migration resource (review prompt)**: AppHost models `database → AddEFMigrations → web` with `WaitForCompletion`; inspect with `aspire describe --include-hidden` after implement. Do not upgrade Aspire major versions solely to gain `AddEFMigrations` — already available on pinned 13.6.4 hosting line.

**Rationale**: User prompt Azure/Aspire policy; constitution deployment governance and Aspire resource consumption mandate; AGENTS.md operational steps (`aspire` / `aspireify` skills for AppHost changes).

**Alternatives considered**:
- AKS / Functions / Redis / SignalR / Blob / Key Vault — deferred per release gates until measured need.
- Manual `UseSqlServer(connectionString)` in `Program.cs` from configuration — rejected at runtime; breaks AppHost parity and violates constitution 1.1.0.

## OpenTelemetry

**Decision**: Public stages → Application Insights / Log Analytics via OpenTelemetry; sample successful traces; retain errors; ~0.1 GB/day cap, 30–31 day retention; **exclude** bodies, uploads, payee text, account refs, exports, sensitive SQL parameters.

**Rationale**: User prompt network/telemetry section; constitution IV (no raw statements in logs).

## CSV library note (package governance)

**Decision**: Add **CsvHelper** (and Aspire/EF/BlazorBlueprint packages) via central `Directory.Packages.props` when implementing — not done in plan phase per AGENTS.md package rule; recorded here as the chosen parser. Each new package version MUST be the latest stable at introduction time (constitution 1.1.0).

**Completion gate (constitution 1.1.0 / AGENTS.md)**: Before dependency-related implementation is considered complete, run `dotnet package list --outdated`, upgrade centrally managed packages where compatible, then run the full solution test suite (`dotnet test Potsure.slnx -c Release`) after any version change.

**Rationale**: Agent constraint on unprompted package adds; constitution central package versioning reduces vulnerability drift and restore skew.
