<!--
Sync Impact Report
- Version: (template placeholders) → 1.0.0
- Modified principles: all placeholders replaced with Potsure principles I–VI
- Added sections: Domain & Product Constraints; Development Workflow & Quality Gates
- Removed sections: none (initial establishment)
- Deferred: none
-->

# Potsure Constitution

## Core Principles

### I. Code Quality

All production code MUST follow a consistent C# style: small, focused units; no dead
code; dependency injection for composition; async/await for I/O with
`CancellationToken` on public async APIs; nullable reference types enabled; prefer
records for immutable data; outbound HTTP via `IHttpClientFactory` and typed clients.
Analyser and formatter rules MUST be enforced in CI (`dotnet format --verify-no-changes`,
build with warnings as errors). **Rationale:** predictable, reviewable code reduces defects
and keeps contributions aligned without ad hoc style debates.

### II. Testing Discipline

Functional changes MUST land with tests appropriate to the area changed. The budgeting
core MUST have unit tests with explicit, independently verified expected results (not
only snapshot-of-implementation checks). Statement parsing MUST use golden-file tests.
A small set of Playwright (C#) end-to-end tests MUST cover key user journeys. Tests
MUST use xUnit v3, NSubstitute when mocks are required, and built-in `Assert` only.
Aspire AppHost resource modelling MUST NOT be unit-tested. **Rationale:** Potsure’s
correctness depends on arithmetic and parsing; layered tests catch regressions where
bugs hurt users most.

### III. Maintainability

The codebase MUST keep clear module boundaries: parsing, detection, budgeting core, and
reporting. The budgeting core MUST remain pure and free of I/O (no file, network, or
database access inside core logic). Prefer small, documented interfaces at boundaries so
contributors can replace adapters without rewriting rules. **Rationale:** a pure core
makes the budget model testable and keeps product rules from leaking into infrastructure.

### IV. Security & Privacy by Default (NON-NEGOTIABLE)

Potsure handles highly sensitive personal financial data; a public hosted instance is in
scope. Security MUST outrank convenience in every design and implementation choice. Every
user input—including each uploaded statement and every form field—MUST be validated and
treated as untrusted; inputs MUST be validated and sanitised before use. Financial data
MUST be encrypted in transit and at rest on hosted deployments. All network-exposed hosted
instances (single-user or multi-tenant) MUST require authentication with passkey support.
V1 MUST preserve a local-only, account-free mode that MUST NOT be exposed over a network.
Raw statements, full account numbers, and secrets MUST NOT appear in logs. Persist the
minimum data required for function; deletion MUST remove user financial data completely.
**Rationale:** trust is the product; weak input handling or logging destroys it irreversibly.

### V. Documentation as a Deliverable

Work is not complete until documentation is complete. Public APIs, public types, and
non-obvious logic MUST carry XML (or equivalent) documentation in code. Architecture,
contributor workflow, and secure self-host setup MUST be documented for contributors and
self-hosters. **Rationale:** OSS sustainability and safe self-hosting depend on docs as
much as on features.

### VI. Transparency & Openness

The project MUST remain public open source with readable history. Significant decisions
MUST be recorded (e.g. ADRs). Contributions MUST uphold this constitution; reviewers
MUST block merges that violate non-negotiable mandates. **Rationale:** open process and
documented decisions let users and contributors audit behaviour and intent.

## Domain & Product Constraints

### Budget model (NON-NEGOTIABLE)

- Exactly three fixed budget categories: **Needs**, **Wants**, **Savings & Debt**.
- Only two allocation presets are permitted: **50/30/20** (default) and **75/15/10**
  (alternative). No custom category ratios in v1.
- Bank **pots** are optional, have no hierarchy, and each pot used in the budget maps to
  exactly one category.
- Combined totals MUST add mapped pot amounts to direct category amounts without
  double-counting; pots MUST NOT introduce custom ratios.
- V1 supports **one Monzo bank account per user**, accepting **Monzo-format CSV**
  statement files only. Multi-account and multi-provider consolidation are out of v1
  scope.

### Deployment governance

- Secure **self-hosting** MUST remain a supported path.
- **Account-free local-only** v1 use MUST be preserved and MUST NOT be replaced by
  mandatory cloud accounts for local workflows.
- Hosted deployments MUST comply with Principle IV (authentication, passkeys, encryption,
  no local-only mode on the network).

## Development Workflow & Quality Gates

- Before merge, changes MUST pass repository CI: format verification, Release build with
  warnings as errors, and full test suite.
- Pull requests MUST follow `plan/PULL_REQUEST_POLICY.md` and `.github/PULL_REQUEST_TEMPLATE.md`;
  agent-authored PRs require human review before merge.
- Runtime coding detail lives in `CONVENTIONS.md`, product boundaries in `SCOPE.md` and
  `GOALS.md`, and agent workflow in `AGENTS.md`. Where those documents conflict with this
  constitution, this constitution wins until amended.
- Scope or rule changes that affect non-negotiable mandates MUST update this constitution
  or an ADR with explicit rationale.

## Governance

- This constitution is the highest-order project governance for Spec Kit phases
  (`specify`, `plan`, `tasks`, `implement`) and for day-to-day development.
- **Amendments:** propose changes via PR updating `.specify/memory/constitution.md`,
  including a Sync Impact Report (HTML comment) until the amendment is committed; bump
  `CONSTITUTION_VERSION` per semantic versioning:
  - **MAJOR:** removal or incompatible redefinition of principles or non-negotiable mandates.
  - **MINOR:** new principles/sections or material expansion of guidance.
  - **PATCH:** clarifications and non-semantic wording fixes.
- **Compliance review:** PR authors MUST attest constitution compliance; reviewers MUST
  verify mandates for budget model, security, testing, documentation, and deployment paths
  when relevant files change.
- **Version line** at file footer is authoritative for constitution semver.

**Version**: 1.0.0 | **Ratified**: 2026-11-02 | **Last Amended**: 2026-11-02
