# Implementation Plan: Potsure v1

**Branch**: `001-potsure-v1` | **Date**: 2026-11-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification plus planning prompt (conceptual data model, architecture, stack, Azure/Aspire stages, Monzo CSV reference layout).

## Summary

Potsure v1 is a **Blazor Server** web application (**.NET 10**, **Aspire** orchestration) that ingests **Monzo CSV** statements for a **single account**, detects and user-confirms **recurring income/expenses**, applies fixed **50/30/20** or **75/15/10** presets across **Needs / Wants / Savings & Debt**, optionally maps **Monzo pots** to categories without double-counting, and reports **category health**, **required income / limiting category**, **runway warnings**, and **export/deletion**. A **pure budgeting core library** holds all ratio and shortfall maths for golden unit tests; **statement parsing**, **recurrence detection**, **EF Core persistence**, and **BlazorBlueprint** UI sit at the edges.

## Technical Context

**Language/Version**: C# / .NET 10 (`global.json` pinned SDK)

**Primary Dependencies**:
- ASP.NET Core Blazor Server (InteractiveServer)
- BlazorBlueprint.Components v4 (+ matching Primitives, Lucide icons)
- .NET Aspire AppHost (Azure Container Apps, Azure SQL integrations)
- EF Core 10 + SQL Server provider
- CsvHelper (Monzo CSV — added at implement via central package management)
- ASP.NET Core Identity passkeys (SQL persistence)
- Microsoft.Extensions.Http.Resilience (typed HTTP clients if needed)

**Storage**: Azure SQL Database (hosted); SQL Server container locally (`RunAsContainer`). Migrations in repo; reviewed idempotent deploy scripts for production traffic changes. UserId on owned tables, query filters, explicit ownership checks. Data Protection key ring in SQL.

**Testing**: xUnit v3, NSubstitute, built-in Assert; golden files for Monzo parsing; independent expected results for budgeting core; Playwright C# for key journeys; AppHost not unit-tested.

**Target Platform**: Linux containers on Azure Container Apps (Consumption); local Aspire + Docker SQL for development.

**Project Type**: Multi-project web application + pure libraries + AppHost.

**Performance Goals**: Interactive Blazor acceptable for single-user/small hosted instances; streaming import within documented row/size limits; bounded DB retries compatible with SQL auto-pause on dev/test tiers.

**Constraints**:
- Budgeting core: no I/O (constitution III).
- GBP only; no raw statements in logs; no Blob retention for uploads v1.
- Local-only mode not network-exposed; hosted passkey-only.
- UK English UI/docs.

**Scale/Scope**: v1 single working budget period per user; one Monzo account; Must FR-M001–M037; deferred services per release gates (Redis, SignalR, Blob staging, Key Vault, etc.).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Plan compliance |
|-----------|-----------------|
| I Code Quality | DI, async + CancellationToken, nullable, records for DTOs, IHttpClientFactory pattern documented in CONVENTIONS |
| II Testing | Golden Monzo tests, pure core golden maths, Playwright journeys planned |
| III Maintainability | Parsing / detection / budgeting / reporting separation; core pure |
| IV Security & Privacy | Passkeys hosted, local mode, validation, encryption, minimal logging |
| V Documentation | plan, research, data-model, contracts, quickstart; implement adds XML + self-host docs |
| VI Transparency | Decisions traceable to spec and research.md |
| Budget model (constitution) | Three categories, two presets, optional pots, single Monzo CSV — aligned |
| Deployment governance | Self-host + local-only + hosted auth paths in Aspire staging model |

**Post-design re-check**: Phase 1 artifacts respect module boundaries and non-negotiable budget rules. **No unjustified gate violations.** Complexity Tracking table empty.

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

**Structure Decision**: Expand from current placeholder `src/Potsure/` into layered projects matching constitution module boundaries. Web references Infrastructure + Reporting; Infrastructure references EF + statement/recurrence adapters; Reporting and Recurrence reference Budgeting; Statements standalone library consumed by Infrastructure.

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

Completed in [research.md](./research.md): Monzo/CsvHelper, de-dupe, variable expense averaging with partial-month normalisation, one-off vs commitment rules, EF/Aspire/Identity/OTel, BlazorBlueprint v4, staged ACA/SQL policy, upload/export streaming.

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

- `AddAzureContainerAppEnvironment`, `AddAzureSqlServer` + `RunAsContainer` locally, `AddDatabase`, `WithReference`, `WaitFor`, `PublishAsAzureContainerApp`.
- Customise SQL SKU/offer, ACA CPU/memory/replicas/ingress per stage (dev/test 0–1 replica; public min 1; commercial 2–4 after approval).
- Managed identity to SQL; no password in image; reconcile Data Protection with SQL key ring.

### Upload pipeline

- Stream parse → validate account → stage → dedupe → atomic commit; clear temp on all exits; no durable raw Blob v1.

### Identity

- Passkeys + SQL; reauth before export/delete/credential changes; hosted no password fallback v1.

## Complexity Tracking

> No constitution violations requiring justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Next steps

`tasks.md` is generated; run `/speckit-implement` or the repository implement workflow. Optional: `/speckit-analyze` before implement.
