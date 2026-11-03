# Feature Specification: Potsure v1

**Feature Branch**: `001-potsure-v1`

**Created**: 2026-11-02

**Status**: Draft

**Product alignment**: [GOALS.md](../../GOALS.md) **G1–G6**; [SCOPE.md](../../SCOPE.md) v1 boundaries; non-negotiable budget and deployment rules in [constitution v1.1.0](../../.specify/memory/constitution.md).

**Input**: Build Potsure, a web app that helps people run a pots-based budget from bank statements with minimal manual upkeep (original specify prompt).

**Last reviewed**: 2026-11-02 — reconciled against current GOALS, SCOPE, constitution, and repository templates (no scope change).

## Clarifications

### Session 2026-11-02

- Q: When a user exports their plan and stored transactions, what portable format should v1 deliver? → A: User chooses at export time between a versioned JSON bundle and a ZIP of CSV files (transactions plus separate plan/metadata CSV).
- Q: If an uploaded statement contains transactions in a currency other than GBP, what should v1 do by default? → A: Reject the upload (or non-GBP rows) with a clear error; no user override to import non-GBP amounts in v1.
- Q: When mapped pot amounts plus unassigned amounts within a category would exceed that category’s budget allocation, how should the system respond? → A: Allow saving but show a persistent visible over-allocation warning on category health until corrected.
- Q: For Monzo CSV import, how much field mapping should v1 require from the user? → A: Auto-detect Monzo columns when possible; user confirms or adjusts mapping before import; remembered when layout unchanged.
- Q: When a user cannot use a passkey on their current device on a hosted instance, what should v1 offer? → A: Clear guidance to use a device with a registered passkey or register a passkey on a supported device; no non-passkey login on hosted v1. Account rescue mechanisms (e.g. one-time keys at signup) are out of v1 scope for future design.
- Q: For imported spending that does not match any confirmed recurring expense pattern—and is not manually categorised as a one-off—how should it affect category “actual activity” on category health? → A: Only debits matched to confirmed recurring or one-off categorisation count in category actuals; unmatched debits are excluded from categories but shown as uncategorised spend with a persistent visible warning until assigned.
- Q: When the same Monzo transaction appears twice within one file or across overlapping uploads, which rule should v1 use to treat two rows as the same transaction for de-duplication? → A: Prefer Monzo Transaction ID when available in the mapped CSV; otherwise duplicate = same date + amount + normalised description.
- Q: When an imported expense debit clearly matches a user’s confirmed recurring expense pattern, should v1 automatically count it toward that pattern’s category (and optional pot) in category actual activity without asking again on each import? → A: Auto-attribute matching debits to the confirmed pattern’s category and optional pot; user may reassign individual transactions; corrections persist on re-upload.
- Q: For a user-selected budget period with a start date and an end date, should imported transactions and period-scoped totals include activity on both the first and last day of that range? → A: Both start and end dates inclusive (full calendar days) for imports, actual activity, and period reports.
- Q: If the user switches allocation preset (50/30/20 ↔ 75/15/10) while the same budget period is still selected, should category allocation amounts update straight away from the current budgeted income using the new preset? → A: Immediate recalculation of category allocations for the selected period from budgeted income; recorded funding, actual activity, and commitments unchanged until the user edits them.
- Q: For the budget balance and required-income report (FR-M018), what should count as a period commitment when comparing planned income to the chosen preset? → A: Confirmed recurring expense patterns only, each projected into the selected period (cadence-aware); used with planned income and preset percentages for required income, limiting category, and yes/no balance.
- Q: Must genuine one-off transaction categorisation (**FR-M037**) ship in v1.0.0, or can that release rely on recurring match plus uncategorised-spend warnings until one-offs are added? → A: Required for v1.0.0 — one-off categorisation is part of the first release (release gate for v1.0.0).
- Q: When a user adds an optional tag to a confirmed recurring expense (FR-M009), what should that tag represent in v1? → A: Optional free-text label for display and user reference in the recurring summary (and related lists); no effect on allocations, actuals, commitments, or budget balance calculations.
- Q: When a user defines allocation splits for a confirmed recurring income pattern across Needs, Wants, and Savings & Debt, what rule should v1 enforce on those split percentages? → A: Must sum to 100% across the three categories for each confirmed income pattern (validation blocks save otherwise).
- Q: In v1, should users be able to keep multiple saved budget periods (each with its own dates and budgeted income), or only one working period whose date range and income they change in place? → A: Single working period — one active date range and budgeted income; adjusting dates or income updates that same period (no saved period library).
- Q: For each category on category health, should under/over be computed as the category allocation minus actual activity only (recorded funding stays a separate line and does not change the under/over figure)? → A: Under/over = allocation − actual activity; recorded funding remains a separate measure and MUST NOT be subtracted in the under/over calculation.
- Q: When de-duplicating rows without a Monzo Transaction ID, how should the system normalise the description before comparing two rows (same date and amount)? → A: Trim; collapse internal whitespace to one space; strip non-alphanumeric characters; compare case-insensitively.
- Q: For category runway warnings (FR-M020), should recorded funding count toward covering projected remaining spend, so a warning appears only when projected remaining spend exceeds allocation minus actual activity plus recorded funding for that category? → A: Yes — warn when projected remaining category spend in the window exceeds (allocation − actual activity + recorded funding); optional mapped pot detail in the warning.
- Q: When a user merges two confirmed recurring expense patterns that disagree on category, pot, or tag, how should v1 resolve those assignments for the single merged pattern? → A: User must choose one category, optional pot, and optional tag for the merged pattern before save; future matches and stored attributions use the merged pattern and chosen assignments.
- Q: When a user splits one confirmed recurring expense pattern into two or more patterns, what must v1 require for category, pot, and tag before the split can be saved? → A: User must assign a category and optional pot/tag to each resulting pattern before save; save blocked until every split child is fully assigned.
- Q: For confirmed recurring income patterns with Needs/Wants/Savings & Debt splits totalling 100%, should those splits change any v1 calculations (category allocations, budget balance, or actual receipt breakdown), or stay display-only on the recurring summary? → A: Display-only — category allocations and FR-M018 use budgeted income and the chosen preset only; income splits appear on the recurring summary and related lists with no effect on allocations, commitments, actuals, or budget balance.
- Q: When an imported refund or merchant credit is attributed to a budget category (via recurring match or one-off categorisation), should it reduce that category’s actual activity for the period? → A: Yes — category actual activity is net attributed spend: attributed expense debits minus attributed refunds/credits in that category (same attribution rules as debits).
- Q: How should v1 treat imported Monzo rows that are internal transfers (for example pot moves or paying yourself) rather than spending at a merchant? → A: Exclude from category actual activity and uncategorised spend by default when identifiable as non-spend transfers from statement data; user may one-off categorise to count as category spend if needed.
- Q: Should transactions classified as internal transfers or pot movements be excluded from recurring income and expense pattern detection? → A: Yes — exclude classified transfers from detection input entirely; they do not generate or feed suggested patterns.
- Q: When the user sets budgeted income for the working period, may v1 suggest a figure from confirmed income patterns projected into that period, as long as the user explicitly confirms or edits it before category allocations update? → A: Yes — optional suggestion from confirmed income patterns; category allocations update only after the user confirms or edits budgeted income (no auto-apply on import or pattern change).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Import Monzo statements safely (Priority: P1)

As a lazy believer who avoids weekly spreadsheet upkeep, I upload one or more Monzo-format CSV files for my single Monzo account so my plan stays current from bank activity instead of manual entry.

**Why this priority**: Without trustworthy ingestion, no other budgeting value exists; silent loss or wrong-account imports would destroy trust immediately.

**Independent Test**: Upload valid, overlapping, and invalid files; verify de-duplication, explicit errors, account confirmation when identity is missing, and rejection of other banks or different accounts without importing their transactions.

**Acceptance Scenarios**:

1. **Given** a valid Monzo CSV for the user’s confirmed account, **When** the user uploads it and confirms or adjusts auto-detected column mapping, **Then** all parseable transactions are imported and a summary reports counts, date range, and any skipped rows with clear reasons, **And** rows identified as internal transfers or pot movements are excluded from category actual activity and uncategorised spend unless later one-off categorised as spend.
2. **Given** two uploads with overlapping transactions, **When** the second file is imported, **Then** duplicate transactions are not stored twice (matching by Monzo Transaction ID when present, else same date, amount, and normalised description per FR-M004) and the user sees how many were de-duplicated.
3. **Given** a CSV from another bank or a Monzo file that clearly belongs to a different account, **When** the user attempts import, **Then** the import is rejected and no transactions from that file are stored.
4. **Given** a Monzo CSV with no reliable account identity, **When** the user starts import, **Then** the system requires explicit user confirmation of account identity before persisting transactions.
5. **Given** a malformed or unsupported file, **When** import runs, **Then** the user receives clear errors and no transactions are silently dropped without explanation.

---

### User Story 2 - Detect and confirm recurring patterns (Priority: P1)

As a user with irregular pay and multiple income sources, I want the app to suggest recurring income and expenses (weekly, fortnightly, monthly) so I can confirm, merge, split, or reject each pattern without assuming a fixed payday.

**Why this priority**: Recurring detection drives planned income and commitments; user confirmation keeps forecasts honest when history is short.

**Independent Test**: Import one to three months of data; review suggested patterns; confirm, merge, split, and reject; verify income patterns define sources and splits while expense patterns receive category (and optional pot/tag) assignment.

**Acceptance Scenarios**:

1. **Given** at least one month of statement history, **When** detection runs, **Then** the user sees suggested recurring income and expenses separately from confirmed items, with cadence and fixed or variable amount indication where inferable, **And** rows classified as internal transfers or pot movements are not included in detection input.
2. **Given** variable or ad-hoc income, **When** the user reviews income, **Then** they can confirm amounts without forcing a recurrence pattern or fixed payday.
3. **Given** a user confirms a detected expense pattern, **When** they assign Needs, Wants, or Savings & Debt and optionally a mapped bank pot and tag, **Then** that assignment is stored and appears in the confirmed recurring summary.
4. **Given** a user confirms an income pattern, **When** they define allocation splits across budget categories, **Then** expense category assignment is not applied to income patterns, **And** the three split percentages MUST sum to 100% before the pattern can be saved, **And** those splits MUST NOT alter category allocation amounts, FR-M018 results, or actual receipt breakdown (display-only on recurring summary and related lists).
5. **Given** two confirmed expense patterns with differing category, pot, or tag, **When** the user merges them, **Then** they MUST choose one category, optional pot, and optional tag for the merged pattern before save, **And** subsequent matching and stored attributions apply to the merged pattern with those assignments.
6. **Given** a confirmed expense pattern, **When** the user splits it into two or more patterns, **Then** they MUST assign a category and optional pot and tag to each resulting pattern before save, **And** the system MUST block save until every split child has a category assignment.
7. **Given** re-upload of statements after prior corrections, **When** detection and matching run, **Then** remembered assignments and user corrections are applied without requiring full re-entry, **And** debits matching confirmed expense patterns are auto-attributed to those patterns’ category and optional pot for category actual activity unless the user has reassigned them.

---

### User Story 3 - Choose preset and plan the period (Priority: P1)

As a pots-method budgeter, I choose 50/30/20 (default) or 75/15/10, select a budget period independent of pay schedule, and confirm expected or estimated income for that period while keeping actual receipts separate from forecasts.

**Why this priority**: The three fixed categories and two presets are the non-negotiable budget model; period and income inputs anchor all allocations and reports.

**Independent Test**: Switch presets; set a custom date range; enter planned income; verify category allocations derive only from total budgeted income and the chosen preset percentages.

**Acceptance Scenarios**:

1. **Given** a new plan, **When** the user has not changed settings, **Then** the 50/30/20 preset applies to Needs, Wants, and Savings & Debt respectively.
2. **Given** the user selects 75/15/10, **When** allocations are calculated for the period, **Then** category percentages change accordingly and no custom ratios are offered, **And** switching preset again during the same selected period immediately recalculates category allocation amounts from the current budgeted income while leaving recorded funding, actual activity, and commitments unchanged until edited.
3. **Given** the single working budget period, **When** the user confirms expected or estimated income for that period, **Then** category allocation amounts equal the preset percentages of that total budgeted income, **And** transactions dated on the period start or end date are included in period-scoped totals, **And** the system MAY suggest budgeted income from confirmed income patterns projected into the period but MUST NOT apply that suggestion to allocations until the user explicitly confirms or edits the figure.
4. **Given** the user changes the working period’s start or end dates, **When** reports reload, **Then** period-scoped totals use the updated inclusive range and there is no separate library of alternate saved periods in v1.
5. **Given** actual receipts in statements, **When** reports show income, **Then** actual receipts remain distinct from planned or forecast income and are not treated as already covering future costs.

---

### User Story 4 - Map bank pots or budget by category only (Priority: P2)

As a Monzo user, I either use three pots named after the categories or several expense-specific pots (each mapped to exactly one category), or I budget directly by category without any bank pots.

**Why this priority**: Supports real Monzo workflows without letting pots override preset mathematics.

**Independent Test**: Configure three category pots or multiple expense pots; record funding and spending; verify each amount counts once in its category total.

**Acceptance Scenarios**:

1. **Given** the user defines bank pots, **When** each pot is mapped to exactly one of Needs, Wants, or Savings & Debt, **Then** pots have no hierarchy and no independent preset percentages.
2. **Given** mapped pot amounts and direct category amounts, **When** category totals are shown, **Then** combined totals include both without double-counting the same money.
3. **Given** the user chooses not to configure pots, **When** they record funding and assign expenses by category, **Then** the plan works fully at category level with optional pot detail omitted.

---

### User Story 5 - Record funding already set aside (Priority: P2)

As a user who moves money into pots or mentally earmarks category balances, I record amounts already set aside separately from planned future funding, knowing that imported statements do not move money or establish live bank-pot balances.

**Why this priority**: Separates “already funded” from “still to fund” and avoids implying statement import changes real-world pot balances.

**Independent Test**: Enter recorded funding against a category and against a mapped pot; verify it appears separately from planned future funding and from imported transaction activity.

**Acceptance Scenarios**:

1. **Given** recorded funding on a category, **When** the period view loads, **Then** allocation, recorded funding, actual activity, and under/over appear as separate measures.
2. **Given** recorded funding in a mapped pot, **When** category health is computed, **Then** pot funding rolls into the parent category once only.
3. **Given** newly imported transactions, **When** the user views funding, **Then** imports do not automatically increase recorded funding or live pot balances.

---

### User Story 6 - Category health and actual spend (Priority: P1)

As a primary “lazy believer” audience member, I see allocation, recorded funding, actual spending, and under/over for Needs, Wants, and Savings & Debt for my selected period, with optional mapped pot detail.

**Why this priority**: This is the low-effort current-period check that replaces spreadsheet upkeep.

**Independent Test**: With a populated plan and imports, open category health; verify four measures and optional pot breakdown; confirm one-off spend categorised at transaction level appears as actual-only without creating forecast purchases; verify unmatched debits appear as uncategorised spend with warning and do not inflate category actuals.

**Acceptance Scenarios**:

1. **Given** a selected period with planned allocations and imports, **When** the user views category health, **Then** each category shows allocation, recorded funding, actual activity, and under/over clearly labelled, **And** under/over equals allocation minus actual activity (recorded funding does not alter under/over).
2. **Given** amounts assigned to mapped pots plus amounts not assigned to any pot, **When** their combined recorded funding exceeds the category’s budget allocation, **Then** the entries are saved and category health shows a persistent visible over-allocation warning until corrected; **When** combined amounts do not exceed allocation, **Then** no over-allocation warning is shown.
3. **Given** a genuine one-off expense, **When** the user categorises it at transaction level, **Then** it contributes to actual spend only and never generates a forecast recurring purchase.
4. **Given** a refund or merchant credit attributed to a category (recurring match or one-off), **When** category health is computed, **Then** that category’s actual activity reflects net attributed spend (attributed debits minus attributed refunds/credits for the period).
5. **Given** imported expense debits in the selected period that do not match any confirmed recurring pattern and are not one-off categorised, **When** the user views category health, **Then** those debits do not increase any category’s actual activity, **And** their total appears as uncategorised spend with a persistent visible warning until each debit is assigned (via pattern match, one-off categorisation, or equivalent user action).

---

### User Story 7 - Required income, limiting category, and budget balance (Priority: P1)

As a user checking whether my plan fits the chosen ratio, I see required income per category, overall requirement, shortfall, the limiting category driving ratio-related shortfall, and a clear yes/no with monetary gap—using the same period for planned income and commitments while keeping actual receipts separate.

**Why this priority**: Delivers the core “does my plan fit 50/30/20 (or 75/15/10)?” answer with honest separation of plan vs reality.

**Independent Test**: Configure commitments and income short of requirement; verify limiting category, shortfall amount, and qualified yes/no conclusion with monetary gap.

**Acceptance Scenarios**:

1. **Given** planned income and confirmed recurring expense commitments projected into the same selected date range, **When** the user opens the budget balance report, **Then** required income is shown per category and in total using the chosen preset percentages at category level (not at pot level), incorporating those projected expense commitments.
2. **Given** a ratio-related shortfall, **When** results are displayed, **Then** the limiting budget category is identified with contributing mapped pot detail where configured.
3. **Given** planned income below required income, **When** the yes/no conclusion is shown, **Then** the user sees a monetary gap and messaging that does not imply costs are covered by money not yet received.
4. **Given** weekly expected receipts, **When** income timing is shown, **Then** dates reflect expected receipt schedule (not an assumed four payments per month).

---

### User Story 8 - Category runway warnings (Priority: P2)

As a user who may review occasionally or daily, I receive warnings when a category is projected to run short within the selected budget period or forecast horizon, with optional pot funding detail, without requiring a single next payday.

**Why this priority**: Prevents surprise shortfalls while respecting irregular income.

**Independent Test**: Set forecast horizon (default three months, user override); trigger projected shortfall; verify warning text and optional pot detail; confirm statement-history gaps and uncertain income are disclosed.

**Acceptance Scenarios**:

1. **Given** confirmed recurring variable spending, **When** forecast uses recent history, **Then** the system uses the last three months’ average monthly total when available, or shorter history with explicit labelled coverage and user correction path.
2. **Given** a selected forecast horizon defaulting to three months, **When** the user overrides the horizon, **Then** warnings recalculate for the chosen range.
3. **Given** gaps in statement history or uncertain income dates or amounts, **When** warnings or forecasts are shown, **Then** those limitations are disclosed alongside projections.
4. **Given** recorded funding and category allocation, actual activity, and projected remaining category spend for the warning window, **When** projected remaining spend exceeds `(allocation − actual activity + recorded funding)` for that category (recorded funding includes mapped-pot amounts rolled up once at category level), **Then** the system shows a runway warning with optional mapped pot detail; **When** projected remaining spend does not exceed that amount, **Then** no runway warning is shown for that category for that window.

---

### User Story 9 - Confirmed recurring summary (Priority: P2)

As a user maintaining my plan, I view the complete confirmed recurring income and expense summary—including cadence, fixed or variable amounts, expense assignments, and income splits—with unconfirmed suggestions kept separate.

**Why this priority**: Single place to audit what drives the plan after confirmation workflows.

**Independent Test**: Confirm several patterns; open summary; verify all confirmed fields present and suggestions listed separately.

**Acceptance Scenarios**:

1. **Given** confirmed recurring items, **When** the user opens the recurring summary, **Then** each entry shows cadence, amount behaviour, category (for expenses), optional pot and tag, and income splits (for income).
2. **Given** pending suggestions, **When** the summary is viewed, **Then** unconfirmed items are visually and structurally separate from confirmed entries.

---

### User Story 10 - Export and permanent deletion (Priority: P1)

As a privacy-conscious user or self-hoster, I export my current plan and stored transaction data and can permanently delete all my data.

**Why this priority**: Mandatory v1 outcomes for trust, portability, and compliance with minimal-data principles.

**Independent Test**: Export plan and transactions; verify completeness for the user’s scope; delete all data; verify no user financial data remains retrievable.

**Acceptance Scenarios**:

1. **Given** an active plan with imports, **When** the user requests export and selects a format, **Then** they receive their plan and stored transaction data as either a versioned JSON bundle (single file or ZIP of JSON parts) or a ZIP of CSV files (transactions plus separate plan/metadata CSV), with the chosen format documented for portability.
2. **Given** the user confirms permanent deletion, **When** deletion completes, **Then** all their financial data is removed and they receive confirmation; hosted instances enforce user isolation so deletion affects only their data.

---

### User Story 11 - Local-only and hosted access modes (Priority: P1)

As a self-hoster or local user, I run Potsure in account-free local-only mode on my device, or I use a hosted instance where every user—including a sole user—must authenticate with passkey support and isolated data.

**Why this priority**: Constitution-mandated deployment paths; wrong exposure of local mode or weak hosted auth would violate product trust.

**Independent Test**: Run local-only mode without network account; deploy hosted instance; verify passkey authentication required and users cannot access each other’s data.

**Acceptance Scenarios**:

1. **Given** local-only mode, **When** the user works with their budget, **Then** no hosted account is required and the mode is not exposed as a network service.
2. **Given** a hosted instance, **When** any user accesses the app, **Then** authentication with passkey support is required before viewing or changing financial data.
3. **Given** multiple users on a hosted instance, **When** each user operates normally, **Then** financial data is isolated per user with no cross-user visibility.

---

### User Story 12 - Accessible, responsive review (Priority: P3)

As a user who may review on phone or keyboard-only, I navigate imports, confirmations, and reports with responsive layout and full keyboard operability.

**Why this priority**: Inclusive use without mandating a review cadence or live bank connection.

**Independent Test**: Complete primary flows using keyboard only and on a narrow viewport; verify operability and readable reports.

**Acceptance Scenarios**:

1. **Given** core flows (import, confirm recurring, view category health, export), **When** accessed via keyboard only, **Then** all actions are reachable and activatable without a pointer.
2. **Given** a mobile-width viewport, **When** the user views reports, **Then** layout remains usable without horizontal scrolling for primary content.

---

### Edge Cases

- Income pattern split percentages that do not total 100% MUST be rejected on save with a clear validation message.
- Budget balance and required income (FR-M018) use period commitments from confirmed recurring **expense** patterns only (cadence-projected into the selected period); income patterns, one-offs, recorded funding, and imports are excluded from commitment totals.
- Upload contains duplicate rows within the same file versus duplicates across files; both must de-duplicate with reported counts using Monzo Transaction ID when mapped and present, otherwise same date, amount, and normalised description (trim, collapse internal whitespace, strip non-alphanumeric characters, case-insensitive compare).
- Upload spans gaps in calendar coverage; reports and forecasts disclose missing history.
- Only one month of history on first use; detection may be incomplete—user-led confirmation remains available without blocking onboarding.
- Annual or rare payments with multi-year history; detection may miss some—limits are disclosed, not guaranteed complete.
- Variable recurring amounts with fewer than three months of history; forecasts use available data with labelled coverage.
- Income arrives on irregular dates (self-employed, multiple sources); budget period does not require aligning to a payday.
- User maps multiple expense pots to the same category; totals still aggregate once at category level.
- User attempts to exceed category allocation across pots plus unassigned amounts; system saves the entries and shows a persistent visible over-allocation warning on category health until corrected.
- Account identity ambiguous across sequential uploads; user must confirm before merge.
- Re-import after partial deletion or export; behaviour remains consistent with de-duplication and remembered assignments.
- Same stored inputs and settings viewed twice; category health and budget balance totals match on both views.
- Budget period boundaries are inclusive of both start and end calendar dates for transaction inclusion and period-scoped report totals.
- v1 exposes one working budget period only; users adjust dates and budgeted income in place—no multi-period save/switch library.
- User changes allocation preset mid-period; category allocations recalculate immediately from budgeted income; recorded funding, actual activity, and commitments remain until user updates them (may trigger over- or under-allocation warnings); under/over recalculates from updated allocation minus unchanged actual activity.
- Forecast horizon shorter than recurring cadence (e.g. weekly bill, one-week horizon); warnings remain mathematically consistent and labelled.
- Category runway warning triggers when projected remaining spend in the window exceeds allocation minus actual activity plus recorded funding (pot funding rolled up at category level); optional pot detail in messaging.
- Permanent deletion is irreversible; export is offered as the recovery path before deletion.
- Hosted passkey unavailable on a device; user MUST receive clear guidance to authenticate on a device with a registered passkey or to register a passkey on a supported device, with no non-passkey login bypass on hosted v1.
- Currency other than GBP in a file; import MUST reject the upload or affected rows with a clear error and MUST NOT offer an override to persist non-GBP amounts in v1 (no silent conversion).
- Confirmed recurring income pattern splits totalling 100% are display-only; category allocations and budget balance use budgeted income and preset only, not per-pattern splits.
- Confirmed recurring rejected then similar transactions appear; user can re-suggest without auto-confirming.
- User merges confirmed expense patterns with conflicting assignments; system requires choosing one category, optional pot, and optional tag before save; merged pattern drives future matches and attributions.
- User splits a confirmed expense pattern; each resulting pattern must have category (and optional pot/tag) assigned before save is allowed.
- Imported expense debits unmatched to confirmed recurring and not one-off categorised must not count toward category actual activity; uncategorised spend total and persistent warning remain until user assigns each debit.
- Attributed refunds or merchant credits reduce category actual activity (net attributed spend); unattributed refunds or credits do not change category actuals until assigned with the same rules as debits.
- Rows identifiable as internal transfers or pot movements (non-merchant spend) are excluded from category actual activity and uncategorised spend by default; import summary reports how many were classified as transfers; user may one-off categorise a transfer as category spend when appropriate.
- Classified internal transfers and pot movements are excluded from recurring pattern detection input and MUST NOT produce suggested recurring items.
- Budgeted income may be suggested from confirmed income patterns for the period; allocations MUST NOT change from a suggestion until the user confirms or edits budgeted income.

## Requirements *(mandatory)*

### Functional Requirements — Must

- **FR-M001**: The system MUST accept upload of one or more Monzo-format CSV statement files per user for a single Monzo account only.
- **FR-M002**: The system MUST auto-detect Monzo CSV column mapping when the layout matches a known Monzo export; the user MUST confirm or adjust mapping before import proceeds; unchanged layouts MUST reuse the remembered mapping. The system MUST reject unsupported formats with clear errors.
- **FR-M003**: The system MUST NOT silently drop transactions; every skipped or failed row MUST be reported with a reason. Rows classified as internal transfers or pot movements MUST be reported in the import summary (counts and reasons) and MUST NOT contribute to uncategorised spend or category actual activity unless the user later one-off categorises them as spend.
- **FR-M004**: The system MUST de-duplicate imported transactions within a file and across overlapping uploads and report de-duplication counts. Two rows represent the same transaction when they share the same Monzo Transaction ID from the mapped CSV, or—when that ID is absent for either row—when they share the same transaction date, amount, and **normalised description**. Normalisation for comparison MUST: trim leading and trailing whitespace; collapse runs of internal whitespace to a single space; remove all non-alphanumeric characters from the description; compare the result case-insensitively.
- **FR-M005**: The system MUST reject other banks’ CSV layouts and files identifiable as a different account without importing their transactions.
- **FR-M006**: When account identity is absent or ambiguous, the system MUST require explicit user confirmation before persisting transactions.
- **FR-M007**: The system MUST detect recurring income and expenses with cadences including weekly, fortnightly, and monthly where evidence exists. Transactions classified as internal transfers or pot movements (per FR-M017) MUST be excluded from detection input and MUST NOT generate suggested recurring patterns.
- **FR-M008**: The system MUST allow confirmation, merge, split, and reject for each detected pattern and MUST separate confirmed from unconfirmed suggestions. When the user merges two **confirmed recurring expense** patterns whose category, optional pot, or optional tag differ, the system MUST require the user to choose a single category, optional pot, and optional tag for the merged pattern before persisting; subsequent auto-attribution and assignment memory MUST use the merged pattern and those chosen assignments. When the user splits a **confirmed recurring expense** pattern into two or more patterns, the system MUST require a category assignment (and optional pot and tag) for **each** resulting pattern before persisting the split and MUST block save until every resulting pattern has a category assigned.
- **FR-M009**: The system MUST assign confirmed expense patterns to exactly one of Needs, Wants, or Savings & Debt, with optional mapped bank pot and optional **tag** (free-text label for display only; MUST NOT affect allocations, actual activity, commitments, or budget balance maths).
- **FR-M010**: The system MUST treat income patterns as sources with allocation splits across Needs, Wants, and Savings & Debt, not as expense category assignments. For each confirmed income pattern, split percentages MUST sum to exactly 100%; the system MUST reject save until satisfied. Income splits MUST be **display-only** labels on the recurring summary and related lists; they MUST NOT affect category allocations (FR-M011), period commitments, category actual activity, actual receipt breakdown, or budget balance calculations (FR-M018).
- **FR-M010a**: The system MUST remember expense assignments and user corrections on re-upload. Expense debits that match a confirmed recurring expense pattern MUST be auto-attributed to that pattern’s category and optional pot for category actual activity without per-import re-confirmation; attributed refunds or merchant credits MUST reduce category actual activity under the same assignment rules. The user MUST be able to reassign individual transactions, and those corrections MUST persist on subsequent imports.
- **FR-M011**: The system MUST support only two allocation presets: 50/30/20 (default) and 75/15/10 (alternative), applied to the three fixed categories only. When the user changes preset for the currently selected budget period, the system MUST immediately recalculate category allocation amounts from the period’s budgeted income using the new preset without requiring a new period; recorded funding, actual activity, and commitment inputs MUST remain unchanged until the user edits them.
- **FR-M012**: The system MUST NOT allow removal, replacement, or custom percentages for the three budget categories in v1.
- **FR-M013**: The system MUST allow optional bank pots mapped one-to-one to categories, with no pot hierarchy or independent preset ratios.
- **FR-M014**: The system MUST allow budgeting directly by category without bank pots.
- **FR-M015**: The system MUST record amounts already set aside separately from planned future funding at category or mapped-pot level.
- **FR-M016**: Imported statements MUST NOT move money, set live bank-pot balances, or auto-establish funding without user recording.
- **FR-M017**: For the selected period, the system MUST show allocation, recorded funding, actual activity, and under/over per category, counting each amount once in category totals. **Under/over** MUST equal category allocation minus category actual activity for that period; recorded funding MUST be displayed separately and MUST NOT be included in the under/over calculation. Category **actual activity** MUST equal **net attributed spend** for that category in the period: the sum of expense debits attributed via match to a confirmed recurring expense pattern or via one-off transaction categorisation, minus attributed refunds or merchant credits under the same attribution rules. Unattributed debits MUST NOT be allocated across categories. Unattributed refunds or credits MUST NOT reduce category actual activity until assigned. Rows identifiable as **internal transfers or pot movements** (non-merchant spend) MUST NOT enter category actual activity or uncategorised spend unless the user explicitly one-off categorises them as spend.
- **FR-M017a**: When recorded funding or pot assignments cause mapped-pot plus unassigned amounts to exceed a category’s budget allocation, the system MUST persist the user’s entries and MUST display a persistent visible over-allocation indicator on category health until corrected.
- **FR-M017b**: For the selected period, the system MUST total imported expense debits that are not attributed to any category (per FR-M017) as **uncategorised spend**, display that total separately from the three category actual figures, and show a persistent visible warning on category health until each such debit is assigned. Internal transfers and pot movements excluded under FR-M017 MUST NOT be included in this total.
- **FR-M018**: The system MUST compute required income per category, overall requirement, shortfall, limiting category for ratio-related shortfall, and a qualified yes/no budget balance with monetary gap using the same period for planned income and **commitments**. Period **commitments** MUST be derived only from **confirmed recurring expense patterns**, each projected into the selected period in a cadence-aware way; income patterns, one-off debits, recorded funding, and unattributed imports MUST NOT be treated as commitments for this calculation.
- **FR-M019**: Actual receipts and activity MUST remain separate from planned income and forecasts in all balance and health views.
- **FR-M020**: The system MUST warn when a category is projected to run short within the selected period or forecast horizon, with optional pot detail. **Projected remaining category spend** for the warning window MUST include in-period actual activity to date plus cadence-projected remaining confirmed recurring expense commitments and variable expense estimates for that category (per **FR-M034**) over the overlapping window. A runway warning MUST be shown when projected remaining spend exceeds `(category allocation − category actual activity + category recorded funding)`, where recorded funding includes mapped-pot funding rolled into the category once only; when it does not exceed that amount, the system MUST NOT show a runway warning for that category for that window.
- **FR-M021**: The system MUST present a complete confirmed recurring income and expense summary with cadence, fixed or variable amounts, assignments, and income splits.
- **FR-M022**: The system MUST export the user’s current plan and stored transaction data on demand; the user MUST choose at export time between (1) a versioned JSON bundle (single JSON file or ZIP of JSON parts) and (2) a ZIP of CSV files including stored transactions and separate plan/metadata CSV.
- **FR-M023**: The system MUST permanently delete all of a user’s financial data on confirmed request.
- **FR-M024**: The system MUST provide a local-only, account-free single-user mode that MUST NOT be exposed over a network.
- **FR-M025**: Every hosted instance MUST require authentication with passkey support and MUST isolate user data per account. When passkeys are unavailable on the current device, the system MUST show guidance to use or register a passkey on a supported device and MUST NOT offer non-passkey login on hosted v1.
- **FR-M026**: The system MUST operate in GBP for v1 monetary amounts; uploads containing non-GBP transactions MUST be rejected (whole file or affected rows) with a clear error and MUST NOT allow user override to import non-GBP amounts. The system MUST collect only minimal personal data necessary for function.
- **FR-M027**: Hosted deployments MUST encrypt financial data at rest and in transit; the system MUST NOT share financial data with third-party financial-data aggregators.
- **FR-M028**: The system MUST validate and sanitise all user inputs, including uploads and form fields.
- **FR-M029**: The system MUST NOT log raw statements, full account numbers, or secrets.
- **FR-M030**: The system MUST support safe self-hosting documentation outcomes (export, deletion, isolation) as user-facing capabilities, not implementation choices.
- **FR-M031**: v1 MUST provide exactly **one working budget period** per plan (user-editable inclusive start and end dates and budgeted income in place; no library of multiple saved periods). That period MUST be independent of pay schedules. Period boundaries MUST be inclusive of both the start and end calendar dates for transaction inclusion, actual activity, commitments in scope, and all period-scoped reports. The system MAY suggest budgeted income for the period from confirmed income patterns projected into that range; it MUST NOT update stored budgeted income or category allocations from such a suggestion until the user explicitly confirms or edits the budgeted income figure (including after import or income-pattern changes).
- **FR-M032**: The system MUST disclose statement-history gaps and uncertain income dates or amounts where they affect forecasts or warnings.
- **FR-M033**: The system MUST NOT imply that future costs are covered by money not yet received.
- **FR-M034**: For confirmed recurring variable expenses, forecasts MUST use the last three months’ average monthly total when available, or shorter available history with explicit coverage labelling and user correction.
- **FR-M035**: Default forecast horizon MUST be three months with user override; users SHOULD be informed that three months of history is recommended for normal automatic recurring detection without blocking one-month onboarding.
- **FR-M036**: For the same stored transactions, confirmed recurring items, period selection, preset, and user-recorded funding, report totals MUST be identical on repeat viewing (no non-deterministic drift for unchanged inputs).
- **FR-M037**: The system MUST support genuine one-off transaction categorisation at transaction level in v1.0.0. One-off categorisation MUST apply to expense debits, refunds, and merchant credits; it MUST affect category actual activity only (debits increase net spend, refunds/credits decrease it) and MUST NOT create forecast or guessed future recurring purchases.

### Functional Requirements — Should

- **FR-S002**: The system SHOULD provide transaction-level actual-spend breakdown with optional mapped pot detail. **Deferred for v1.0.0** unless explicitly pulled into scope — aggregate category actuals on category health (FR-M017) are in scope; see [tasks.md](./tasks.md) Notes.
- **FR-S003**: One-off categorisation (FR-M037) MUST NOT create forecast or guessed future purchases (Must-level constraint on one-off behaviour).

### Functional Requirements — Could

- **FR-C001**: The system COULD provide period-to-period trend reporting; this remains out of Must scope for v1.

### Functional Requirements — Out of scope (v1)

- **FR-X001**: Multi-account and multi-provider statement consolidation MUST NOT be required or implied as supported in v1.
- **FR-X002**: Live bank feeds or promises of continuous sync MUST NOT be offered in v1.
- **FR-X003**: Mandatory review cadence or payday-aligned budget periods MUST NOT be imposed.
- **FR-X006**: A library of multiple saved budget periods (switching among distinct stored period profiles) MUST NOT be offered in v1; only the single working period in **FR-M031** is in scope.
- **FR-X004**: General accounting, tax filing, investment advice, or unrelated financial products MUST NOT be offered or implied as in scope for v1.
- **FR-X005**: Account rescue or credential recovery beyond passkey-on-supported-device guidance (e.g. one-time recovery keys at signup) MUST NOT be required or implied as shipped in v1; future rescue design is explicitly deferred.

### Key Entities *(include if feature involves data)*

- **User**: Person using Potsure; in hosted mode has authenticated identity; in local-only mode operates without remote account.
- **Budget period**: The single working user-editable date range for planning and reports, independent of pay schedule; both start and end dates are inclusive for period-scoped calculations. v1 does not maintain a library of alternate saved periods—changing dates updates this same period.
- **Budget category**: One of Needs, Wants, Savings & Debt; fixed for v1; receives allocation from preset percentages of budgeted income.
- **Allocation preset**: Either 50/30/20 (default) or 75/15/10 (alternative); applies only at category level.
- **Bank pot (optional)**: User-defined label mapped to exactly one category; no hierarchy; no own ratio.
- **Statement upload**: Monzo CSV file with auto-detected or user-adjusted column mapping (confirmed before import), import status, errors, de-duplication outcome, and remembered mapping when layout unchanged.
- **Transaction**: Dated monetary movement from imports; basis for actual activity and detection input; may be classified as merchant spend, income, refund/credit, or internal transfer/pot movement when identifiable from statement data. Uniqueness for storage uses Monzo Transaction ID when imported, else date + amount + normalised description (per FR-M004 normalisation rules) for de-duplication.
- **Detected pattern**: Suggested recurring income or expense with cadence and amount behaviour; not active until confirmed; MUST NOT be generated from transactions classified as internal transfers or pot movements.
- **Confirmed recurring item**: User-approved pattern with assignments (category, optional pot, optional free-text tag for expenses; splits for income). Expense items contribute to period **commitments** when projected into the selected budget period; income items do not. Tags and income category splits are display-only labels and MUST NOT affect allocations, actuals, commitments, or budget balance. Confirmed **income** patterns MUST have category splits totalling 100%.
- **Income allocation splits**: Per confirmed income pattern, percentages assigned to Needs, Wants, and Savings & Debt; MUST sum to 100% (validated on save); display-only—MUST NOT drive category allocations, actual receipt attribution, or FR-M018 maths.
- **Expense tag (optional)**: User-entered free-text label on a confirmed recurring expense; shown in the recurring summary and related lists; no effect on allocations, actuals, commitments, or budget balance.
- **Period commitment (budget balance)**: Cadence-aware projection of a confirmed recurring **expense** pattern’s expected cost into the selected budget period; sole input class for FR-M018 commitment totals (excluding income patterns, one-offs, funding, and imports).
- **Planned income**: User-confirmed expected or estimated income for a period (budgeted income); distinct from actual receipts. The system MAY suggest a figure from confirmed income patterns but MUST apply allocations only after user confirmation or edit.
- **Actual receipt**: Income observed in statements; tracked separately from plans and forecasts.
- **Recorded funding**: Money user declares already set aside toward a category or mapped pot; distinct from planned future funding.
- **Planned future funding**: User-intended additional funding not yet recorded as set aside.
- **Assignment memory**: Stored user corrections and category/pot/tag choices applied on subsequent imports; includes per-transaction reassignments overriding auto-attribution from confirmed recurring matches.
- **Category health snapshot**: Allocation, recorded funding, actual activity, under/over for a period, optionally broken down by pot; **actual activity** = net attributed spend (attributed debits minus attributed refunds/credits); **under/over** = allocation − actual activity (recorded funding excluded from under/over); excludes unattributed debits from category actuals (see uncategorised spend).
- **Uncategorised spend**: In-period imported expense debits not yet attributed to a category via confirmed recurring match or one-off categorisation; excludes internal transfers and pot movements unless one-off categorised as spend; shown as a separate total with persistent warning until resolved.
- **Required income report**: Per-category and total income needed so planned income and preset percentages fit projected confirmed recurring **expense** commitments for the period; limiting category; shortfall.
- **Budget balance conclusion**: Qualified yes/no whether planned income and commitments fit the chosen ratio for the period, with monetary gap.
- **Forecast horizon**: User-configurable forward window (default three months) for runway warnings; runway compares projected remaining category spend to `(allocation − actual activity + recorded funding)`.
- **Export bundle**: Portable copy of plan and stored transactions; user-selected format is either a versioned JSON bundle or a CSV ZIP (transactions plus plan/metadata CSV), each documented for portability.
- **Deletion request**: Irreversible removal of all user financial data.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 90% of users in moderated usability tests can upload a valid Monzo CSV and see imported transactions with zero unexplained silent drops (every row accounted for in success or reported error).
- **SC-002**: Users can complete first-time setup (import, choose preset, confirm at least one recurring item, view category health) in under 30 minutes with only one month of data, without requiring a fixed payday entry.
- **SC-003**: After confirming recurring items, 95% of repeated imports in test scenarios re-apply remembered assignments without manual re-entry for previously confirmed patterns.
- **SC-004**: For scripted plan scenarios, required income, limiting category, and budget balance yes/no with monetary gap match independently verified expected results in 100% of golden scenarios.
- **SC-005**: 100% of hosted deployment acceptance checks enforce authentication with passkey before data access; local-only mode acceptance checks confirm no network exposure path for account-free use.
- **SC-006**: Export followed by full deletion removes all user financial data in acceptance testing with no residual retrievable transactions or plan fields for that user.
- **SC-007**: Primary reports (category health, budget balance, confirmed recurring summary) remain readable and operable via keyboard-only navigation in accessibility test passes for core flows.
- **SC-008**: Users viewing forecasts with less than three months of history see explicit coverage labelling in 100% of test cases where shorter history drives variable recurring estimates.
- **SC-009**: After at least two weeks without using the app, test participants can upload fresh statements and reach updated category health and budget balance views within 15 minutes without re-entering previously confirmed recurring patterns (supports **G1** low-touch upkeep).

## Assumptions

- Target currency for v1 is GBP; users primarily hold UK Monzo accounts with exportable CSV history.
- Users accept that recurring detection is probabilistic; the product communicates uncertainty rather than guaranteeing completeness.
- Self-hosted operators follow published secure deployment guidance; the product provides export and deletion but does not manage their infrastructure.
- “Lazy believer” remains the primary persona; frequent daily review uses the same plan and reports without a separate product mode.
- v1 planning uses one working budget period; multi-period libraries are out of scope (**FR-X006**).
- Budgeted income may be suggested from confirmed income patterns; user confirmation or edit is required before allocations change (**FR-M031**).
- Monzo CSV column layouts remain stable enough for explicit mapping; breaking bank format changes are handled as import errors, not silent fixes.
- Passkeys are available for hosted users on at least one device; v1 provides passkey-on-supported-device guidance when the current device cannot authenticate, with no non-passkey hosted login. Broader account rescue flows (e.g. one-time keys generated at signup) are deferred post-v1.
- One-off transaction categorisation is a v1.0.0 release requirement (FR-M037); forecasts MUST NOT treat one-offs as recurring commitments.
- Trend reporting (Could) is omitted from v1 release success unless explicitly pulled into scope later.
- No third-party open banking or aggregation is assumed; statements are user-uploaded files only.
- Legal and regulatory compliance beyond stated privacy minimisation and user-controlled deletion is the operator’s responsibility; the product provides the technical means for portability and erasure.
