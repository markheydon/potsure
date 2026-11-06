# Agent Instructions

Repository-specific operating rules for AI coding agents.

## Core Context

Read before non-trivial changes:

- `GOALS.md`
- `SCOPE.md`
- `CONVENTIONS.md`
- `.specify/memory/constitution.md` - non-negotiable mandates; wins over other docs until amended
- Active feature under `specs/` (see `.specify/feature.json`, e.g. `specs/001-potsure-v1/`)
- `adr/` - accepted decisions

## Language

UK English in documentation, comments, and user-facing text. Do not use the em dash (U+2014); see `CONVENTIONS.md` (Documentation).

## Tech Stack

- .NET 10.0 (SDK pinned in `global.json`).
- xUnit v3 with Microsoft.Testing.Platform; NSubstitute when mocks are required; built-in asserts only.

## Aspire & dependencies

Constitution mandates (see `.specify/memory/constitution.md`, *Development Workflow & Quality
Gates*) apply to all agents. Operational expectations:

### Aspire AppHost resources

When the distributed app is modelled in a .NET Aspire AppHost, consume every integrated
resource (SQL Server, caches, messaging, and similar) through the dependency-injection
registrations and client types supplied by the matching Aspire integration package. Do **not**
construct connections from guessed, assumed, or hard-coded connection strings when AppHost
already models that resource. Wire AppHost project references and register clients as each
integration documents - for example,
[Add the SQL Server client](https://aspire.dev/integrations/databases/sql-server/sql-server-connect/#add-the-sql-server-client).

Use the `aspire`, `aspireify`, and related skills under `.agents/skills/` when wiring or
changing AppHost resources.

### Central package management

Do not add or remove NuGet packages unless the user has explicitly instructed that work (see
**Not Allowed Without Explicit Instruction** below). When implementation **does** introduce
packages, or when you are explicitly maintaining central versions:

- Add package versions only in `Directory.Packages.props` (central package management).
- Pin each newly introduced package at the **latest stable version available at the time it
  is added**.
- Before finishing dependency-related work, run:

```bash
dotnet package list --outdated
```

Upgrade centrally managed packages to the latest compatible versions where appropriate, then
run the full test suite (not only `--no-build`) after any version changes:

```bash
dotnet test Potsure.slnx -c Release
```

## Spec Kit

Workflow commands: `.cursor/commands/` (e.g. `do-implement.md`). Skills: `.cursor/skills/speckit-*`. Do not edit `.specify/` or Spec Kit skill sources unless the user explicitly asks.

Typical flow: `/speckit-constitution` → `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` → implement (optional analyze/converge steps).

## Skills

Project skills live in `.agents/skills/`.

| Skill | Use when |
|---|---|
| `create-architectural-decision-record` | Creating or major-updating an ADR |
| `documentation-writer` | Diátaxis-aligned documentation |
| `project-documentation` | Project-aware docs placement |
| `code-review` | PR or branch review before merge (see `.cursor/commands/loop-code-review.md`) |
| `principal-engineering` | Non-trivial implementation, refactors, analyzer/build fixes, EF migrations, new abstractions - keep changes architecturally sound and root-cause based |
| `blazor-blueprint` | BlazorBlueprint UI in `Potsure.Web` (`Bb*` components, theme, overlays, icons) |
| `aspire` (and related `aspire-*` skills) | AppHost exists or you are wiring/deploying Aspire resources |

## Task Routing

- **Code** (`src/`, `tests/`): `CONVENTIONS.md`.
- **Documentation** (`**/*.md` except `adr/`): documentation skills.
- **ADRs** (`adr/*.md`): ADR skill only.
- **Feature specs** (`specs/**`): Spec Kit skills and constitution compliance.

## Not Allowed Without Explicit Instruction

- Add or remove NuGet packages (except updating central versions in `Directory.Packages.props`
  when the user explicitly asks you to maintain dependencies - then follow **Central package
  management** above).
- Modify CI/CD behaviour.
- Commit secrets or API keys.

## Pull Request Workflow

Follow [plan/PULL_REQUEST_POLICY.md](plan/PULL_REQUEST_POLICY.md) and [`.github/PULL_REQUEST_TEMPLATE.md`](.github/PULL_REQUEST_TEMPLATE.md).

Before opening a PR:

```bash
bash scripts/verify-no-em-dash.sh
dotnet format Potsure.slnx --verify-no-changes
dotnet build Potsure.slnx -c Release -warnaserror
dotnet test Potsure.slnx -c Release --no-build
```

Title shape: `[Type] Imperative summary (#NNN)` with labels from [plan/LABEL_STRATEGY.md](plan/LABEL_STRATEGY.md).

All agent-authored pull requests require human review before merge.
