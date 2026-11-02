> Coding and design conventions for this repository.

# Conventions

**Project:** Acme.Project
**Last updated:** (date)

When in doubt, follow this file. Significant architecture changes need an ADR.

---

## Project Structure

```
src/
└── Acme.Project/

tests/
└── Acme.Project.Tests/
```

Add folders (`samples/`, `docs/`, Blazor projects, etc.) as the product grows. Update this section when layout changes.

---

## C# patterns

- **Async/await** for I/O; propagate `CancellationToken` on public async APIs.
- **Nullable reference types** enabled (`Directory.Build.props`).
- **Treat warnings as errors** in all projects.
- **XML documentation** on public API members.
- **HTTP outbound calls** (if any): use `IHttpClientFactory` and typed clients — see [dotnet-api-client-template](https://github.com/markheydon/dotnet-api-client-template) or ADR-0001 in that template.

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
