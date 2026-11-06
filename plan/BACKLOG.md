# Product backlog (post-v1.0.0)

**Purpose:** Capture UX and quality improvements discovered during manual testing and dogfooding. Items here are **not** in scope for the current v1.0.0 release gate unless explicitly promoted into `specs/001-potsure-v1/tasks.md`.

**Last updated:** 2026-11-06 (spec tension: recurrence vs budget income)

---

## Multi-account and split categorisation (future)

| ID | Theme | Observation | Possible direction |
|----|--------|-------------|-------------------|
| **BL-021** | Credit card payments across categories | Manual testing with **Barclaycard** (and similar): a single payment from the current account can blend **contractual minimum** (arguably Needs) with **discretionary overpayment** toward debt (Savings and debt). v1 assigns each transaction to **one** category (and optional one-off split is not in scope for arbitrary debits). | **Post-v1 product design**, not a quick tweak: model **split lines** or **allocation rules** per payment (e.g. minimum vs extra; fixed vs variable portions); **do not assume** how much is minimum versus extra (ratios change month to month and by product unless we integrate statement data or explicit user rules). Reconcile with recurring patterns, commitments (FR-M018), and attribution; consider Monzo-book mental model without forcing users to manually split every payment. **Related caveat:** Recurring review **Split expense pattern** today uses an implicit **equal halving** of the source typical fixed amount  -  also an assumption; any future work here should allow user-defined or rule-based portions, not only 50/50. Needs spec before implementation. |

## Recurring pattern review

| ID | Theme | Observation | Possible direction |
|----|--------|-------------|-------------------|
| **BL-019** | Detection review layout | Manual testing (Nov 2026): card-per-suggestion works as a first pass but a **dense table** (label, type, cadence, amount, actions) may be easier to scan when there are many suggestions. | **Add alongside** current layout, not a replacement: optional **table view** (toggle or secondary tab) for power users; keep cards as default if testing shows they help first-time reviewers. Align row actions with confirm / reject / merge flows. |
| **BL-020** | Suggested patterns at £0.00 | Some suggestions show **typical £0.00**; users are unsure what **Confirm** would do and expected fallback amounts when history is short (e.g. last payment or average). | **Current behaviour (v1):** `RecurrenceDetector` infers **Fixed** vs **Variable** from amount spread; **typical amount** is only set for **Fixed** (median of absolute debit/credit amounts **with magnitude &gt; 0**). If amounts vary beyond tolerance, behaviour is **Variable** and `TypicalAmount` stays **null**, which the review layer shows as **£0.00** (`pattern.TypicalAmount ?? 0m` in `RecurrenceReviewService`). **Confirm** then persists **£0** as a **fixed** recurring line for expenses (detection never auto-selects `ExpenseCadence.Variable`; variable estimation runs only when cadence is **Variable**, which the UI does not set from suggestions today). **Direction:** surface **Variable / amount unknown** in the UI instead of £0; for variable patterns, show **estimated monthly** (reuse `VariableExpenseEstimator`) or **last observed** as a hint; block or warn on confirm with zero fixed amount; consider aligning suggestion copy with FR-M034 / US8 (three-month average when available, shorter history with explicit coverage) for the **suggestion** row, not only post-confirm. |
| **BL-023** | Income suggestions missing while expenses appear | Manual testing (Nov 2026): user sees **outgoing** patterns but **no income**, despite having salary or other credits on file. Feels wrong vs expectation of "new job / new loan" without waiting months. | **Spec tension:** [US2 AC1](specs/001-potsure-v1/spec.md) asks for suggestions with **at least one month** of history **where inferable**; [FR-M007](specs/001-potsure-v1/spec.md) says weekly/fortnightly/monthly **where evidence exists**. **Implementation** is conservative: **≥3** eligible rows per normalised name plus **stable spacing** (weekly ~6–8 days, fortnightly ~13–16, monthly ~27–35) before any suggestion; mixed kinds for the same name are skipped; `InternalTransfer` / pot movements excluded. **Budget outcome today:** users do **not** need detection to refresh the plan ** - ** [US3 / FR-M031](specs/001-potsure-v1/spec.md): enter **budgeted income** (and period) on **Plan settings** immediately; category allocations follow from that. **Confirmed income patterns** are mainly for the **recurring summary** and optional suggestion of budgeted income; they do not block editing the plan. **FR-M018 commitments** use **confirmed recurring expenses** only, not income. **Direction:** keep clarifying on the review page; consider **manual add** for income/expense patterns without detection, or **weaker suggestions** (1–2 credits with explicit "uncertain" labelling), or cadence rules that tolerate irregular pay dates; promote with spec amendment if behaviour changes. |

## Import, dedupe, and label display

| ID | Theme | Observation | Possible direction |
|----|--------|-------------|-------------------|
| **BL-022** | Lower-case vendor names in the UI | Users notice merchant labels shown in **lower case**. **Import** stores `Transaction.Name` (and description) **as in the CSV** (`StatementImportService`). **Deduping** uses a **normalised** form internally (`DescriptionNormalizer`), not as the stored display name. **Recurrence** groups and labels use **`PatternLabelNormalizer`** (letters and digits → lower case, collapse spaces) for matching; the **suggestion title** reuses that normalised key, so the review page reads lowercase even when the statement used mixed case. | If testing shows friction: keep normalised keys for matching but surface a **display label** (e.g. most recent raw `Name` from the cluster) on recurrence and anywhere else keyed by normalised label; optional title-case helper **only for display**. |

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
| **BL-012** | Plan settings terminology | Manual testing (Nov 2025): labels feel like internal jargon, not plain English. Users unsure what to enter or which date range to pick. | **Aligns with spec (US3, FR-M031)**  -  behaviour is correct; **copy and helper text** need work, not a new product model. See breakdown below. |
| **BL-014** | `BbSelect` closed labels | Enum names (`CategoryNamedPots`), GUIDs, or `None` show in the trigger until the menu opens; `BbSelectItem.Text` alone is not enough (BlazorBlueprint). Same class of bug as **BL-010** on Plan preset. | **Fixed on Pot mapping** (mode + category) and Plan preset. **Audit remaining** Guid/category selects on Detection review, attribution, one-off categorise, privacy export format, etc. Pattern documented in `blazor-blueprint` `potsure-integration.md`. |
| **BL-015** | Pot mapping page copy | FR-M014 in UI, confusing "category-only" messaging, mode names unclear. | **Partially fixed** Nov 2025 (`PotMappingModeDisplay`, section copy). **BL-014** fixes dropdown display; deeper onboarding tie to US4 / Monzo pot workflows still open. |
| **BL-016** | Category order in pickers | Users expect **Needs → Wants → Savings and debt** everywhere. | **Fixed** Nov 2025: `BudgetCategoryDisplay.StrategyOrder` for pot mapping category list; other loaders already used enum order. Reports use budgeting `TieBreakOrder` in core calculators. Centralise any remaining ad-hoc lists on `BudgetCategoryDisplay`. |
| **BL-017** | Pot mapping rules and maintenance | Manual testing (Nov 2025): In **three pots named after categories** mode, users can still add unlimited pots; no UI to **edit** or **delete** a pot after a typo. | **Spec (US4)**: either three category-named pots **or** multiple expense-specific pots (each 1:1 with a category), or category-only ([spec.md](../specs/001-potsure-v1/spec.md) US4). **Backend** already has `BankPotService.UpdateAsync` / `DeleteAsync` (T061); **UI** only exposes create. **Direction:** enforce mode-specific limits in create (e.g. category-named: at most one pot per category, max three; block duplicate category mapping; optional suggested names Needs/Wants/Savings); add edit/delete (or rename + reassign category) on pot list; confirm delete when recorded funding &gt; 0 or transactions reference pot. Promote with US4 independent test scenarios. |
| **BL-013** | Plan save feedback | Save persisted but gave no visible confirmation (logs showed successful `UPDATE` on period/profile/categories). | **Fixed locally** Nov 2025 (success alert + "Saving…" on button); not yet on `main`/PR branch unless committed. |

### BL-012: Plan settings copy (spec alignment notes)

Product intent from [spec.md](../specs/001-potsure-v1/spec.md) **US3** and **FR-M031** (for when rewriting labels):

| Current UI (approx.) | User confusion | What the product actually means (spec) | Copy direction (draft) |
|----------------------|----------------|----------------------------------------|-------------------------|
| **Allocation preset** | Sounds technical; not obviously a budgeting rule | One of two **fixed category splits** (50/30/20 or 75/15/10) applied to **Needs / Wants / Savings & Debt** only ([budgeting-core.md](../specs/001-potsure-v1/contracts/budgeting-core.md)). | e.g. **Budget split model** or **Needs / Wants / Savings split**, with subtitle naming 50/30/20 or 75/15/10. |
| **Period start / Period end (inclusive)** | Calendar year? Jan–Dec? What is the period *for*? | **Single working budget window** for reports and period-scoped totals  -  **any** inclusive date range the user chooses, **not** tied to payday or tax year ([FR-M031](spec.md)). Default seed is often **current calendar month**, not forced to 1 Jan–31 Dec. Transactions on both boundary dates count. | e.g. **Budget window** or **Planning period**; short explainer: "Reports and category totals use transactions between these dates (both days included). Pick the range you want to plan and review  -  for example this month, last three months, or a custom range." |
| **Planned income (budgeted)** | Feels like **monthly** take-home | **Total income you expect across the whole selected period** (one number for that window). Category allocations = preset % × **this total** ([US3 AC3](spec.md)). **Actual receipts** from statements stay separate on reports ([FR-M019](spec.md), `ReportCopy`). | e.g. **Total planned income for this period** with explicit "not monthly unless your period is one month"; optional helper showing period length or £/month equivalent as **display only**. |

**Not against spec:** Clarifying that income is **per period total** and period is **user-chosen window** matches FR-M031. **Against spec would be:** implying monthly-only input, auto-applying suggested income without confirm, or multiple saved periods.

**Promotion path:** UX/copy task under US3 or post-v1 polish; may touch `PlanSettings.razor`, `ReportCopy.cs`, and dashboard stepper Step 2 wording (**BL-001** / **BL-002**). Consider a short in-page "How this plan works" collapsible rather than tooltip-only.

## Import and Monzo account

| ID | Theme | Observation | Possible direction |
|----|--------|-------------|-------------------|
| **BL-004** | First-upload account confirm | Monzo CSVs lack an account identifier; first import requires a one-time checkbox (no separate settings page). Copy was unclear before Nov 2025 testing. | **Partially addressed** in import wizard messaging; revisit if users still miss the flow. |
| **BL-005** | Duplicate rows on re-import | Re-uploading the same file after a partial or retried import shows many "already imported" rows. Behaviour is correct but was confusing before summary copy improved. | **Partially addressed** in import summary labels and narrative; consider a single-line link to "what dedupe means" in docs or UI. |
| **BL-018** | Import column mapping UI | Official Monzo CSV with full auto-detect still shows a large **Confirm column mapping** table every time; feels redundant when headers match and all required columns are detected. | **Spec tension:** [FR-M002](../specs/001-potsure-v1/spec.md) and **T138** require user **confirm or adjust** before import (not silent skip when remembered layout matches). **UX direction (feedback):** for **trusted official layout** (supported Monzo layout, no missing required headers, auto-map complete), default to a compact summary + primary **Import** / **Confirm mapping and import**, with the full header mapping table in a **collapsible** ("Check or change column mapping"). Expand by default when mapping incomplete, parse preview errors, or non-standard/unsupported-adjacent layout. Keeps spec confirm step without blocking happy path. |

## Local development (Aspire)

| ID | Theme | Observation | Possible direction |
|----|--------|-------------|-------------------|
| **BL-006** | Ephemeral local SQL | Stopping Aspire (or waiting between sessions) resets the SQL container; users re-register and re-import. Expected for default `RunAsContainer` without a data volume. | **Fixed** Nov 2025: AppHost `RunAsContainer` uses `WithDataVolume("potsure-sql-data")` and `ContainerLifetime.Persistent`; trade-offs in [`quickstart.md`](../specs/001-potsure-v1/quickstart.md). |

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
