# Potsure

Web application for running a **pots-based budget** from bank statements with minimal manual upkeep. Potsure discovers recurring income and payments from uploads, lets you confirm them instead of retyping, and shows **planned vs. actual** spending by category—the same check a spreadsheet gives, without weekly reconciliation drudgery.

Product direction and success criteria: [GOALS.md](GOALS.md). Boundaries: [SCOPE.md](SCOPE.md).

## Quick start

```bash
dotnet build Potsure.slnx -c Release -warnaserror
dotnet test Potsure.slnx -c Release --no-build
```

## Repository layout

| Path | Purpose |
|------|---------|
| `GOALS.md`, `SCOPE.md`, `CONVENTIONS.md` | Product and coding context for humans and agents |
| `AGENTS.md` | Cursor agent operating rules |
| `.specify/memory/constitution.md` | Project constitution (Spec Kit governance) |
| `specs/` | Feature specifications (`001-potsure-v1` is active v1 spec) |
| `.cursor/rules/` | File-scoped agent constraints |
| `.agents/skills/` | Project skills (ADR, docs, code review, BlazorBlueprint, Aspire) |
| `plan/` | Release and PR policy |
| `adr/` | Architectural decision records |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Security concerns: [SECURITY.md](SECURITY.md).

## Licence

MIT — see [LICENSE](LICENSE).
