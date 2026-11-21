# Manual testing and dogfooding feedback

**Last updated:** 2026-11-09

## Read this first (humans and AI agents)

This file is **not** product direction, a backlog, or a specification.

| Use this for | Do **not** use this for |
|--------------|-------------------------|
| Remembering what came up during manual testing and dogfooding | Deciding what to build next |
| Capturing tensions with `specs/001-potsure-v1/spec.md` before Spec Kit clarifies them | Implementing features "because FEEDBACK says so" |
| Ideas that might become a **future** spec after human review | Overriding or reinterpretating `GOALS.md`, `SCOPE.md`, `specs/**/spec.md`, or `.specify/memory/constitution.md` |

**Source of truth for product and release behaviour:** active feature specs under `specs/`, `GOALS.md`, `SCOPE.md`, constitution, and ADRs. Spec Kit (`/speckit-clarify`, `/speckit-specify`, tasks in the active feature folder) is the only path from an idea here to agreed work.

Row IDs (`BL-001`, …) are **archive labels** for cross-referencing notes. They do not imply priority, approval, or v1.0.0 scope.

---

## Spec review themes (capture for future Spec Kit)

Themes surfaced by comparing this file and live testing to [specs/001-potsure-v1/spec.md](../specs/001-potsure-v1/spec.md) (Nov 2026). **Under active human review** (income and plan dates first). Nothing below is agreed direction until encoded in a spec.

| Theme | Spec touchpoints | Observation (feedback only) | Likely Spec Kit action |
|-------|------------------|-----------------------------|-------------------------|
| **Plan period and budgeted income** | US3, FR-M031, budgeting-core | Single inclusive date window + one **total** planned income for that window drives allocations and all period reports. Dogfooding suggests this may not match the intended household mental model (standing rent/income until changed vs "planning period"). | Clarify or amend plan model; may be a new feature spec rather than copy-only |
| **Income detection vs G2** | US2, FR-M007, GOALS G2 | Expense suggestions often appear; salary credits often do not (conservative ≥3-row + cadence windows). Spec allows incomplete detection but G2 emphasises discovery from statements. | Clarify detection bars; consider manual add, uncertain suggestions, income-specific rules |
| **Recurring lifecycle after confirm** | US2, US9, FR-M008, FR-M010a | Merge/split/reject exist; **edit** confirmed amount, cadence, or fixed/variable is not clearly specified and is largely absent in UI. Bills increasing cannot be updated without workarounds. | New or extended user stories + FRs for edit; confirm-step override for amount kind |
| **Pot modes and confirm defaults** | US4, FR-M013–014 | Three-pot mode does not enforce limits in UI; optional mapped pot on confirm is always manual. Ideas: default pot when one pot maps to category; enforce mode invariants. | Extend US4 ACs; product rules for defaults |
| **Onboarding order** | quickstart scenarios vs UX ideas | Quickstart: import → recurring → plan. Stepper drafts: import → plan → recurring. | Clarify first-run narrative if onboarding becomes release-critical |
| **One-off categorisation discoverability** | US6, FR-M037 | Route `/reports/categorise` exists; not in main nav (links from category health / attribution). | Navigation spec or acceptance criteria, not necessarily model change |

**Explicitly post-v1 / new product specs unless promoted:** split categorisation on card payments (BL-021), custom locale (BL-011), dense detection table (BL-019).

---

## Session notes (2026-11-09, unnumbered)

Observations from final-leg testing; not yet promoted to `BL-*` rows.

- **Plan settings dates:** Behaviour matches current spec; owner judgement is that the **model** may be wrong for original product intent (see theme table above), not only labels (BL-012).
- **Fixed vs variable on confirm:** Detector chooses kind; user wants explicit control (checkbox or dropdown) when wrong on thin history.
- **Pot default on recurring confirm:** In three-pot (or single-pot-per-category) mode, pre-select mapped pot; retain ability to choose None (e.g. spend from main balance).
- **Playwright / automated testing:** Journey smoke exists (`tests/Potsure.E2E/`); numeric truth belongs in budgeting/statement goldens and infrastructure tests. Future work: map quickstart scenarios to integration tests before expanding UI assertions.

---

## Multi-account and split categorisation (future ideas)

| ID | Theme | Observation | Possible follow-up (needs spec) |
|----|--------|-------------|--------------------------------|
| **BL-021** | Credit card payments across categories | Manual testing with **Barclaycard** (and similar): a single payment from the current account can blend **contractual minimum** (arguably Needs) with **discretionary overpayment** toward debt (Savings and debt). v1 assigns each transaction to **one** category (and optional one-off split is not in scope for arbitrary debits). | Model **split lines** or **allocation rules** per payment; do not assume minimum vs extra without statement or user rules. Reconcile with recurring patterns, commitments (FR-M018), and attribution. **Related:** recurring **Split expense pattern** uses implicit equal halving; any future work should allow user-defined portions. |

---

## Recurring pattern review

| ID | Theme | Observation | Possible follow-up (needs spec) |
|----|--------|-------------|--------------------------------|
| **BL-019** | Detection review layout | Card-per-suggestion works as a first pass; a **dense table** may be easier when there are many suggestions. | Optional table view alongside cards; align actions with confirm / reject / merge. |
| **BL-020** | Suggested patterns at £0.00 | Some suggestions showed **typical £0.00**; users unsure what **Confirm** would do. | **Fixed** Nov 2026 in implementation: variable copy, positive amount on confirm, variable patterns with estimate not £0 fixed. |
| **BL-023** | Income suggestions missing while expenses appear | User sees **outgoing** patterns but **no income**, despite salary or credits on file. | **Spec tension (documented):** US2 / FR-M007 vs conservative implementation (≥3 rows, spacing windows). Workaround in current spec: budgeted income on Plan (FR-M031). Ideas: manual add, weaker suggestions with "uncertain" labelling, irregular pay rules. **Active owner review:** income detection and plan dates. |

---

## Import, dedupe, and label display

| ID | Theme | Observation | Possible follow-up (needs spec) |
|----|--------|-------------|--------------------------------|
| **BL-022** | Lower-case vendor names in the UI | Suggestion titles looked lower case due to normalised grouping keys. | **Fixed** Nov 2026: display label from latest raw transaction name in cluster. |

---

## Onboarding and journey continuity (ideas only)

Ideas from testing (Nov 2025–2026): a **dashboard stepper** showing import, plan, and recurring progress. **Not agreed product direction.**

| Step (draft idea) | Rough completion signal | Typical destination |
|-------------------|-------------------------|---------------------|
| **1. Upload statement(s)** | At least one successful import with transactions | Import |
| **2. Set up your plan** | Budget period, preset, budgeted income configured | Plan settings |
| **3. Confirm recurring patterns** | User reviewed suggestions where applicable | Detection review |

Step order (2 vs 3) conflicts with quickstart scenario order; needs product sign-off before any spec.

| ID | Theme | Observation | Possible follow-up (needs spec) |
|----|--------|-------------|--------------------------------|
| **BL-001** | After import | Import summary lacks clear **next step**. | Link to a future home stepper or CTA (e.g. plan or recurring). |
| **BL-002** | Home dashboard | Home prompts to import when data already exists. | State-aware home when/if stepper is specified. |
| **BL-003** | Import page | Returning to Import does not show that data exists. | Optional last-import line; or rely on home stepper. |

---

## Plan settings and presentation

| ID | Theme | Observation | Possible follow-up (needs spec) |
|----|--------|-------------|--------------------------------|
| **BL-009** | Date display | Period dates showed US short-date form. | **Fixed** Nov 2025: `en-GB` via `RequestLocalization`. |
| **BL-010** | Enum labels in `BbSelect` | Raw enum in preset trigger. | **Fixed** Nov 2025: `FormatPreset` on Plan settings. |
| **BL-011** | User-chosen locale | UK default only. | Post-v1 settings idea if scoped. |
| **BL-012** | Plan settings terminology | Labels feel like jargon; date range purpose unclear. | Originally framed as copy-only vs US3/FR-M031. **Nov 2026:** owner may revisit **plan model**, not just copy (see spec review themes). Draft copy mappings below remain **suggestions**. |
| **BL-013** | Plan save feedback | No visible save confirmation. | **Fixed** in UI: success alert + "Saving…". |
| **BL-014** | `BbSelect` closed labels | GUIDs / enum names in triggers. | **Fixed** Nov 2026 on key pages; scan new Guid selects if reported. |
| **BL-015** | Pot mapping page copy | FR-M014 in UI; mode names unclear. | Partially improved; deeper US4 onboarding may need spec. |
| **BL-016** | Category order in pickers | Expect Needs → Wants → Savings and debt. | **Fixed** Nov 2025: `BudgetCategoryDisplay.StrategyOrder`. |
| **BL-017** | Pot mapping rules and maintenance | Three-pot mode allows unlimited pot creates; no edit/delete in UI. | Ideas: enforce US4 mode limits, edit/delete; backend already has update/delete. |

### BL-012: Plan settings copy (draft suggestions only)

Mappings from **current** spec text (for if copy is updated without changing the model):

| Current UI (approx.) | User confusion | What the **current** spec means | Draft copy idea (not approved) |
|----------------------|----------------|--------------------------------|--------------------------------|
| **Allocation preset** | Sounds technical | Fixed 50/30/20 or 75/15/10 on three categories | "Budget split model" + subtitle |
| **Period start / end** | What is the period for? | Single working window for period-scoped totals | "Budget window" + inclusive range explainer |
| **Planned income (budgeted)** | Feels monthly | Total for **whole** selected period | "Total planned income for this period" + display-only £/month helper |

If the plan model changes in Spec Kit, this table is likely obsolete.

---

## Import and Monzo account

| ID | Theme | Observation | Possible follow-up (needs spec) |
|----|--------|-------------|--------------------------------|
| **BL-004** | First-upload account confirm | Checkbox flow; copy was unclear. | Partially addressed in wizard; revisit if users still miss it. |
| **BL-005** | Duplicate rows on re-import | Many "already imported" rows; correct dedupe. | Partially addressed in summary narrative. |
| **BL-018** | Import column mapping UI | Full mapping table every time for trusted Monzo layout. | **Spec tension:** FR-M002 requires confirm. Idea: compact summary + collapsible mapping when auto-map complete. |

---

## Local development (Aspire)

| ID | Theme | Observation | Possible follow-up (needs spec) |
|----|--------|-------------|--------------------------------|
| **BL-006** | Ephemeral local SQL | Data lost between sessions without volume. | **Fixed** Nov 2025: persistent volume in AppHost; see quickstart. |

---

## Technical notes from testing

| ID | Theme | Observation | Possible follow-up (needs spec) |
|----|--------|-------------|--------------------------------|
| **BL-007** | Import + SQL retry strategy | Import failed until execution strategy wrapped transaction. | **Fixed** Nov 2025. |
| **BL-008** | Data Protection key ring | Antiforgery warnings after manual DB wipe with old cookies. | Tester documentation; expected after wipe. |

---

## How to add to this file

1. Add a row when manual testing surfaces friction, a bug, or an idea.
2. Prefer linking to spec sections (`US*`, `FR-M*`) when noting tension; state whether behaviour matches **current** spec.
3. Do **not** implement from this file without spec/tasks promotion and human agreement.
4. When an idea is **agreed** in Spec Kit, record the decision in the feature `spec.md` / ADR and optionally mark the row **Promoted to spec** with a link; do not leave ambiguous "direction" language here.

---

## Document history

| Date | Change |
|------|--------|
| 2026-11-09 | Renamed from `BACKLOG.md` to `FEEDBACK.md`; added non-authoritative disclaimer, spec review themes, session notes; reframed tables as ideas not direction. |
| 2026-11-06 | Former backlog header (post-v1 capture). |
