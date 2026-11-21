# Specification Quality Checklist: Potsure v1

**Purpose**: Validate specification completeness and quality before proceeding to planning  
**Created**: 2026-11-02  
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- Validation pass (iteration 1): All checklist items satisfied. Spec aligns with constitution budget model, Monzo-only v1 scope, deployment modes, and MoSCoW priorities from user input.
- Validation pass (iteration 2, 2026-11-02): Re-reviewed against updated GOALS.md, SCOPE.md, and constitution v1.0.0. Added **FR-M036** (deterministic report totals), **FR-X004** (accounting/tax/investment exclusions), **SC-009** (G1 return-visit upkeep), product-alignment header, and determinism edge case. No contradictions found; MoSCoW requirement grouping retained (richer than base spec template). **2026-11-04**: hosted Identity pivot aligned to constitution **v1.2.0** in spec/plan/tasks; checklist not re-run item-by-item.
- Validation pass (iteration 3, 2026-11-09): Amended **spec.md** in place (not a new feature folder) from post-implement feedback: standing budget plan without plan dates; estimated income + cadence; report date range; income credit attribution (**FR-M010b**); actual income rolling average cap 12 months (**FR-M019**); **FR-M031** / **FR-M031a**. Prior 2026-11-02 clarifications marked superseded where needed. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 4, 2026-11-09): Pot mapping modes: **FR-M013** (three pots vs several pots, optional per-category mapping, mode-switch guard); **FR-M013b** (default pot on expense assignment); **US4** acceptance scenarios expanded. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 5, 2026-11-09): Maintenance flows: **FR-M008c** (edit/delete confirmed recurring); **FR-M013c** (edit/delete mapped pots); **US2**, **US4**, **US9** updated. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 6, 2026-11-09): In-place amend (not a new feature folder): **FR-M008d** (user override of inferred fixed vs variable at confirm, merge, split, and edit; override persists on re-import); Session 2026-11-09 clarification; **US2** scenario 12; edge cases; **Key Entities**. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 7, 2026-11-09): Report-scoped category allocation **proration** to inclusive report date range (clarify session); **FR-M017**, **FR-M017a**, **FR-M020**, **FR-M031**, **US6**, edge cases, **Key Entities**. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 8, 2026-11-09): **FR-M018** estimated income **prorated** to report range (same rule as **FR-M017**); **US7**, edge cases, **Key Entities**. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 9, 2026-11-09): Default report date range = **current calendar month** on first report open (**FR-M031a**); edge cases, **Key Entities**. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 10, 2026-11-09): **Pot mapping mode** triad (**category-only**, **three pots**, **several pots**); **FR-M013**, **FR-M013b**, **FR-M014**, **US4**, edge cases, **Key Entities**. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 11, 2026-11-09): **Shared in-session report date range** across report views (**FR-M031a**, **FR-M036**); **US3**, edge cases, **Key Entities**. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 12, 2026-11-09): **Variable expense commitments** — stored user-confirmed monthly estimate for **FR-M018** / **FR-M020**; **FR-M034** suggestion-only (**FR-M008c**, edge cases, **Key Entities**). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 13, 2026-11-09): **Runway window** — report start through report end + **N** horizon months; actuals report-range only; allocation and commitments over full runway (**FR-M020**, **FR-M035**, **US8**, edge cases, **Key Entities**). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 14, 2026-11-09): **Proration divisor** **30.4375** days for **FR-M017**, **FR-M018**, **FR-M020**, **FR-M036** / **SC-004**. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 15, 2026-11-09): **Income cadence → monthly equivalent** pay-period factors (**FR-M031**, **US3**, edge cases, **Key Entities**). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 16, 2026-11-09): **Actual-income rolling average** default **3 months** (**FR-M019**, edge cases, **Key Entities**). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 17, 2026-11-09): **Variable expense commitments** — **30.4375** day-based proration on stored monthly estimate (**FR-M018**, **FR-M020**); no calendar-month overlap (**FR-M036** / **SC-004**). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 18, 2026-11-09): **Runway end date** — report end + **N** calendar months with last-day clamp (**FR-M020**, **FR-M035**). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 19, 2026-11-09): **Actual-income rolling window** persisted on standing plan (**FR-M019**); not in-session-only. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 20, 2026-11-09): **Pot delete funding** — user chooses roll to category or discard with confirm when non-zero (**FR-M013c**, **US4**). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 21, 2026-11-09): **Report date range** shared per browser tab only; not synchronized across tabs (**FR-M031a**, **US3**). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 22, 2026-11-09): **Payment anchor** for fixed recurring expense commitment counting (**FR-M008c**, **FR-M018**, **FR-M020**, edge cases, Key Entities). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 23, 2026-11-09): **FR-M018** overall required income = max of per-category commitments÷preset%; limiting category tie **Needs → Wants → Savings & Debt** (**US7**, edge cases, Key Entities). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 24, 2026-11-09): **FR-M019** rolling window = **N** calendar months ending report range end (reports) or today (plan); monthly mean indication (**US3**, edge cases, Key Entities). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 25, 2026-11-09): **FR-M036** banker's rounding to 2dp at report emission (**SC-004**, edge cases). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 26, 2026-11-09): **Annual/rare fixed** expense yearly occurrence projection (**FR-M007**, **FR-M008c**, **FR-M018**, **FR-M020**, edge cases, Key Entities). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 27, 2026-11-09): **Fixed monthly** occurrence counting — calendar-month stepping from anchor with last-day clamp (**FR-M018**, **FR-M020**, **FR-M036**, edge cases, Key Entities). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 28, 2026-11-09): **FR-M034** suggestion window ends on **today** (not report range end); **US8**, edge cases, **FR-M034**. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 29, 2026-11-09): **Rejected** recurring patterns may be **suggested again**; no permanent suppress or auto-confirm (**FR-M008**, edge cases, Key Entities). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 30, 2026-11-09): **Estimated income** unset → allocations and **FR-M018** **not set** (not £0); **FR-M031**, **US3**, edge cases, Key Entities. All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Validation pass (iteration 31, 2026-11-09): **Forecast horizon** **N** constrained **1–12** months, default **3**, persisted on standing plan (**FR-M035**, **US8**, edge cases, Key Entities). All checklist items pass; no `[NEEDS CLARIFICATION]` markers.
- Ready for `/speckit-converge` (recommended) then `/speckit-plan` refresh so `plan.md` / `tasks.md` match standing-plan pivot, income cadence maths, proration (**30.4375**), variable commitments, runway window, **FR-M019** default window, pot-delete funding, runway end-date rules, payment-anchor rules, fixed-monthly occurrence stepping, **FR-M034** suggestion window, reject re-suggestion, income-not-set UX, and horizon bounds.
