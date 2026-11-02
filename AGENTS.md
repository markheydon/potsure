# Agent Instructions

Repository-specific operating rules for AI coding agents.

## Core Context

Read before non-trivial changes:

- `GOALS.md`
- `SCOPE.md`
- `CONVENTIONS.md`
- `.specify/memory/constitution.md` — non-negotiable mandates; wins over other docs until amended
- Active feature under `specs/` (see `.specify/feature.json`, e.g. `specs/001-potsure-v1/`)
- `adr/` — accepted decisions

## Language

UK English in documentation, comments, and user-facing text.

## Tech Stack

- .NET 10.0 (SDK pinned in `global.json`).
- xUnit v3 with Microsoft.Testing.Platform; NSubstitute when mocks are required; built-in asserts only.

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
| `aspire` (and related `aspire-*` skills) | AppHost exists or you are wiring/deploying Aspire resources |

## Task Routing

- **Code** (`src/`, `tests/`): `CONVENTIONS.md`.
- **Documentation** (`**/*.md` except `adr/`): documentation skills.
- **ADRs** (`adr/*.md`): ADR skill only.
- **Feature specs** (`specs/**`): Spec Kit skills and constitution compliance.

## Not Allowed Without Explicit Instruction

- Add or remove NuGet packages (except central versions in `Directory.Packages.props` when maintaining deps).
- Modify CI/CD behaviour.
- Commit secrets or API keys.

## Pull Request Workflow

Follow [plan/PULL_REQUEST_POLICY.md](plan/PULL_REQUEST_POLICY.md) and [`.github/PULL_REQUEST_TEMPLATE.md`](.github/PULL_REQUEST_TEMPLATE.md).

Before opening a PR:

```bash
dotnet format Potsure.slnx --verify-no-changes
dotnet build Potsure.slnx -c Release -warnaserror
dotnet test Potsure.slnx -c Release --no-build
```

Title shape: `[Type] Imperative summary (#NNN)` with labels from [plan/LABEL_STRATEGY.md](plan/LABEL_STRATEGY.md).

All agent-authored pull requests require human review before merge.
