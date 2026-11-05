# Potsure.Infrastructure

Application orchestration, DI composition, and persistence helpers. Folder layout follows [ADR-0002](../../adr/adr-0002-feature-folder-source-layout.md).

| You are adding… | Location |
|-----------------|----------|
| Feature orchestration (import, plan, reports, …) | `Features/{FeatureName}/` |
| EF repositories and seeding | `Persistence/` |
| Owner identity and ownership validation | `Tenancy/` |
| Permanent user financial data deletion | `Deletion/` |
| Service registration | `DependencyInjection.cs` (project root) |

Feature folders (align with Web where 1:1):

- **`Features/Import/`** — statement upload pipeline (wraps `Potsure.Statements`).
- **`Features/Plan/`** — budget period, presets, pots, funding, plan queries.
- **`Features/Recurrence/`** — income/expense patterns, review, recurring summary read model.
- **`Features/Reports/`** — report services, attribution, one-off categorisation, report queries (wraps `Potsure.Reporting` builders).
- **`Features/Export/`** — export snapshot and file formats.

Web **`Features/Privacy/`** (export and delete UI) maps to **`Features/Export/`** plus **`Deletion/`** here—not a duplicate `Features/Privacy/` folder.

Namespaces: `Potsure.Infrastructure.Features.{FeatureName}` for feature code.

EF entities, configurations, and migrations live in **`Potsure.Data`**, not in this project.
