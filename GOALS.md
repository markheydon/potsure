# Goals

**Project:** Potsure  
**Last updated:** 2026-11-02

---

## Why This Exists

Many people maintain a pots-based or category budget in a spreadsheet, then spend time each week reconciling bank activity by hand. Potsure exists to replace that upkeep with statement-driven planning while keeping the same plan-versus-actual clarity.

Potsure has two primary goals of **equal weight**:

1. **Discover and categorise recurring transactions** from uploaded statements.
2. **Track actual spending** against the chosen budget allocation.

Neither is secondary to the other.

---

## Goals for v1.0

| ID | Goal | Success looks like |
|----|------|-------------------|
| **G1** | Remove weekly manual upkeep | A user can go weeks without touching the app, upload fresh statements, and get an accurate plan in minutes. |
| **G2** | Discover recurring income and expenses | The app suggests recurring income and payments from uploaded statements; users confirm rather than type them in. |
| **G3** | Track actual vs. budgeted by category | Each budget category shows planned, actual, and under/over at a glance, retaining the spreadsheet’s plan-versus-actual comparison. Optional bank-pot detail shows how the category total is made up. |
| **G4** | Surface shortfalls early | The app flags budget categories projected to run short, shows the overall income shortfall, and identifies the limiting budget category. Bank-pot detail highlights contributing funding gaps where pots are configured. |
| **G5** | Confirm the budget balances | A clear yes/no on whether income and outgoings fit the selected **50/30/20** or **75/15/10** ratio. |
| **G6** | Be safe to run independently | Financial information is protected and access is restricted, including when an instance is available over the internet. |

---

## How Success Is Judged Day to Day

### Different schedules, the same useful check

Someone paid weekly and someone receiving irregular self-employed income can review a chosen budget period **without** setting a fixed payday. The default look-ahead is **three months**.

Users may start with one month of statements, identify recurring payments themselves, and add history a month at a time. At least **three months** of history improves automatic suggestions. Annual payments may need several years of history or manual identification.

### Reports users can revisit daily

The same inputs must give the same totals. Uploading overlapping statements must **not** count transactions twice.

Expected income must be clearly distinguished from money already received. The app must **not** imply that costs are covered when funding or payment dates are uncertain.

---

## Kill Criteria

Stop investing in the current direction if:

- Users cannot trust imports (silent data loss, double-counting, or unclear errors).
- Recurring detection plus confirmation does not materially reduce manual entry versus a spreadsheet for the target audience.
- Plan-versus-actual reports disagree with independently verified totals for the same inputs.
- A hosted or internet-exposed deployment cannot meet the security bar in **G6**.

---

## What This Is NOT For

(See also: [SCOPE.md](SCOPE.md))

- General accounting, tax filing, or investment advice.
- Live bank connections as a substitute for deliberate statement upload (unless explicitly added to scope later).
- Custom budget category schemes or arbitrary allocation percentages outside the supported presets.
- Promising that bills are paid or pots are funded when the product only has forecasts or incomplete history.

---

## Revision History

| Date | Change | Reason |
|------|--------|--------|
| 2026-11-02 | Replaced template placeholders with Potsure goals and success criteria | Product definition |
| (prior) | Initial draft from csharp-oss-template | New repository |
