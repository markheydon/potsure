# Scope

**Project:** Potsure  
**Last updated:** 2026-11-02

Boundaries below support [GOALS.md](GOALS.md). Detailed v1 behaviour lives in feature specifications; this file states what the product is for and what it deliberately excludes.

---

## In Scope — v1.0

Aligned with **G1–G6**:

- **Statement import:** Upload bank statement files, map fields explicitly, import transactions with de-duplication across overlapping uploads, and report errors without silent drops. **V1** (per [specs/001-potsure-v1/spec.md](specs/001-potsure-v1/spec.md)) is **Monzo-format CSV for a single account** only.
- **Recurring patterns:** Suggest recurring income and expenses from history; users confirm, adjust, merge, split, or reject suggestions rather than typing every item from scratch.
- **Budget model:** Fixed category set with **50/30/20** (default) or **75/15/10** presets; category allocations derived from budgeted income for the selected period.
- **Period planning:** User-chosen budget period independent of pay schedule; default forecast horizon of three months; actual receipts kept separate from expected or planned income.
- **Plan vs. actual:** Per-category planned, actual, and under/over views; optional bank-pot breakdown where pots are mapped to categories.
- **Funding and shortfalls:** Record amounts already set aside; flag categories projected to run short; show overall income shortfall, limiting category, and budget balance (yes/no with monetary gap) for the same period.
- **Low-touch upkeep:** Workflows that stay accurate after gaps between visits when fresh statements are uploaded.
- **Deployment and security:** Local-only use without a hosted account, and hosted use with authenticated access and isolated user data (**G6**).

---

## Out of Scope

Do not implement without an explicit scope change.

- Accounting ledgers, invoicing, payroll, or tax products.
- Open-ended custom budget categories or user-defined allocation percentages.
- Mandatory weekly reviews, fixed paydays, or cadence enforced by the product.
- Treating imported transactions as automatically moving money between pots or establishing live bank balances without user-recorded funding.
- Claiming costs are covered when funding timing or payment certainty is unknown.
- Investment, debt consolidation, or credit-score features unrelated to category planning.

Bank-specific import formats, deployment modes, and non-functional requirements may be narrowed further in specifications and ADRs without contradicting the goals above.

---

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2026-11-02 | Defined Potsure v1 boundaries from product goals | Product definition |
| (prior) | Initial draft from csharp-oss-template | New repository |
