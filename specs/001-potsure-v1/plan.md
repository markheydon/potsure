# Implementation Plan: Potsure v1

**Branch**: `001-potsure-v1` | **Date**: 2026-11-04 | **Spec**: [spec.md](./spec.md)

**Governance**: [constitution v1.2.0](../../.specify/memory/constitution.md); agent workflow in [AGENTS.md](../../AGENTS.md) (Aspire consumption + central package versioning).

**Re-plan note (2026-11-04)**: Identity / hosted auth sections updated for password-primary ASP.NET Core Identity and Blazor Identity template UX; budgeting, Monzo import, EF migration orchestration, and Azure/Aspire topology unchanged unless auth wiring requires it.

**Input**: Feature specification plus planning prompt (conceptual data model, architecture, stack, Azure/Aspire stages, Monzo CSV reference layout) and **EF migrations review prompt** (Aspire migration orchestration, separation of schema apply from web startup, `dotnet ef` developer workflow).

## Summary

Potsure v1 is a **Blazor Server** web application (**.NET 10**, **Aspire** orchestration) that ingests **Monzo CSV** statements for a **single account**, detects and user-confirms **recurring income/expenses**, applies fixed **50/30/20** or **75/15/10** presets across **Needs / Wants / Savings & Debt**, optionally maps **Monzo pots** to categories without double-counting, and reports **category health**, **required income / limiting category**, **runway warnings**, and **export/deletion**. A **pure budgeting core library** holds all ratio and shortfall maths for golden unit tests; **statement parsing**, **recurrence detection**, **EF Core persistence**, and **BlazorBlueprint** UI sit at the edges.

## Technical Context

**Language/Version**: C# / .NET 10 (`global.json` pinned SDK)

**Primary Dependencies**:
- ASP.NET Core Blazor Server (InteractiveServer)
- BlazorBlueprint.Components v4 (+ matching Primitives, Lucide icons)
- .NET Aspire AppHost (Azure Container Apps, Azure SQL integrations)
- EF Core 10 + SQL Server provider
- CsvHelper (Monzo CSV — versions in `Directory.Packages.props` only; constitution 1.2.0 + AGENTS.md)
- ASP.NET Core Identity + EF Core stores on SQL Server (`ApplicationUser`, existing `PotsureDbContext`; cookie authentication)
- Microsoft.Extensions.Http.Resilience (typed HTTP clients if needed)

**Future (post–v1.0.0 acceptance, optional)**: WebAuthn passkeys on login/register/manage (template-style optional submit), not a v1 primary dependency.

**Storage**: Azure SQL Database (hosted); SQL Server container locally (`RunAsContainer`). EF model and migrations in `Potsure.Infrastructure` (single `PotsureDbContext` — no separate migrations-only project unless structure later justifies it). Schema apply is an **explicit orchestration step** (Aspire `AddEFMigrations` locally; controlled CI/job or publish migration script/bundle for hosted — not `Database.MigrateAsync()` on web startup). Application data seeding (`BudgetProfileSeeder`, etc.) stays separate from migration execution. UserId on owned tables, query filters, explicit ownership checks. Data Protection key ring in SQL.

**Testing**: xUnit v3, NSubstitute, built-in Assert; golden files for Monzo parsing; independent expected results for budgeting core; Playwright C# for key journeys; AppHost not unit-tested.

**Target Platform**: Linux containers on Azure Container Apps (Consumption); local Aspire + Docker SQL for development.

**Project Type**: Multi-project web application + pure libraries + AppHost.

**Performance / import bounds**: Interactive Blazor acceptable for single-user/small hosted instances; Monzo CSV import uses streaming parse within operator-documented row/size limits (see [research.md](./research.md)); bounded DB retries compatible with SQL auto-pause on dev/test tiers.

**Constraints**:
- Budgeting core: no I/O (constitution III).
- GBP only; no raw statements in logs; no Blob retention for uploads v1.
- Local-only mode not network-exposed; hosted mode requires Identity sign-in before financial data (email/password v1).
- UK English UI/docs.
- **Aspire integration consumption** (constitution 1.2.0): SQL and other AppHost-modelled resources consumed at runtime via matching Aspire client registrations (e.g. `AddSqlServerDbContext` with the AppHost database reference name); no ad hoc connection strings in application startup when AppHost already models the resource.
- **Central package management** (constitution 1.2.0): new packages only in `Directory.Packages.props` at latest stable when introduced; `dotnet package list --outdated` and full `dotnet test Potsure.slnx -c Release` after dependency upgrades (AGENTS.md).
- **EF migration orchestration**: finite migration apply before database-dependent resources start; web must not own schema migration at startup; Aspire resource graph `database → migrations → web` with `WaitFor` / `WaitForCompletion` (see [research.md](./research.md)).

**Scale/Scope**: v1 single working budget period per user; one Monzo account; Must FR-M001–M037; deferred services per release gates (Redis, SignalR, Blob staging, Key Vault, etc.).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Plan compliance |
|-----------|-----------------|
| I Code Quality | DI, async + CancellationToken, nullable, records for DTOs, IHttpClientFactory pattern documented in CONVENTIONS |
| II Testing | Golden Monzo tests, pure core golden maths, Playwright journeys planned |
| III Maintainability | Parsing / detection / budgeting / reporting separation; core pure |
| IV Security & Privacy | Hosted: ASP.NET Core Identity email/password (hashed credentials), cookie auth, per-user isolation (`ICurrentOwnerAccessor`, EF query filters, fallback authorization policy); local-only unchanged; validation, encryption, structured logging; optional passkeys on template — aligned with constitution 1.2.0 |
| V Documentation | plan, research, data-model, contracts, quickstart; implement adds XML + self-host docs |
| VI Transparency | Decisions traceable to spec and research.md |
| Budget model (constitution) | Three categories, two presets, optional pots, single Monzo CSV — aligned |
| Deployment governance | Self-host + local-only + hosted auth paths in Aspire staging model |
| Aspire resource consumption (constitution 1.2.0) | AppHost `WithReference` to SQL; web uses `AddSqlServerDbContext<PotsureDbContext>("potsure")`; EF design-time factory reads config for migrations only — not runtime bypass |
| Central package versioning (constitution 1.2.0) | Dependencies listed for central management; implement/tasks gate on latest-stable pins and outdated-package sweep before dependency work is “done” |
| EF migration execution (review prompt + constitution II) | No startup `MigrateAsync` on web; AppHost models explicit migration resource; schema seeding not bundled into migration job; AppHost not unit-tested |

**Post-design re-check** (2026-11-04): Phase 1 artifacts respect module boundaries and non-negotiable budget rules. Hosted auth plan uses password-primary Identity (constitution 1.2.0); no passkey-only mandate. Aspire client consumption, central package workflow, and EF migration orchestration (`AddEFMigrations`, design-time factory, hosted apply path) remain documented in this plan, [research.md](./research.md), and [quickstart.md](./quickstart.md). **No unjustified gate violations.** Complexity Tracking table empty.

## Project Structure

### Documentation (this feature)

```text
specs/001-potsure-v1/
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1 validation guide
├── contracts/           # Phase 1 interface contracts
│   ├── monzo-import.md
│   ├── export-formats.md
│   └── budgeting-core.md
├── spec.md
├── checklists/
└── tasks.md             # Phase 2 (/speckit-tasks — complete)
```

### Source Code (repository root)

```text
src/
├── Potsure.AppHost/              # Aspire orchestration, stage config
├── Potsure.Web/                  # Blazor Server, Identity, UI (BlazorBlueprint)
├── Potsure.Budgeting/            # Pure budgeting core (no I/O)
├── Potsure.Statements/           # Monzo CSV parse, validate, classify, dedupe
├── Potsure.Recurrence/           # Pattern detection + projection (I/O-free logic + interfaces)
├── Potsure.Reporting/            # PeriodSummary, report DTO assembly (uses Budgeting)
└── Potsure.Infrastructure/       # EF Core, repositories, upload pipeline, export stream

tests/
├── Potsure.Budgeting.Tests/      # Golden maths
├── Potsure.Statements.Tests/     # Golden CSV fixtures (anonymised)
├── Potsure.Recurrence.Tests/
├── Potsure.Reporting.Tests/
├── Potsure.Infrastructure.Tests/
├── Potsure.Web.Tests/            # Component/integration where appropriate
└── Potsure.E2E/                  # Playwright

samples/
└── monzo/                        # Anonymised golden CSV snippets only

docs/                             # Self-host, encryption, deployment stages (implement)
```

**Structure Decision**: Expand from current placeholder `src/Potsure/` into layered projects matching constitution module boundaries. Web references Infrastructure + Reporting; Infrastructure references Data + statement/recurrence adapters; Reporting, Recurrence, and Statements reference Budgeting for shared domain types and calculators (`TransactionKind`, etc.).

## Architecture overview

```text
┌─────────────────────────────────────────────────────────┐
│ Potsure.Web (Blazor Server, Identity, BlazorBlueprint)  │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│ Potsure.Infrastructure (EF, upload/export, auth hooks)  │
└─┬───────────────┬────────────────────┬──────────────────┘
  │               │                    │
  ▼               ▼                    ▼
Statements    Recurrence          Reporting
  │               │                    │
  └───────────────┴──────────┬─────────┘
                             ▼
                      Potsure.Budgeting (pure)
```

## Phase 0 — Research

Completed in [research.md](./research.md): Monzo/CsvHelper, de-dupe, variable expense averaging with partial-month normalisation, one-off vs commitment rules, EF/Aspire/Identity/OTel, BlazorBlueprint v4, staged ACA/SQL policy, upload/export streaming, **EF migration ownership and Aspire `AddEFMigrations` orchestration** (2026-11-03), **hosted Identity template + password-primary auth** (2026-11-04).

All Technical Context items resolved; no remaining **NEEDS CLARIFICATION**.

## Phase 1 — Design & Contracts

| Artifact | Path |
|----------|------|
| Data model | [data-model.md](./data-model.md) |
| Monzo import | [contracts/monzo-import.md](./contracts/monzo-import.md) |
| Export | [contracts/export-formats.md](./contracts/export-formats.md) |
| Budgeting core API | [contracts/budgeting-core.md](./contracts/budgeting-core.md) |
| Validation guide | [quickstart.md](./quickstart.md) |

## Implementation notes (for tasks phase)

### BlazorBlueprint integration

- Target `net10.0`, InteractiveServer.
- `AddBlazorBlueprintComponents()` once in `Program.cs`.
- `App.razor` head: `_content/BlazorBlueprint.Components/blazorblueprint.css`, `theme-init.js`.
- Imports: `BlazorBlueprint.Components`, `BlazorBlueprint.Primitives`, `BlazorBlueprint.Primitives.Services`, `BlazorBlueprint.Icons.Lucide.Components`.
- `BbPortalHost` in interactive root layout.
- Charts/icons per official docs (blazorblueprintui.com llms + chart docs).

### AppHost resource model

- `AddAzureContainerAppEnvironment`, `AddAzureSqlServer` + `RunAsContainer` locally, `AddDatabase("potsure")`, `PublishAsAzureContainerApp`.
- Customise SQL SKU/offer, ACA CPU/memory/replicas/ingress per stage (dev/test 0–1 replica; public min 1; commercial 2–4 after approval).
- Managed identity to SQL; no password in image; reconcile Data Protection with SQL key ring.
- **Constitution 1.2.0 / AGENTS.md**: AppHost is the source of truth for connection configuration. Application projects register SQL via the Aspire SQL Server client integration (`AddSqlServerDbContext` with the same reference name as AppHost). Do not construct runtime SQL access from guessed or hard-coded connection strings. Use `aspire` / `aspireify` skills when changing AppHost wiring. EF `IDesignTimeDbContextFactory` may read local config for migration tooling only.

**Dependency order (database → migrations → web)** — adapt to actual project names; do not copy blindly:

```text
sql (Azure SQL resource)
  └── database "potsure"
        └── ef-migrations (AddEFMigrations — WaitFor database, finite apply, exit success)
              └── web (WithReference database; WaitForCompletion migrations)
```

- Prefer **Aspire 13.6 `AddEFMigrations`** on the `Potsure.Web` project resource (startup project for EF CLI) with `WithMigrationsProject` pointing at **`Potsure.Data`**, explicit `DbContext` type names `Potsure.Data.PotsureDbContext` and `Potsure.Data.PotsureDataProtectionDbContext`, `WithReference(database)`, `WaitFor(database)`, and `RunDatabaseUpdateOnStart()` for local `aspire run` / AppHost dev only.
- Web resource: keep `WithReference(database)`; replace “web starts when database is healthy only” with **`WaitForCompletion(migrations)`** so schema exists before Blazor/Identity touch SQL.
- AppHost references **`Potsure.Data`** for `WithMigrationsProject` — not a runtime dependency inversion (Data must not reference AppHost).
- **Hosted publish**: `RunDatabaseUpdateOnStart` does not deploy; use `PublishAsMigrationScript()` / migration bundle and/or the controlled job documented in `docs/deployment-database.md` (single runner, no migrate-all-replicas).
- Do **not** add a dedicated migration worker project unless `AddEFMigrations` proves incompatible with this layout after inspection.
- Do **not** call `Database.MigrateAsync()` (or equivalent) from `Potsure.Web` startup; do **not** combine `BudgetProfileSeeder` or other app-data init into the migration resource.

### EF Core migrations architecture

| Concern | Location | Notes |
|---------|----------|--------|
| `PotsureDbContext`, entities, `IEntityTypeConfiguration` | `src/Potsure.Data/` | Tenancy filters, Identity + Data Protection keys |
| Migration artefacts (`Migrations/`, snapshot) | `src/Potsure.Data/Migrations/` | Source-controlled; created via `dotnet ef migrations add` |
| Design-time `DbContext` | `DesignTime/` factories in `Potsure.Data` | `UseSqlServer` + config fallback **for `dotnet ef` only** (T108); see `docs/deployment-database.md` |
| Runtime SQL registration | `Potsure.Web/Program.cs` | `AddSqlServerDbContext<PotsureDbContext>("potsure")` |
| Repositories, services, seeding | `src/Potsure.Infrastructure/` | Feature orchestration under `Features/`; `Persistence/` for repositories and seeders |
| Schema apply (local Aspire) | AppHost `AddEFMigrations` resource | Runs `dotnet ef database update`; completes then exits |
| Schema apply (hosted) | CI / release job or published script | Idempotent reviewed SQL; backup/restore discipline |

**Developer commands** (unchanged intent):

```bash
dotnet ef migrations add <Name> \
  --project src/Potsure.Data/Potsure.Data.csproj \
  --startup-project src/Potsure.Web/Potsure.Web.csproj

dotnet ef database update \
  --project src/Potsure.Data/Potsure.Data.csproj \
  --startup-project src/Potsure.Web/Potsure.Web.csproj
```

Implement: add `Aspire.Hosting.EntityFrameworkCore` to AppHost via central packages (same Aspire line as existing 13.6.4 integrations) when executing T111–T114 in [tasks.md](./tasks.md).

### Central package management (implement gate)

- Add or bump package versions only in `Directory.Packages.props` when implementation (or explicit user instruction) introduces dependencies.
- Pin each newly introduced package at the **latest stable** version available at introduction time.
- Before closing dependency-related tasks: run `dotnet package list --outdated`, upgrade centrally managed packages where compatible, then run the **full** test suite (`dotnet test Potsure.slnx -c Release`), not `--no-build` only.
- Agents MUST NOT add/remove packages without explicit user instruction except when maintaining central versions per AGENTS.md.

### Upload pipeline

- Stream parse → validate account → stage → dedupe → atomic commit; clear temp on all exits; no durable raw Blob v1.

### Identity (hosted)

- **Stack**: ASP.NET Core Identity on SQL Server via existing `PotsureDbContext` / `ApplicationUser`; cookie authentication; per-user isolation via `ICurrentOwnerAccessor`, EF global query filters, and hosted **fallback authorization policy** (authenticated user required).
- **UX / structure**: Adopt the standard .NET 10 Blazor Server Identity account template layout under `Components/Account/Pages` (Login, Register, ForgotPassword, Manage, etc.), `MapAdditionalIdentityEndpoints`, `IdentityRedirectManager`, and `IdentityRevalidatingAuthenticationStateProvider`. Behavioural reference: [penny-for-your-stocks](https://github.com/markheydon/penny-for-your-stocks) (adapt routes, copy, and Potsure deployment modes — not a fork).
- **Presentation**: Convert template markup to BlazorBlueprint v4 (`BbCard`, `BbButton`, `BbInput`, `BbAlert`, Lucide via house patterns); UK English copy (`blazor-blueprint` skill).
- **Primary sign-in (v1)**: Email + password via `PasswordSignInAsync`; first hosted user created through **Register**, not passkey enrolment.
- **Remove from v1 path**: Hosted passkey-only UI, custom passkey-only minimal API routes (e.g. dedicated passkey sign-in/register endpoints), and passkey step-up for export/delete. Passkeys deferred post–v1.0.0 acceptance; when added, optional on login (template-style passkey submit), not mandatory.
- **Email**: Register `IEmailSender<ApplicationUser>` — no-op / logging sender acceptable for v1 self-host (Penny-style dev experience); production uses operator-configured SMTP or provider later (not blocking v1 plan).
- **Reauthentication**: Export and permanent deletion require password confirmation (preferred) or typed-phrase pattern aligned with amended spec; no passkey step-up for v1 acceptance.
- **LocalOnly**: Unchanged — no Identity account pages required; not network-exposed.

## Complexity Tracking

> No constitution violations requiring justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Next steps

`tasks.md` is generated; run `/speckit-implement` starting with **Phase 23** (**T143**–**T148**) and release-evidence tasks **T149**–**T151**. Pre-implement `/speckit-analyze` gate: artifacts aligned 2026-11-04; open checkboxes are implement backlog, not spec gaps.
