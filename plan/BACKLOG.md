# Product backlog (post-v1.0.0)

**Purpose:** Capture UX and quality improvements discovered during manual testing and dogfooding. Items here are **not** in scope for the current v1.0.0 release gate unless explicitly promoted into `specs/001-potsure-v1/tasks.md`.

**Last updated:** 2026-11-05

---

## Onboarding and journey continuity

| ID | Theme | Observation | Possible direction |
|----|--------|-------------|-------------------|
| **BL-001** | After import | Successful import ends on a summary with stats and a tip about three months of history, but no clear **next step** (for example review recurring patterns, open category health, or set budget period). Users ask "now what?" | Add primary/secondary actions on import complete aligned with quickstart Scenario 2 onward; optional checklist on home for first-time hosted users. |
| **BL-002** | Home dashboard | Home still prompts to import a statement even when the user already has transactions in the system. | Make home **state-aware**: empty state vs "you have data" with links to the next recommended journeys (detection review, plan settings, reports). |
| **BL-003** | Import page | Returning to Import does not indicate that data already exists (may be fine for a upload-focused page). | Decide explicitly: keep Import upload-only, or add lightweight context (last import date, transaction count, link to reports) without turning Import into a dashboard. |

## Import and Monzo account

| ID | Theme | Observation | Possible direction |
|----|--------|-------------|-------------------|
| **BL-004** | First-upload account confirm | Monzo CSVs lack an account identifier; first import requires a one-time checkbox (no separate settings page). Copy was unclear before Nov 2025 testing. | **Partially addressed** in import wizard messaging; revisit if users still miss the flow. |
| **BL-005** | Duplicate rows on re-import | Re-uploading the same file after a partial or retried import shows many "already imported" rows. Behaviour is correct but was confusing before summary copy improved. | **Partially addressed** in import summary labels and narrative; consider a single-line link to "what dedupe means" in docs or UI. |

## Local development (Aspire)

| ID | Theme | Observation | Possible direction |
|----|--------|-------------|-------------------|
| **BL-006** | Ephemeral local SQL | Stopping Aspire (or waiting between sessions) resets the SQL container; users re-register and re-import. Expected for default `RunAsContainer` without a data volume. | Optional AppHost data volume for dev persistence; document trade-offs in quickstart (do not block v1.0.0). |

## Technical debt surfaced in testing

| ID | Theme | Observation | Possible direction |
|----|--------|-------------|-------------------|
| **BL-007** | User-initiated transactions + retry | Statement import failed under Aspire SQL until import commit ran inside `CreateExecutionStrategy().ExecuteAsync`. | **Fixed** Nov 2025; audit `UserDataDeletionService` and other manual transactions for the same pattern. |
| **BL-008** | Data Protection key ring | Antiforgery errors in logs when SQL was reset while browser cookies referenced old key IDs. | Expected after DB wipe; document for testers; avoid alarming users if similar symptoms appear in hosted ops. |

---

## How to use this file

1. Add rows when manual testing surfaces friction that is **out of scope** for the current release push.
2. Promote items by creating or updating tasks in the active feature spec, not by implementing directly from this list without agreement.
3. Mark **Partially addressed** or **Fixed** in the table when shipped; keep the row for history until the theme is fully closed.
