# Implementation Plan: Potsure v1

**Branch**: `001-potsure-v1` | **Date**: 2026-11-20 | **Spec**: [spec.md](./spec.md)

**Governance**: [constitution v2.0.0](../../.specify/memory/constitution.md); agent workflow in [AGENTS.md](../../AGENTS.md) (Aspire consumption + central package versioning).

**Re-plan note (2026-11-21)**: Testing alignment with constitution v2.0.0. Automated coverage is layered unit and integration tests per area (`Potsure.*.Tests`), including golden parsing, budgeting maths, infrastructure services, and hosted Identity HTTP tests via `WebApplicationFactory`. AppHost resource modelling is not unit-tested.

**Re-plan note (2026-11-20)**: Commercial persistence pivot. Azure SQL cannot be created on the current subscription, so the running store is **no longer a database**. AppHost models **Azure Blob Storage** (Azurite locally). Each signed-in user is one tenant with one financial JSON document, plus separate identity blobs. EF Core, SQL containers, and `Potsure.MigrationsHost` leave the running path. A later database is a new implementation of the document store, not a reason to keep SQL wired now. Product behaviour (standing plan, reports, pots, Identity password sign-in) is unchanged. [tasks.md](./tasks.md) **Phase 35** (**T207**–**T233**) is the only active queue for persistence, AppHost blob wiring, and constitution 1.2.0 consumption gates; earlier `[X]` SQL/EF tasks are historical evidence only.

**Re-plan note (2026-11-09)**: Standing **budget plan** (estimated income + cadence, no plan start/end dates); **report date range** for period-scoped reports (**FR-M031a**, default current calendar month, shared per browser tab, not persisted); **pot mapping modes** (category-only, three pots, several pots; **FR-M013**–**FR-M013c**); recurring **edit/delete** and **payment anchor** (**FR-M008c**); user **fixed vs variable** override (**FR-M008d**); **30.4375** day proration and **banker's** 2dp rounding (**FR-M017**, **FR-M036**); **max-of-ratios** required income (**FR-M018**); variable commitment uses **stored monthly estimate** not live rolling average; **FR-M019** / **FR-M035** persisted on plan. Identity, Monzo import, EF migration orchestration, and Azure/Aspire topology unchanged from 2026-11-04 unless plan-model work touches them.

**Re-plan note (2026-11-04)**: Identity / hosted auth  -  password-primary ASP.NET Core Identity and Blazor Identity template UX.

**Input**: Feature specification plus planning prompt (conceptual data model, architecture, stack, Azure/Aspire stages, Monzo CSV reference layout) and **EF migrations review prompt** (Aspire migration orchestration, separation of schema apply from web startup, `dotnet ef` developer workflow).

## Summary

Potsure v1 is a **Blazor Server** web application (**.NET 10**, **Aspire** orchestration) that ingests **Monzo CSV** statements for a **single account**, detects and user-confirms **recurring income/expenses** (with **edit/delete**, **payment anchors**, and **fixed vs variable** overrides), maintains one **standing budget plan** (**estimated income** + **cadence**, preset, confirmed recurring items; **no** saved plan-period dates), and lets users pick an in-session **report date range** (shared per browser tab) for **category health**, **budget balance** (max-of-ratios **FR-M018**), and **runway** (**FR-M020**). Users choose **category-only**, **three pots**, or **several pots** mapping modes (**FR-M013**). Fixed **50/30/20** or **75/15/10** presets apply to **Needs / Wants / Savings & Debt**; allocations **prorate** with **30.4375** average-month days. A **pure budgeting core library** holds proration, commitment projection, and shortfall maths for golden unit tests; **statement parsing**, **recurrence detection**, **per-user JSON document persistence**, and **BlazorBlueprint** UI sit at the edges.

## Technical Context

**Language/Version**: C# / .NET 10 (`global.json` pinned SDK)

**Primary Dependencies**:
- ASP.NET Core Blazor Server (InteractiveServer)
- BlazorBlueprint.Components v4 (+ matching Primitives, Lucide icons)
- .NET Aspire AppHost (Azure Container Apps, Azure Blob Storage)
- Aspire Azure Blob client (`Aspire.Azure.Storage.Blobs`: `BlobContainerClient` for the `userdata` container)
- CsvHelper (Monzo CSV  -  versions in `Directory.Packages.props` only; constitution 1.2.0 + AGENTS.md)
- ASP.NET Core Identity with a custom blob-backed user store (`ApplicationUser`; cookie authentication; password hasher unchanged)
- Microsoft.Extensions.Http.Resilience (typed HTTP clients if needed)

**Future (post–v1.0.0 acceptance, optional)**: WebAuthn passkeys on login/register/manage (template-style optional submit), not a v1 primary dependency. A relational database may replace the document store later; it is not on the running path while Azure SQL cannot be provisioned.

**Storage**: Private Azure Blob container `potsure-userdata` (hosted); Azurite via Aspire `RunAsEmulator` locally, persistent volume. One `tenants/{userId}/state.json` per user. Identity account blobs and a hashed email index are separate from the financial document. Data Protection keys live in `system/data-protection-keys.xml` in the same container. Document `schemaVersion` is migrated in process when that user's file is loaded. Repositories stay in **`Potsure.Infrastructure`**. **`Potsure.Data`** keeps the document and identity types and drops EF. **`Potsure.MigrationsHost`** is removed. Local-only mode (no AppHost) writes the same relative paths under a configured directory. See [contracts/user-state-document.md](./contracts/user-state-document.md).

**Testing**: xUnit v3, NSubstitute, built-in Assert; golden files for Monzo parsing; independent expected results for budgeting core; area-appropriate integration tests (including `Potsure.Web.Tests` for hosted Identity HTTP); AppHost not unit-tested.

**Target Platform**: Linux containers on Azure Container Apps (Consumption); local Aspire + Azurite for development. No Azure SQL.

**Project Type**: Multi-project web application + pure libraries + AppHost.

**Performance / import bounds**: Interactive Blazor acceptable for one user and one Monzo account. Each save rewrites that user's JSON document (see [research.md](./research.md)). Monzo CSV import uses streaming parse, then one conditional blob replace. Row and size limits stay operator-documented. This is not a query engine.

**Constraints**:
- Budgeting core: no I/O (constitution III); inject all calendar boundaries; **30.4375** proration divisor and **banker's rounding** at report emission (**FR-M036**).
- Standing plan: one profile per user; **estimated income** + **income cadence** (no plan start/end); incomplete income → explicit **not set** on allocations and **FR-M018** (**FR-M031**).
- Reports: **report date range** independent of plan; default current month; **FR-M031a** tab-scoped session state (not persisted **FR-X006**).
- Pot modes: **category-only** | **three pots** | **several pots** with invariant enforcement and **FR-M013b** pre-selection.
- Recurring: **stored** variable monthly estimate for commitments; **FR-M034** suggestions only on user actions; reject does not permanently suppress re-suggestion (**FR-M008**).
- GBP only; no raw statements in logs. Raw CSV uploads are not stored. Normalised per-user JSON is the store.
- Local-only mode not network-exposed; hosted mode requires Identity sign-in before financial data (email/password v1).
- UK English UI/docs.
- **Aspire integration consumption** (constitution 1.2.0): the blob container is consumed via `AddAzureBlobContainerClient("userdata")` matching AppHost `AddBlobContainer("userdata")`. No hand-built storage connection strings when AppHost models the resource. Local-only mode has no AppHost resource and uses the configured data directory instead.
- **Central package management** (constitution 1.2.0): new packages only in `Directory.Packages.props` at latest stable when introduced; `dotnet package list --outdated` and full `dotnet test Potsure.slnx -c Release` after dependency upgrades (AGENTS.md). Adding `Aspire.Hosting.Azure.Storage` and `Aspire.Azure.Storage.Blobs`, and removing unused SQL/EF packages, happens in implementation under that rule.
- **Document evolution**: `schemaVersion` on `state.json`. Transforms run when that document is loaded. No Aspire migration resource and no web startup pass over every user (see [research.md](./research.md)).

**Scale/Scope**: v1 one **standing budget plan** per user plus in-session **report date ranges**; one Monzo account; one JSON document per user; Must FR-M001–M037 (incl. **FR-M008c**/**FR-M008d**, **FR-M013** modes). Deferred: Redis, SignalR, Key Vault, and a return to a relational database. Raw statement blobs stay out of v1.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Plan compliance |
|-----------|-----------------|
| I Code Quality | DI, async + CancellationToken, nullable, records for DTOs, IHttpClientFactory pattern documented in CONVENTIONS |
| II Testing | Golden Monzo tests, pure core golden maths, area test projects and hosted Identity HTTP coverage; AppHost not unit-tested |
| III Maintainability | Parsing / detection / budgeting / reporting separation; core pure |
| IV Security & Privacy | Hosted: ASP.NET Core Identity email/password (hashed credentials), cookie auth, per-user isolation (owner id selects the only blob names the request may touch; fallback authorization policy); private blob container; HTTPS; storage service encryption at rest; local-only unchanged; validation; structured logging; optional passkeys on template  -  aligned with constitution 1.2.0 |
| V Documentation | plan, research, data-model, contracts, quickstart; implement adds XML + self-host docs |
| VI Transparency | Decisions traceable to spec and research.md |
| Budget model (constitution) | Three categories, two presets, optional pots (three modes), single Monzo CSV, standing plan + report range  -  aligned with spec 2026-11-09 |
| Deployment governance | Self-host + local-only + hosted auth paths in Aspire staging model |
| Aspire resource consumption (constitution 1.2.0) | AppHost `WithReference` to blob container `userdata`; web uses `AddAzureBlobContainerClient("userdata")`. Local-only has no modelled resource and uses a data directory. No runtime connection-string bypass when AppHost is present |
| Central package versioning (constitution 1.2.0) | Dependencies listed for central management; implement/tasks gate on latest-stable pins and outdated-package sweep before dependency work is “done” |
| Document evolution (replaces EF migration gate) | No shared schema job. `schemaVersion` transforms run per document on load. Default categories are created with the user's first state document, not by a migration resource. AppHost is not unit-tested |

**Post-design re-check** (2026-11-20): Phase 1 artifacts updated for the blob document store. Budget model, pure core, and password-primary Identity are unchanged. Encryption at rest moves from SQL TDE to Azure Storage service encryption on a private container. Removing EF is proportionate: the subscription cannot host Azure SQL, and a dormant SQL stack would violate the "no unused structure" bar. **No unjustified gate violations.** Complexity Tracking table empty.

**Earlier post-design re-check** (2026-11-09): household standing plan, report date range, pot modes, proration, and recurring lifecycle. [tasks.md](./tasks.md) still records that work and the SQL stack; it is historical until a new tasks pass.

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
│   ├── budgeting-core.md
│   └── user-state-document.md
├── spec.md
├── checklists/
└── tasks.md             # Phase 2 (/speckit-tasks); Phase 35 pivot queue (2026-11-20) extends backlog
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
├── Potsure.Data/                 # Document and identity types (no EF after this pivot)
└── Potsure.Infrastructure/       # Document store, repositories, upload/export, feature services

tests/
├── Potsure.Budgeting.Tests/      # Golden maths
├── Potsure.Statements.Tests/     # Golden CSV fixtures (anonymised)
├── Potsure.Recurrence.Tests/
├── Potsure.Reporting.Tests/
├── Potsure.Infrastructure.Tests/
└── Potsure.Web.Tests/            # Hosted Identity HTTP and startup validation

samples/
└── monzo/                        # Anonymised golden CSV snippets only

docs/                             # Self-host, encryption, deployment stages (implement)
```

**Structure Decision**: Layered projects match constitution module boundaries. Web references Infrastructure + Reporting. Infrastructure owns the blob/file document store and references Data for document types. Reporting, Recurrence, and Statements reference Budgeting. After this pivot, remove `Potsure.MigrationsHost` from the solution. Do not add a new project for the store.

## Architecture overview

```text
┌─────────────────────────────────────────────────────────┐
│ Potsure.Web (Blazor Server, Identity, BlazorBlueprint)  │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│ Potsure.Infrastructure (repositories, upload/export)    │
└─┬───────────────┬────────────────────┬──────────────────┘
  │               │                    │
  ▼               ▼                    ▼
Statements    Recurrence          Reporting ──► document store (blob or local files)
  │               │                    │
  └───────────────┴──────────┬─────────┘
                             ▼
                      Potsure.Budgeting (pure)
```

## Phase 0  -  Research

Completed in [research.md](./research.md): Monzo/CsvHelper, de-dupe, **standing plan + report range**, **30.4375 proration / banker's rounding**, pot mapping modes, **payment-anchor occurrence projection**, variable expense **stored estimate vs FR-M034 suggestions**, edit/delete recurring and pots, one-off vs commitment rules, **per-user JSON in Azure Blob Storage** (2026-11-20, replaces EF/Azure SQL), Identity password sign-in on a custom store, OTel, BlazorBlueprint v4, upload/export streaming, **hosted Identity** (2026-11-04), **household plan model** (2026-11-09).

All Technical Context items resolved; no remaining **NEEDS CLARIFICATION**.

## Phase 1  -  Design & Contracts

| Artifact | Path |
|----------|------|
| Data model | [data-model.md](./data-model.md) |
| Monzo import | [contracts/monzo-import.md](./contracts/monzo-import.md) |
| Export | [contracts/export-formats.md](./contracts/export-formats.md) |
| Budgeting core API | [contracts/budgeting-core.md](./contracts/budgeting-core.md) |
| Per-user JSON store | [contracts/user-state-document.md](./contracts/user-state-document.md) |
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

Aspire 17 APIs (confirmed against `aspire docs` for Azure Blob Storage, package `Aspire.Hosting.Azure.Storage` 17.0.0):

- Keep `AddAzureContainerAppEnvironment` and `PublishAsAzureContainerApp` for the web app.
- Replace SQL with:

```csharp
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite => azurite
        .WithDataVolume("potsure-userdata")
        .WithLifetime(ContainerLifetime.Persistent));

var userdata = storage.AddBlobContainer("userdata", blobContainerName: "potsure-userdata");
```

- Web: `WithReference(userdata)`, `WaitFor(userdata)`, `WithComputeEnvironment(aca)`.
- Remove `AddAzureSqlServer`, `AddDatabase`, `schema-host`, both `AddEFMigrations` resources, and the `Potsure.MigrationsHost` project reference.
- Hosted access is managed identity. Aspire's default blob role is **Storage Blob Data Contributor**. Do not put an account key in the image. Emulator connection strings stay inside the Aspire injection for Azurite only.
- Keep the default Standard storage SKU. Do not select Premium. ACA replica guidance is unchanged (dev/test 0–1; first public min 1; commercial 2–4 after approval).
- **Constitution 1.2.0 / AGENTS.md**: AppHost is the source of truth for the storage connection. Web calls `AddAzureBlobContainerClient("userdata")` and consumes `BlobContainerClient`. Do not construct a storage connection string in application startup when AppHost models the container. Use the `aspire` and `aspireify` skills when the implementation edits AppHost.

**Dependency order**:

```text
storage (Azure Storage, Azurite locally)
  └── userdata container "potsure-userdata"
        └── web (WithReference + WaitFor)
```

There is no migration resource. The container being ready is enough for the web app to start. An absent `state.json` means "new user", not "schema missing".

### Document store

| Concern | Location | Notes |
|---------|----------|--------|
| Document and identity record types | `src/Potsure.Data/` | No EF context, no migrations folder once implementation lands |
| Blob and local-file store, repositories, first-document seeding | `src/Potsure.Infrastructure/` | Feature services stay on repository interfaces |
| Runtime client | `Potsure.Web/Program.cs` | `AddAzureBlobContainerClient("userdata")` when AppHost is hosting; data directory when `LocalOnly` |
| Data Protection | same container | `PersistKeysToAzureBlobStorage` at `system/data-protection-keys.xml` |
| Document version | `schemaVersion` on `state.json` | In-process transform on load. See [contracts/user-state-document.md](./contracts/user-state-document.md) |
| Removed | `Potsure.MigrationsHost`, `src/Potsure.Data/Migrations/`, design-time SQL factories | Do not keep them for a hypothetical return to SQL |

`docs/deployment-database.md` is obsolete after implementation and should be replaced by a short storage note (private container, Azurite volume, managed identity). Do not add `dotnet ef` steps.

### Upload pipeline

- Stream parse, validate account, stage, dedupe, then one conditional replace of `state.json`. Clear temp files on every exit.
- Do not store the raw CSV as a blob. The JSON document is normalised data only.

### Central package management (implement gate)

- Add or bump package versions only in `Directory.Packages.props` when implementation (or explicit user instruction) introduces dependencies.
- Pin each newly introduced package at the **latest stable** version available at introduction time.
- Before closing dependency-related tasks: run `dotnet package list --outdated`, upgrade centrally managed packages where compatible, then run the **full** test suite (`dotnet test Potsure.slnx -c Release`), not `--no-build` only.
- Agents MUST NOT add/remove packages without explicit user instruction except when maintaining central versions per AGENTS.md.

### Identity (hosted)

- **Stack**: ASP.NET Core Identity via a custom user store on the identity blobs in [contracts/user-state-document.md](./contracts/user-state-document.md), `ApplicationUser`, cookie authentication, and the hosted **fallback authorization policy** (authenticated user required). `ICurrentOwnerAccessor` is the only source of `userId` used to build blob names. There is no EF query filter because there is no shared database.
- **UX / structure**: Adopt the standard .NET 10 Blazor Server Identity account template layout under `Components/Account/Pages` (Login, Register, ForgotPassword, Manage, etc.), `MapAdditionalIdentityEndpoints`, `IdentityRedirectManager`, and `IdentityRevalidatingAuthenticationStateProvider`. Behavioural reference: [penny-for-your-stocks](https://github.com/markheydon/penny-for-your-stocks) (adapt routes, copy, and Potsure deployment modes  -  not a fork).
- **Presentation**: Convert template markup to BlazorBlueprint v4 (`BbCard`, `BbButton`, `BbInput`, `BbAlert`, Lucide via house patterns); UK English copy (`blazor-blueprint` skill).
- **Primary sign-in (v1)**: Email + password via `PasswordSignInAsync`; first hosted user created through **Register**, not passkey enrolment.
- **Remove from v1 path**: Hosted passkey-only UI, custom passkey-only minimal API routes (e.g. dedicated passkey sign-in/register endpoints), and passkey step-up for export/delete. Passkeys deferred post–v1.0.0 acceptance; when added, optional on login (template-style passkey submit), not mandatory.
- **Email**: Register `IEmailSender<ApplicationUser>`  -  no-op / logging sender acceptable for v1 self-host (Penny-style dev experience); production uses operator-configured SMTP or provider later (not blocking v1 plan).
- **Reauthentication**: Export and permanent deletion require password confirmation (preferred) or typed-phrase pattern aligned with amended spec; no passkey step-up for v1 acceptance.
- **LocalOnly**: Unchanged  -  no Identity account pages required; not network-exposed.

## Complexity Tracking

> No constitution violations requiring justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
|  -  |  -  |  -  |

## Next steps

Run **`/speckit-implement`** on **Phase 35** only (**T207**–**T233** in [tasks.md](./tasks.md)). Phases **1–34** product behaviour is largely complete; do not extend Azure SQL, EF migrations, or `AddSqlServerDbContext` on the running path. Close release sign-off with quickstart Scenario **8c** on the blob path (**T231**), storage encryption docs (**T230**), and full test/format gates (**T232**, **T233**). Re-run **`/speckit-tasks`** only if the spec or this plan changes materially again. Use **`/speckit-converge`** if implementation discovers new spec gaps.
