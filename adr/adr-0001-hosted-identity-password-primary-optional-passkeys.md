---
title: "ADR-0001: Hosted identity — password primary with optional passkeys"
status: "Accepted"
date: "2026-11-04"
authors: "Potsure maintainers"
tags: ["architecture", "security", "identity"]
supersedes: ""
superseded_by: ""
---

# ADR-0001: Hosted identity — password primary with optional passkeys

## Status

**Accepted**

## Context

Potsure v1 supports **local-only** (single implicit owner) and **hosted** (multi-user, SQL-backed) deployment modes. An earlier implementation treated hosted sign-in as passkey-only, with synthetic passwords and custom WebAuthn endpoints. That conflicted with constitution 1.2.0, which requires standard ASP.NET Core Identity with hashed credentials and does not mandate passkeys for v1 acceptance.

Export and permanent deletion require step-up reauthentication (FR-M022, FR-M023). Passkey step-up added complexity and duplicated the Blazor Identity template flows.

## Decision

- Register hosted auth via **`AddHostedIdentity`**: ASP.NET Core Identity with **email/password** as the primary path, cookie authentication, and SQL persistence on `PotsureDbContext`.
- Adopt the **.NET 10 Blazor Identity account template** under `/Account/*` (login, register, manage) including **optional** passkey create/sign-in via template endpoints — not required for acceptance.
- Enforce hosted financial pages with a **fallback authorization policy** requiring an authenticated user; remove bespoke passkey-only middleware and custom passkey-only registration endpoints.
- Use **current-password reauthentication** on hosted for export/delete (via `PasswordReauthenticationService` / `IExportReauthenticationGate`); local-only keeps the existing lightweight challenge.

## Consequences

### Positive

- **POS-001**: Aligns with constitution 1.2.0 and standard Identity operational practices (lockout, tokens, password reset hooks).
- **POS-002**: Reuses maintained template UI and endpoints, reducing custom WebAuthn surface area.
- **POS-003**: Clear acceptance criteria: register/login with password; passkeys remain optional.

### Negative

- **NEG-001**: Passkey-first users must create a password account or add a passkey after registration.
- **NEG-002**: Template pages require ongoing merges when upgrading Identity/Blazor templates.
- **NEG-003**: Password policy must be monitored for usability vs security trade-offs.

## Alternatives Considered

### Passkey-only hosted sign-in (prior approach)

- **ALT-001**: **Description**: Random high-entropy password plus mandatory WebAuthn for all hosted users.
- **ALT-002**: **Rejection Reason**: Violates constitution 1.2.0; poor recovery UX; non-standard Identity usage.

### Password-only with no passkey support

- **ALT-001**: **Description**: Disable passkey handler and template passkey UI entirely.
- **ALT-002**: **Rejection Reason**: Template already ships optional passkeys at low cost; deferring removal keeps a migration path for users who prefer WebAuthn.

## References

- Constitution 1.2.0 — hosted Identity and reauthentication
- `specs/001-potsure-v1/plan.md` — Phase 23 (T143–T148)
- `src/Potsure.Web/Configuration/HostedIdentityConfiguration.cs`
