---
title: "ADR-0002: Feature-folder source layout"
status: "Accepted"
date: "2026-11-05"
authors: "Potsure maintainers"
tags: ["architecture", "structure", "maintainability"]
supersedes: ""
superseded_by: ""
---

# ADR-0002: Feature-folder source layout

## Status

**Accepted**

## Context

Potsure spans multiple projects (`Potsure.Web`, domain libraries, data, AppHost). Contributors struggled to locate UI and related code when folders mixed product capabilities with technical layers or placed routable pages in several unrelated trees. The solution must scale to **future restructuring of other projects**, not only the Blazor host, without forcing a single big-bang migration of every assembly.

## Decision

Adopt **feature-folder organisation** as the default way to group product capability code in `src/` projects:

- **Organise by product capability / user-facing feature** (vertical slices) - Import, Plan, Export, Recurrence, and similar named domains - rather than by technical layer alone when code belongs to that domain.
- **Co-locate** types that change together for a capability (UI pages, handlers, queries, feature-specific view models) under one feature folder where the project boundary allows it.
- **Keep cross-cutting code** outside feature folders with explicit names (`Persistence/`, `Tenancy/`, host `Configuration/`, Blazor app shell, migrations, AppHost resources) - not buried inside arbitrary feature trees.
- **Mirror folders in namespaces**: `{ProjectRootNamespace}.Features.{FeatureName}.{Segment}` for feature code (segments defined per profile below).
- **Adopt incrementally**: new work and focused refactors follow this ADR; existing projects migrate in separate PRs. **`Potsure.Web` is reference implementation #1.**

### Project profiles

| Project kind | Feature folder pattern | Notes |
|--------------|------------------------|--------|
| **Blazor host (`Potsure.Web`)** | `Features/{Name}/Pages/` (routable UI), `Features/{Name}/Components/` (feature-only partials); app shell in `Components/` (`App`, `Routes`, `Layout`, `Shared`, Identity `Account/`) | Identity stays template-shaped under `Components/Account/` |
| **Application libraries** (e.g. `Potsure.Infrastructure`) | Prefer `Features/{Name}/` for capability orchestration; retain `Persistence/` and similar infrastructure folders | Align feature names with Web where 1:1 (Import, Export, etc.) |
| **Bounded context projects** (e.g. `Potsure.Budgeting`, `Potsure.Reporting`) | The project is the feature boundary; internal `Features/` subfolders only if a context grows multiple sub-domains | No forced rename of small libraries |
| **Data (`Potsure.Data`)** | Entity, configuration, and migration-centric layout; optional `Features/` only if grouping entities by domain helps without fighting EF conventions | Migrations unchanged unless separately planned |
| **AppHost / ServiceDefaults** | No `Features/` tree | Orchestration and shared host defaults only |

### Web-specific implementation (reference)

- Web-layer security helpers live under `Potsure.Web/Security/` (not project root).
- App styling: Sunset theme CSS plus **`bb:`** layout utilities from BlazorBlueprint (no consumer Tailwind build in `Potsure.Web`).
- Public routes (`@page` URLs) are unchanged when moving files; this ADR governs layout only.
- Web **`Features/Privacy/`** (export and delete UI) corresponds to Infrastructure **`Features/Export/`** and cross-cutting **`Deletion/`** - do not add a duplicate `Features/Export/` tree on Web.
- Project-wide Razor imports live in **`Potsure.Web/_Imports.razor`** at the host project root.

## Consequences

### Positive

- **POS-001**: One predictable place to look for capability code within each project.
- **POS-002**: Aligns Web UI with domain language used in specs and navigation.
- **POS-003**: Single documented rule for agents and humans (`CONVENTIONS.md` references this ADR).

### Negative

- **NEG-001**: Large mechanical moves in PRs with no user-visible behaviour change.
- **NEG-002**: Temporary mismatch until all projects adopt their profile (Infrastructure may stay layer-mixed for a time).
- **NEG-003**: Blazor still requires a `Components/` shell folder for `App.razor`, layouts, and Identity template - two top-level concepts (`Components/` vs `Features/`).

## Alternatives Considered

### Layer-only folders (Services/, Controllers/, Views/)

- **ALT-001**: **Description**: Group all code by technical type across the project.
- **ALT-002**: **Rejection Reason**: Hard to navigate by product feature; scattered changes for one user journey.

### Single `Components/` tree for all Razor including product pages

- **ALT-001**: **Description**: Keep all UI under `Components/{Feature}/` without a root `Features/` folder.
- **ALT-002**: **Rejection Reason**: Blurs “app shell” with product pages; `Components/` reads as generic UI primitives, not domain features.

### Big-bang migration of every project in one PR

- **ALT-001**: **Description**: Restructure Web, Infrastructure, and Data simultaneously.
- **ALT-002**: **Rejection Reason**: Unreviewable diff; ADR allows incremental adoption instead.

## Implementation Notes

- **IMP-001**: Done  -  `src/Potsure.Web` → `Features/{Import,Plan,Reports,Recurrence,Privacy}/` plus `Security/`.
- **IMP-002**: Done  -  `src/Potsure.Infrastructure` → `Features/{Import,Plan,Recurrence,Reports,Export}/` with cross-cutting `Persistence/`, `Tenancy/`, `Deletion/`. Namespaces: `Potsure.Infrastructure.Features.{Name}`.
- **IMP-003**: Success = build and tests green; no route or public API breakage from moves alone.
- **IMP-004**: Domain bounded-context projects (`Potsure.Budgeting`, `Potsure.Statements`, `Potsure.Recurrence`, `Potsure.Reporting`) remain separate assemblies for v1; `Potsure.Budgeting` stays a zero-dependency pure kernel. Optional consolidation of thin libraries is a product decision, not required for layout adoption.
- **IMP-005**: Done  -  documentation and co-location closure on `001-potsure-v1`: `CONVENTIONS.md` Web feature list and Privacy mapping; `specs/001-potsure-v1/plan.md` / `tasks.md` paths aligned with `Potsure.Data`; [`src/Potsure.Infrastructure/README.md`](../src/Potsure.Infrastructure/README.md); `ExportReauthenticationGateAdapter` under `Features/Privacy/`; consolidated Razor `_Imports.razor` at Web project root. v1 layout adoption complete for Web and Infrastructure profiles; Data, bounded contexts, and AppHost unchanged per table above.

## References

- **REF-001**: [`CONVENTIONS.md`](../CONVENTIONS.md)  -  Project Structure
- **REF-002**: [`.agents/skills/blazor-blueprint/references/potsure-integration.md`](../.agents/skills/blazor-blueprint/references/potsure-integration.md)  -  Web paths
- **REF-003**: `specs/001-potsure-v1/plan.md`  -  Blazor / Identity shell expectations
