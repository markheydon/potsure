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
├── Potsure.Data/             # EF Core DbContexts, entities, migrations
├── Potsure.Infrastructure/   # Services, repositories, runtime seeding
├── Potsure.Web/                # Blazor host — see profile below
└── …
tests/
└── …
```

### `Potsure.Web` (Blazor profile)

- **`Features/{Name}/Pages/`** — routable product UI (`@page`).
- **`Features/{Name}/Components/`** — non-route UI used only within that feature.
- **`Components/`** — app shell only: `App.razor`, `Routes.razor`, `Layout/`, `Shared/`, shell `Pages/` (home, error), Identity **`Account/`** (template-shaped).
- **`Configuration/`**, **`Services/`**, **`Security/`**, **`Tenancy/`**, **`Telemetry/`**, **`Resources/`** — cross-cutting web host code (not feature domains).

Namespaces for feature code: `Potsure.Web.Features.{Name}.Pages` or `.Components`.

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
- **Built-in `Assert` only** — no FluentAssertions, Shouldly, Moq, NUnit, or MSTest.
- Test classes: `[ClassName]Tests`. Test methods: `Method_State_Expected`.

---

## Documentation

- UK English in docs and comments.
- User-facing docs in `docs/` when the README is not enough; see `.agents/skills/project-documentation/`.
