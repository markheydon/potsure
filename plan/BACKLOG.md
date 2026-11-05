# Product backlog (post-v1.0.0)

**Purpose:** Capture UX and quality improvements discovered during manual testing and dogfooding. Items here are **not** in scope for the current v1.0.0 release gate unless explicitly promoted into `specs/001-potsure-v1/tasks.md`.

**Last updated:** 2026-11-05 (dashboard stepper direction; plan settings UX notes)

---

## Onboarding and journey continuity

**Preferred direction (2026-11-05):** Replace generic home empty states with a **dashboard stepper** (BlazorBlueprint stepper or equivalent): three high-level steps, each showing **not started**, **in progress**, or **complete** from system state. Example after first successful import: Step 1 shows complete; Step 2 is highlighted as **current** ("Set up your plan"). Users should see at a glance what is done and what to do next without re-reading Import or a static "upload a statement" prompt.

| Step (draft) | Rough completion signal | Typical destination |
|--------------|-------------------------|---------------------|
| **1. Upload statement(s)** | At least one successful import with transactions in the system | Import |
| **2. Set up your plan** | Budget period, preset, and budgeted income configured (align with quickstart Scenario 3) | Plan settings / related plan pages |
| **3. Confirm recurring patterns** | User has reviewed and confirmed suggested recurring income/expenses where applicable (align with quickstart Scenario 2) | Detection review |

Exact step labels, order (2 vs 3 relative to spec scenarios), and "complete" rules need product sign-off before implementation. Import complete summary may link back to the dashboard stepper rather than duplicating the full checklist.

| ID | Theme | Observation | Possible direction |
|----|--------|-------------|-------------------|
| **BL-001** | After import | Successful import ends on a summary with stats and a tip about three months of history, but no clear **next step**. Users ask "now what?" | After import complete, point to **home dashboard stepper** (current step). Optional short CTA matching the highlighted step (for example "Set up your plan"). |
| **BL-002** | Home dashboard | Home still prompts to import a statement even when the user already has transactions in the system. | Implement **dashboard stepper** (see preferred direction above): state-aware progress for the three main steps; retire or demote one-size-fits-all import-only empty state when Step 1 is complete. |
| **BL-003** | Import page | Returning to Import does not indicate that data already exists (may be fine for a upload-focused page). | Keep Import upload-focused; rely on dashboard stepper for "you already have data" context. Optional lightweight line (last import date) only if testing shows confusion. |

## Plan settings and presentation

| ID | Theme | Observation | Possible direction |
|----|--------|-------------|-------------------|
| **BL-009** | Date display (Plan settings and elsewhere) | Period dates showed US short-date form because server culture was not UK despite UK English UI ([plan.md](../specs/001-potsure-v1/plan.md)). | **Fixed** Nov 2025: default `en-GB` via `RequestLocalization` in `Program.cs` (v1 single-locale product). |
| **BL-011** | User-chosen locale | v1 defaults to UK formatting for all users; some may want another locale later. | Post-v1: profile or settings page to override display culture (persist per user), while keeping storage/API on invariant/`DateOnly` and Monzo `dd/MM/yyyy` import parsing unchanged. |
| **BL-010** | Enum labels in `BbSelect` | Allocation preset trigger showed raw enum name (`FiftyThirtyTwenty`) until the dropdown opened. | **Fixed** Nov 2025 on Plan settings (explicit `FormatPreset` in `BbSelectValue`). Scan other enum selects if reported. |

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
