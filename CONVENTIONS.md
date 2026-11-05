> Coding and design conventions for this repository.

# Conventions

**Project:** Potsure  
**Last updated:** 2026-11-05

When in doubt, follow this file. Significant architecture changes need an ADR.

---

## Project Structure

Source layout follows **[ADR-0002: Feature-folder source layout](adr/adr-0002-feature-folder-source-layout.md)** (incremental adoption across projects).

```
src/
├── Potsure.Budgeting/        # Pure budget kernel (money, presets, shared domain enums)
├── Potsure.Statements/       # Monzo CSV parse, validate, classify, dedupe
├── Potsure.Recurrence/       # Recurring pattern detection (I/O-free)
├── Potsure.Reporting/        # Period summaries and report DTO assembly (uses Budgeting)
├── Potsure.Data/             # EF Core DbContexts, entities, migrations
├── Potsure.Infrastructure/   # DI, feature orchestration, persistence helpers - see profile below
├── Potsure.Web/              # Blazor host - see profile below
└── …
tests/
└── …
```

### Domain logic assemblies

The four `Potsure.{Budgeting,Statements,Recurrence,Reporting}` projects are **bounded contexts**: each assembly is the feature boundary (internal `Features/` subfolders only if a context grows large). They stay free of EF and host I/O.

### `Potsure.Infrastructure` (application library profile)

Orchestration aligned with Web feature names:

- **`Features/Import/`**  -  statement upload pipeline (wraps `Potsure.Statements`).
- **`Features/Plan/`**  -  budget period, presets, pots, funding, plan queries.
- **`Features/Recurrence/`**  -  income/expense patterns, review, recurring summary read model.
- **`Features/Reports/`**  -  report services, attribution, one-off categorisation, report queries (wraps `Potsure.Reporting` builders).
- **`Features/Export/`**  -  export snapshot and file formats.

Cross-cutting (not under `Features/`): **`Persistence/`**, **`Tenancy/`**, **`Deletion/`**, root **`DependencyInjection.cs`**.

Namespaces: `Potsure.Infrastructure.Features.{FeatureName}` for feature code.

### `Potsure.Web` (Blazor profile)

Product capability folders (orchestration lives in Infrastructure under the same names where 1:1):

- **`Features/Import/`**  -  statement import wizard UI.
- **`Features/Plan/`**  -  budget period, presets, pots, funding settings.
- **`Features/Recurrence/`**  -  pattern review and recurring summary UI.
- **`Features/Reports/`**  -  reports, attribution, one-off categorisation UI.
- **`Features/Privacy/`**  -  export and permanent deletion UI (`/privacy`). Maps to Infrastructure **`Features/Export/`** and **`Deletion/`** (not a separate Infrastructure `Privacy/` folder).

- **`Features/{Name}/Pages/`**  -  routable product UI (`@page`).
- **`Features/{Name}/Components/`**  -  non-route UI used only within that feature.
- **`Components/`**  -  app shell only: `App.razor`, `Routes.razor`, `Layout/`, `Shared/`, shell `Pages/` (home, error), Identity **`Account/`** (template-shaped).
- **`Configuration/`**, **`Services/`**, **`Security/`**, **`Tenancy/`**, **`Telemetry/`**, **`Resources/`**  -  cross-cutting web host code (not feature domains). Privacy-specific adapters (e.g. export reauthentication bridge) may live under **`Features/Privacy/`** when they change with that UI.

Namespaces for feature code: `Potsure.Web.Features.{Name}.Pages`, `.Components`, or feature root (e.g. `Potsure.Web.Features.Privacy` for co-located helpers).

Project-wide Razor `@using` directives: **`_Imports.razor`** at the Web project root (shell `Components/_Imports.razor` is a pointer only).

Add folders (`samples/`, `docs/`, AppHost, etc.) as the product grows. Update this section when layout changes.

---

## C# patterns

- **Async/await** for I/O; propagate `CancellationToken` on public async APIs.
- **Nullable reference types** enabled (`Directory.Build.props`).
- **Treat warnings as errors** in all projects.
- **XML documentation** on public API members.
- **HTTP outbound calls** (if any): use `IHttpClientFactory` and typed clients; record significant HTTP client choices in an ADR.

---

## Testing

- **xUnit v3** with Microsoft.Testing.Platform.
- **NSubstitute** when mocks are required.
- **Built-in `Assert` only**  -  no FluentAssertions, Shouldly, Moq, NUnit, or MSTest.
- Test classes: `[ClassName]Tests`. Test methods: `Method_State_Expected`.

---

## Documentation

- UK English in docs and comments.
- Do not use the em dash (Unicode U+2014) in user-facing text, code comments, or documentation in product scope (`src/`, `tests/`, `docs/`, `adr/`, `specs/`, and related root markdown). Use spaced ASCII hyphen ` - ` for prose breaks; use a single `-` for empty or missing UI values (for example table placeholders).
- User-facing docs in `docs/` when the README is not enough; see `.agents/skills/project-documentation/`.
