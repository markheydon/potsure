---
name: blazor-blueprint
description: >-
  Build and refactor BlazorBlueprint v4 UI in Potsure (Bb* components, primitives,
  Lucide icons, charts, overlays). Fetches official llms docs on demand instead of
  guessing APIs. Use when editing Razor under src/Potsure.Web, adding Bb components,
  theming, dialogs/toasts, or BlazorBlueprint package alignment.
---

# BlazorBlueprint (Potsure)

BlazorBlueprint ships **machine-readable docs** at [blazorblueprintui.com/llms.txt](https://blazorblueprintui.com/llms.txt). **Do not copy `llms-full.txt` into this repo** (~2.8 MB, changes with library releases). This skill holds **Potsure wiring** and a **fetch map**; load upstream text only for the components you touch.

## Workflow

1. Read [references/potsure-integration.md](references/potsure-integration.md) before changing global setup (`Program.cs`, `App.razor`, layouts, `_Imports.razor`, theme CSS).
2. For component or API work, follow [references/documentation-map.md](references/documentation-map.md) and fetch **one or a few** upstream files (WebFetch or `curl`), not the full bundle unless the task truly spans many unrelated components.
3. Match existing Potsure Razor: `@rendermode InteractiveServer` on interactive pages, `Bb*` styled components first, UK English user-facing copy, AGENTS.md package rules (no new NuGet packages unless the user explicitly asked).
4. After edits, prefer `dotnet build src/Potsure.Web/Potsure.Web.csproj -c Release` when UI compile breaks are likely.

## Quick upstream index

| Need | URL |
|------|-----|
| Entry / package list | `https://blazorblueprintui.com/llms.txt` |
| Component name → doc path | `https://blazorblueprintui.com/llms/index.txt` |
| Install, render modes, Tailwind `bb:` prefix | `https://blazorblueprintui.com/llms/setup.txt` |
| `@bind`, controlled state, conventions | `https://blazorblueprintui.com/llms/patterns.txt` |
| Toast, dialog, theme, portal services | `https://blazorblueprintui.com/llms/services.txt` |
| Styled component | `https://blazorblueprintui.com/llms/components/{slug}.txt` |
| Headless primitive | `https://blazorblueprintui.com/llms/primitives/{slug}.txt` |
| Ready-made compositions | `https://blazorblueprintui.com/llms/blueprints.txt` |

Slug is kebab-case from the index (e.g. `BbDataTable` → `data-table.txt`).

## Rules agents often miss

- **Two layers**: styled `BlazorBlueprint.Components` (`Bb*`) on `BlazorBlueprint.Primitives`; Potsure uses **Lucide** icons (`BlazorBlueprint.Icons.Lucide.Components`).
- **Styles**: library utilities use the **`bb:`** prefix in `blazorblueprint.css`; app shell styling uses project CSS/Tailwind separately (see `setup.txt`).
- **Overlays**: `BbPortalHost` must exist in layouts that host interactive UI using dialogs, sheets, menus, toasts (Potsure: `MainLayout.razor`, `AuthLayout.razor`).
- **Version alignment**: keep `BlazorBlueprint.Components` and `BlazorBlueprint.Primitives` on matching v4 lines (`Directory.Packages.props`).
- **Charts**: use BlazorBlueprint chart components per feature spec; do not add a separate chart library without explicit instruction.

## When a skill is worth it

Upstream docs are complete but **large and generic**. This skill is the thin layer: **where Potsure already integrated Blueprint**, **how to pull the right upstream file**, and **repo constraints**. Without it, agents either hallucinate `Bb*` APIs or burn context on `llms-full.txt`.

## Additional resources

- Potsure setup and file map: [references/potsure-integration.md](references/potsure-integration.md)
- Fetch strategy and task routing: [references/documentation-map.md](references/documentation-map.md)
- Feature decisions: `specs/001-potsure-v1/plan.md` (BlazorBlueprint integration section)
