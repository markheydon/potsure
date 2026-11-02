---

description: "Task list for Potsure v1 implementation"
---

# Tasks: Potsure v1

**Input**: Design documents from `/specs/001-potsure-v1/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Constitution II requires golden tests for statement parsing and budgeting core, plus Playwright E2E for key journeys. Test tasks are included with those areas (not strict TDD ordering unless noted).

**Organization**: Tasks grouped by user story for independent increments; Phase 2 covers shared auth, persistence, and pure core.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1–US12) on story-phase tasks only

## Path Conventions

Per [plan.md](./plan.md): `src/Potsure.*` projects, `tests/Potsure.*.Tests`, `samples/monzo/`, `docs/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Replace placeholder solution with layered projects and tooling.

- [ ] T001 Create `src/Potsure.AppHost`, `src/Potsure.Web`, `src/Potsure.Budgeting`, `src/Potsure.Statements`, `src/Potsure.Recurrence`, `src/Potsure.Reporting`, `src/Potsure.Infrastructure` class-library/web projects per plan.md structure
- [ ] T002 Update `Potsure.slnx` to reference all new projects and retire or migrate `src/Potsure/Potsure.csproj` placeholder
- [ ] T003 Add central package versions in `Directory.Packages.props` for CsvHelper, EF Core 10 SQL Server, Aspire hosting integrations, BlazorBlueprint Components/Primitives/Icons, ASP.NET Core Identity passkeys, and Microsoft.Extensions.Http.Resilience (versions pinned per CONVENTIONS.md)
- [ ] T004 [P] Wire project references: Web → Infrastructure, Reporting; Infrastructure → Statements, Recurrence, EF; Reporting → Budgeting; Recurrence → Budgeting; Statements standalone
- [ ] T005 [P] Create test projects `tests/Potsure.Budgeting.Tests`, `tests/Potsure.Statements.Tests`, `tests/Potsure.Recurrence.Tests`, `tests/Potsure.Reporting.Tests`, `tests/Potsure.Infrastructure.Tests`, `tests/Potsure.Web.Tests`, `tests/Potsure.E2E` and add to `Potsure.slnx`
- [ ] T006 [P] Add `samples/monzo/` with README stating anonymised fixtures only; no personal exports in repo
- [ ] T007 Scaffold `src/Potsure.AppHost/Program.cs` with `AddAzureContainerAppEnvironment`, `AddAzureSqlServer`, `RunAsContainer`, `AddDatabase`, web project reference, `WithReference`, `WaitFor`, `PublishAsAzureContainerApp` per plan.md
- [ ] T008 Configure `src/Potsure.Web` for Blazor Server InteractiveServer, `net10.0`, nullable, analysers aligned with `CONVENTIONS.md`
- [ ] T009 [P] Add `docs/` placeholder structure for self-host, encryption, and deployment stages (implement content in Polish phase)

**Checkpoint**: Solution builds with empty project shells.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Pure budgeting core, persistence, auth modes, and UI shell — **blocks all user stories**.

**⚠️ CRITICAL**: No user story feature work until this phase completes.

- [ ] T010 Define shared enums and records in `src/Potsure.Budgeting/` matching data-model.md (`BudgetCategoryKind`, `AllocationPreset`, `TransactionKind`, `PatternConfirmationState`, etc.)
- [ ] T011 Implement `Money` and rounding rules (GBP 2dp at report emission; banker's rounding) in `src/Potsure.Budgeting/Money.cs` per contracts/budgeting-core.md
- [ ] T012 Implement preset allocation from budgeted income in `src/Potsure.Budgeting/AllocationCalculator.cs` (50/30/20 default, 75/15/10 alternative; category-level only)
- [ ] T013 Implement category health lines (`UnderOver` = allocation − actual activity; recorded funding excluded) in `src/Potsure.Budgeting/CategoryHealthCalculator.cs` per FR-M017
- [ ] T014 Implement required income, limiting category (tie: Needs → Wants → Savings & Debt), shortfall, yes/no conclusion in `src/Potsure.Budgeting/RequiredIncomeCalculator.cs` per contracts/budgeting-core.md and FR-M018
- [ ] T015 Implement runway comparison (`ProjectedRemainingSpend > allocation − actual activity + recorded funding`) in `src/Potsure.Budgeting/RunwayCalculator.cs` per FR-M020
- [ ] T016 [P] Add golden scenario fixtures and tests in `tests/Potsure.Budgeting.Tests/` covering contracts/budgeting-core.md minimum catalogue (8 scenarios)
- [ ] T017 Define EF entity types and configurations in `src/Potsure.Infrastructure/Persistence/` from data-model.md (`BudgetProfile`, `BudgetPeriod`, `BudgetCategory`, `BankPot`, `Transaction`, `IncomeSource`, `RecurringExpense`, `Assignment`, `StatementUpload`, `MonzoAccountAssociation`)
- [ ] T018 Add `UserId` on user-owned entities, global query filters, and ownership validation helpers in `src/Potsure.Infrastructure/Persistence/Tenancy/` per research.md
- [ ] T019 Create initial EF migration in `src/Potsure.Infrastructure/Persistence/Migrations/` and document idempotent deploy approach in `docs/deployment-database.md`
- [ ] T020 Configure ASP.NET Core Data Protection key ring persistence to SQL in `src/Potsure.Infrastructure/` reconciled with Aspire SQL resource
- [ ] T021 Implement deployment mode options (local-only vs hosted) in `src/Potsure.Web/Configuration/DeploymentModeOptions.cs` with FR-M024 network non-exposure for local-only
- [ ] T022 Configure ASP.NET Core Identity with passkey support and SQL persistence in `src/Potsure.Web/` for hosted mode (FR-M025; no password fallback v1)
- [ ] T023 Implement local-only single-user profile key path without remote account in `src/Potsure.Infrastructure/` when deployment mode is local-only
- [ ] T024 Register `AddBlazorBlueprintComponents()`, `App.razor` theme assets, `_Imports.razor`, and `BbPortalHost` in interactive root layout under `src/Potsure.Web/`
- [ ] T025 Create base interactive layout and navigation shell in `src/Potsure.Web/Components/Layout/` (UK English labels)
- [ ] T026 Implement reauthentication gate service in `src/Potsure.Web/Services/ReauthenticationService.cs` for export, deletion, and credential changes
- [ ] T027 Configure OpenTelemetry in `src/Potsure.Web/` excluding upload bodies, payee text, account refs, and export payloads per research.md
- [ ] T028 Register DI composition root in `src/Potsure.Web/Program.cs` and `src/Potsure.Infrastructure/DependencyInjection.cs`

**Checkpoint**: AppHost starts; hosted passkey or local-only path loads shell; budgeting golden tests pass in CI.

---

## Phase 3: User Story 1 — Import Monzo statements safely (Priority: P1) 🎯

**Goal**: Trustworthy Monzo CSV ingest with mapping confirmation, de-dupe, GBP-only, account validation, and transfer exclusion.

**Independent Test**: Upload valid, overlapping, and invalid files; verify de-duplication, explicit errors, account confirmation when identity is missing, and rejection of other banks or different accounts (spec US1).

### Tests for User Story 1

- [ ] T029 [P] [US1] Add anonymised golden CSV fixtures under `tests/Potsure.Statements.Tests/Fixtures/` per contracts/monzo-import.md
- [ ] T030 [P] [US1] Implement golden-file parser tests in `tests/Potsure.Statements.Tests/MonzoCsvParserTests.cs` (constitution II)

### Implementation for User Story 1

- [ ] T031 [P] [US1] Implement description normalisation for de-dupe in `src/Potsure.Statements/Deduplication/DescriptionNormalizer.cs` (trim; collapse internal whitespace to one space; strip non-alphanumeric; case-insensitive compare per FR-M004)
- [ ] T032 [P] [US1] Implement Monzo column map and CsvHelper streaming reader in `src/Potsure.Statements/MonzoCsvParser.cs` per contracts/monzo-import.md and FR-M002 (auto-detect; reject unsupported layouts)
- [ ] T033 [US1] Implement `TransactionKind` classification rules in `src/Potsure.Statements/TransactionClassifier.cs` per contracts/monzo-import.md classification table
- [ ] T034 [US1] Reject non-GBP rows with clear errors (no user override) in `src/Potsure.Statements/MonzoImportValidator.cs` per FR-M026
- [ ] T035 [US1] Implement account fingerprint validation and different-account rejection in `src/Potsure.Statements/MonzoAccountValidator.cs` per FR-M005 and FR-M006
- [ ] T036 [US1] Implement de-duplication by Monzo Transaction ID else date+amount+normalised description in `src/Potsure.Statements/TransactionDeduplicator.cs` per FR-M004
- [ ] T037 [US1] Implement upload pipeline (stream parse → validate → stage → dedupe → atomic commit; temp cleanup on all exits) in `src/Potsure.Infrastructure/Imports/StatementImportService.cs` per research.md
- [ ] T038 [US1] Persist `StatementUpload`, `ImportStatistics`, and remembered `MonzoColumnMapping` in `src/Potsure.Infrastructure/Persistence/Repositories/StatementUploadRepository.cs`
- [ ] T039 [US1] Build import wizard UI (mapping confirm/adjust, account confirm, summary with skip reasons and transfer counts) in `src/Potsure.Web/Components/Import/`
- [ ] T040 [US1] Ensure FR-M003: every skipped row reported; no silent drops in `StatementImportService` and import summary DTO

**Checkpoint**: US1 independent test passes via quickstart Scenario 1.

---

## Phase 4: User Story 11 — Local-only and hosted access modes (Priority: P1)

**Goal**: Constitution-mandated deployment paths with passkey hosted auth and non-network local-only mode.

**Independent Test**: Local-only without network account; hosted requires passkey; users isolated (spec US11).

- [ ] T041 [P] [US11] Document local-only vs hosted behaviour in `docs/self-host.md` and `docs/local-only-mode.md`
- [ ] T042 [US11] Enforce hosted passkey-only sign-in UI and guidance when passkey unavailable in `src/Potsure.Web/Components/Account/` (FR-M025)
- [ ] T043 [US11] Block financial data endpoints and pages without authentication on hosted in `src/Potsure.Web/` middleware/filters
- [ ] T044 [US11] Verify local-only mode does not bind Kestrel to public interfaces by default in `src/Potsure.Web/Program.cs` and document operator checklist in `docs/self-host.md`
- [ ] T045 [US11] Add integration tests for tenancy isolation in `tests/Potsure.Infrastructure.Tests/TenancyIsolationTests.cs`
- [ ] T102 [US11] Configure hosted encryption in transit (HTTPS/TLS ingress) and at rest (Azure SQL TDE/policy) via AppHost and web host; add verification checklist and acceptance tests in `docs/encryption.md` and `tests/Potsure.Infrastructure.Tests/HostedEncryptionAcceptanceTests.cs` (FR-M027, constitution IV)

**Checkpoint**: SC-005 acceptance paths demonstrable locally.

---

## Phase 5: User Story 3 — Choose preset and plan the period (Priority: P1)

**Goal**: Single working budget period, preset selection, user-confirmed budgeted income drives allocations only.

**Independent Test**: Switch presets; set inclusive date range; enter planned income; allocations = preset × budgeted income only (spec US3).

- [ ] T046 [P] [US3] Seed fixed three `BudgetCategory` rows on profile create in `src/Potsure.Infrastructure/Persistence/BudgetProfileSeeder.cs` (FR-M012)
- [ ] T047 [US3] Implement working period CRUD (inclusive `StartDate`/`EndDate`; single period only FR-M031) in `src/Potsure.Infrastructure/Services/BudgetPeriodService.cs`
- [ ] T048 [US3] Implement preset switch with immediate allocation recalculation from unchanged budgeted income in `src/Potsure.Infrastructure/Services/AllocationPresetService.cs` (FR-M011)
- [ ] T049 [US3] Implement optional budgeted income suggestion from confirmed income patterns without auto-apply in `src/Potsure.Infrastructure/Services/BudgetedIncomeSuggestionService.cs` (FR-M031)
- [ ] T050 [US3] Build settings UI for preset, period dates, and budgeted income confirmation in `src/Potsure.Web/Components/Plan/PlanSettings.razor`
- [ ] T051 [US3] Wire period-scoped date filtering helper (both start and end inclusive) in `src/Potsure.Reporting/PeriodScope.cs` for downstream reports
- [ ] T103 [US3] Present `ActualIncomeInPeriod` separately from `BudgetedIncome` and forecast fields in plan settings UI and read models in `src/Potsure.Reporting/PeriodSummaryBuilder.cs` and `src/Potsure.Web/Components/Plan/PlanSettings.razor` (FR-M019)

**Checkpoint**: US3 independent test; quickstart Scenario 3.

---

## Phase 6: User Story 2 — Detect and confirm recurring patterns (Priority: P1)

**Goal**: Suggest, confirm, merge, split, reject recurring income/expenses; assignment memory and auto-attribution.

**Independent Test**: Import 1–3 months; confirm/merge/split; re-upload remembers assignments (spec US2).

### Tests for User Story 2

- [ ] T052 [P] [US2] Add detection unit tests excluding internal transfers from input in `tests/Potsure.Recurrence.Tests/RecurrenceDetectionTests.cs`

### Implementation for User Story 2

- [ ] T053 [P] [US2] Implement cadence detection (weekly, fortnightly, monthly) in `src/Potsure.Recurrence/RecurrenceDetector.cs` excluding `InternalTransfer` and `PotMovement` per FR-M007
- [ ] T054 [US2] Implement variable expense monthly estimate with partial-month normalisation and coverage labels in `src/Potsure.Recurrence/VariableExpenseEstimator.cs` per FR-M034 and research.md
- [ ] T055 [US2] Persist `IncomeSource` with splits must sum to 100% validation (display-only splits) in `src/Potsure.Infrastructure/Services/IncomePatternService.cs` per FR-M010
- [ ] T056 [US2] Persist `RecurringExpense` with category required; optional pot and tag (tag free-text, no calc effect) in `src/Potsure.Infrastructure/Services/ExpensePatternService.cs` per FR-M009
- [ ] T057 [US2] Implement merge/split workflows requiring user resolution of category, optional pot, and tag before save in `src/Potsure.Infrastructure/Services/PatternMergeSplitService.cs` per FR-M008
- [ ] T058 [US2] Implement auto-attribution of matching debits to confirmed expense patterns with per-transaction override persistence in `src/Potsure.Infrastructure/Services/TransactionAttributionService.cs` per FR-M010a
- [ ] T106 [US2] Add re-import integration test asserting remembered pattern assignments and auto-attribution without manual re-entry in `tests/Potsure.Infrastructure.Tests/ReimportAssignmentMemoryTests.cs` (SC-003)
- [ ] T059 [US2] Build detection review UI (suggested vs confirmed, income vs expense) in `src/Potsure.Web/Components/Recurrence/`
- [ ] T060 [US2] Project confirmed expense commitments cadence-aware into selected period in `src/Potsure.Reporting/CommitmentProjector.cs` for FR-M018 inputs

**Checkpoint**: US2 independent test; quickstart Scenario 2.

---

## Phase 7: User Story 4 — Map bank pots or budget by category only (Priority: P2)

**Goal**: Optional pots mapped 1:1 to categories; no double-counting.

**Independent Test**: Configure pots; verify combined totals count each amount once (spec US4).

- [ ] T061 [P] [US4] Implement `BankPot` CRUD with exactly one `CategoryId` each in `src/Potsure.Infrastructure/Services/BankPotService.cs` (FR-M013; no hierarchy)
- [ ] T062 [US4] Support `PotMappingMode` UX state (none, category-named, expense-specific) in `src/Potsure.Web/Components/Plan/PotMapping.razor`
- [ ] T063 [US4] Roll pot recorded funding and allocations into category totals once in `src/Potsure.Reporting/CategoryRollup.cs` per FR-M017
- [ ] T064 [US4] Allow category-only budgeting when user chooses no pots in `src/Potsure.Web/Components/Plan/` (FR-M014)

**Checkpoint**: US4 independent test with category-named and expense-specific pot fixtures.

---

## Phase 8: User Story 5 — Record funding already set aside (Priority: P2)

**Goal**: User-declared funding separate from imports and planned future funding.

**Independent Test**: Enter funding at category and pot; imports do not alter it (spec US5).

- [ ] T065 [P] [US5] Implement recorded funding updates on category (`RecordedFundingDirect`) and pot (`RecordedFunding`) in `src/Potsure.Infrastructure/Services/RecordedFundingService.cs` (FR-M015, FR-M016)
- [ ] T066 [US5] Implement planned future funding fields separately from recorded funding in `src/Potsure.Infrastructure/Services/PlannedFundingService.cs` (Key Entity: Planned future funding; US5 acceptance — distinct from imports and recorded funding)
- [ ] T067 [US5] Build funding entry UI in `src/Potsure.Web/Components/Plan/RecordedFunding.razor` showing four measures context from FR-M017

**Checkpoint**: US5 independent test; funding unchanged after re-import.

---

## Phase 9: User Story 6 — Category health and actual spend (Priority: P1)

**Goal**: Four measures per category, uncategorised spend warning, one-offs, net refunds, over-allocation warning.

**Independent Test**: Category health shows allocation, funding, actual, under/over; orphan debits uncategorised (spec US6).

- [ ] T068 [P] [US6] Implement net attributed actual activity (debits minus refunds/credits same rules) in `src/Potsure.Reporting/ActualActivityCalculator.cs` per FR-M017
- [ ] T069 [US6] Implement uncategorised spend total excluding internal transfers unless one-off categorised in `src/Potsure.Reporting/UncategorisedSpendCalculator.cs` per FR-M017b
- [ ] T070 [US6] Implement over-allocation persistent indicator when mapped pot plus unassigned recorded funding exceeds category allocation in `src/Potsure.Reporting/OverAllocationEvaluator.cs` (allow save; show warning FR-M017a)
- [ ] T071 [US6] Implement one-off transaction categorisation (expense debits, refunds, credits) affecting actuals only in `src/Potsure.Infrastructure/Services/OneOffCategorisationService.cs` per FR-M037
- [ ] T072 [US6] Assemble `PeriodSummary` per category in `src/Potsure.Reporting/PeriodSummaryBuilder.cs` using `Potsure.Budgeting` calculators
- [ ] T073 [US6] Build category health report UI with optional pot breakdown in `src/Potsure.Web/Components/Reports/CategoryHealth.razor`
- [ ] T104 [US6] Label actual receipt income separately from budgeted income and forecasts on category health and related report headers in `src/Potsure.Web/Components/Reports/CategoryHealth.razor` (FR-M019)
- [ ] T074 [US6] Add reporting determinism test (repeat view identical totals) in `tests/Potsure.Reporting.Tests/DeterminismTests.cs` for FR-M036

**Checkpoint**: US6 independent test; quickstart Scenarios 4 and 7.

---

## Phase 10: User Story 7 — Required income, limiting category, and budget balance (Priority: P1)

**Goal**: FR-M018 report with commitments from confirmed recurring expenses only; actual receipts separate.

**Independent Test**: Shortfall shows limiting category, monetary gap, qualified yes/no (spec US7).

- [ ] T075 [US7] Wire `RequiredIncomeCalculator` to cadence-projected commitments only in `src/Potsure.Reporting/BudgetBalanceReportBuilder.cs` (exclude income patterns, one-offs, funding)
- [ ] T076 [US7] Build budget balance report UI with income timing from expected dates (not assumed four payments per month) in `src/Potsure.Web/Components/Reports/BudgetBalance.razor`
- [ ] T077 [US7] Ensure messaging does not imply future costs covered by unreceived money (FR-M033) in report copy resources under `src/Potsure.Web/Resources/`
- [ ] T105 [US7] Keep actual receipts and planned income visually and structurally separate on budget balance and runway report components in `src/Potsure.Web/Components/Reports/` (FR-M019; complements FR-M033)
- [ ] T078 [US7] Extend golden tests for budget balance scenarios in `tests/Potsure.Budgeting.Tests/BudgetBalanceGoldenTests.cs`

**Checkpoint**: US7 independent test; quickstart Scenario 5.

---

## Phase 11: User Story 8 — Category runway warnings (Priority: P2)

**Goal**: Forecast horizon (default 3 months, user override); runway vs allocation − actual + recorded funding.

**Independent Test**: Trigger shortfall; see disclosure for history gaps (spec US8).

- [ ] T079 [P] [US8] Implement forecast horizon setting on `BudgetProfile` (`ForecastHorizonMonths` default 3) in `src/Potsure.Infrastructure/Services/ForecastHorizonService.cs` per FR-M035
- [ ] T080 [US8] Compute projected remaining spend including variable estimates in `src/Potsure.Reporting/RunwayProjector.cs` per FR-M020 and FR-M034
- [ ] T081 [US8] Surface uncertainty flags for statement gaps and uncertain income in `src/Potsure.Reporting/UncertaintyFlagsBuilder.cs` per FR-M032
- [ ] T082 [US8] Build runway warnings UI with optional pot detail in `src/Potsure.Web/Components/Reports/RunwayWarnings.razor`

**Checkpoint**: US8 independent test; quickstart partial-month labelling scenario.

---

## Phase 12: User Story 9 — Confirmed recurring summary (Priority: P2)

**Goal**: Audit view of confirmed patterns; suggestions separate.

**Independent Test**: Summary shows cadence, amounts, assignments, income splits; suggestions listed apart (spec US9).

- [ ] T083 [P] [US9] Build read model query for confirmed vs suggested patterns in `src/Potsure.Infrastructure/Queries/RecurringSummaryQuery.cs`
- [ ] T084 [US9] Build recurring summary page in `src/Potsure.Web/Components/Recurrence/RecurringSummary.razor` per FR-M021

**Checkpoint**: US9 independent test after US2 data present.

---

## Phase 13: User Story 10 — Export and permanent deletion (Priority: P1)

**Goal**: User-chosen JSON bundle or CSV ZIP export; irreversible deletion after reauth.

**Independent Test**: Export both formats; delete all financial data (spec US10).

- [ ] T085 [P] [US10] Implement JSON bundle export per contracts/export-formats.md in `src/Potsure.Infrastructure/Export/JsonBundleExporter.cs`
- [ ] T086 [P] [US10] Implement CSV ZIP export (`transactions.csv`, `plan.csv`, `metadata.json`) in `src/Potsure.Infrastructure/Export/CsvZipExporter.cs`
- [ ] T087 [US10] Implement export orchestration with format choice and reauthentication in `src/Potsure.Infrastructure/Export/ExportService.cs` (FR-M022)
- [ ] T088 [US10] Implement transactional permanent deletion of all user financial data in `src/Potsure.Infrastructure/Deletion/UserDataDeletionService.cs` per FR-M023 and data-model.md Deletion section
- [ ] T089 [US10] Build export and delete UI with confirmation flows in `src/Potsure.Web/Components/Privacy/`
- [ ] T090 [US10] Add export format contract tests in `tests/Potsure.Infrastructure.Tests/ExportFormatTests.cs`

**Checkpoint**: US10 independent test; quickstart Scenario 6.

---

## Phase 14: User Story 12 — Accessible, responsive review (Priority: P3)

**Goal**: Keyboard operability and usable mobile layout for core flows.

**Independent Test**: Keyboard-only import → recurring → category health → export; narrow viewport reports (spec US12).

- [ ] T091 [P] [US12] Audit and fix focus order and keyboard activation on import wizard in `src/Potsure.Web/Components/Import/`
- [ ] T092 [P] [US12] Audit and fix keyboard navigation on recurring review and reports in `src/Potsure.Web/Components/Recurrence/` and `Components/Reports/`
- [ ] T093 [US12] Apply responsive layout adjustments for primary report content without horizontal scroll in `src/Potsure.Web/wwwroot/app.css` and component markup
- [ ] T094 [US12] Provide textual equivalents for chart data on category health and budget balance per SC-007 in report components

**Checkpoint**: quickstart Scenario 9.

---

## Phase 15: Polish & Cross-Cutting Concerns

**Purpose**: E2E, docs, security hardening, quickstart validation.

- [ ] T095 [P] Add Playwright E2E tests for import → confirm recurring → category health → export in `tests/Potsure.E2E/KeyJourneysTests.cs` (constitution II)
- [ ] T096 [P] XML documentation on public APIs in `src/Potsure.Budgeting/`, `src/Potsure.Statements/`, and `src/Potsure.Reporting/` per constitution V
- [ ] T097 Complete `docs/self-host.md`, `docs/encryption.md`, and deployment stage notes aligned with AppHost SKU/replica policy in plan.md
- [ ] T098 Review input validation and sanitisation for uploads and forms across `src/Potsure.Web/` and Infrastructure (FR-M028)
- [ ] T099 Verify logging excludes raw statements, full account numbers, and secrets (FR-M029) in `src/Potsure.Web/` and Infrastructure
- [ ] T100 Run full quickstart.md validation checklist and record results in `specs/001-potsure-v1/quickstart.md` (add “Validation log” subsection)
- [ ] T101 Final `dotnet format Potsure.slnx --verify-no-changes`, Release build `-warnaserror`, and full test suite per AGENTS.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup** → **Phase 2 Foundational** → user story phases **3–14** → **Phase 15 Polish**
- **Phase 4 (US11)** can start after T021–T023 but should complete before hosted demo of later stories
- **Phase 5 (US3)** can proceed in parallel with late US1 work once T017 profile entities exist (after T017)

### User Story Dependencies

| Story | Depends on | Notes |
|-------|------------|-------|
| US1 | Phase 2 | No other stories |
| US11 | Phase 2 T021–T023 | Foundational auth |
| US3 | Phase 2, profile entities | Independent of imports for empty plan |
| US2 | US1 transactions | Detection needs history |
| US4 | US3 categories | Pots map to categories |
| US5 | US3, optional US4 | Funding UI |
| US6 | US1, US2, US3, FR-M037 one-offs | Core lazy-believer view |
| US7 | US2 commitments, US3 preset/income | Uses Budgeting core |
| US8 | US2, US5, US6 inputs | Runway projection |
| US9 | US2 | Read-only summary |
| US10 | Persistence populated | Export/delete |
| US12 | Core UI pages exist | Polish pass |

Suggested **MVP vertical slice** (demo only, not v1.0.0): Phase 1 → 2 → US1 → US3 → US2 → US6 → US7. **v1.0.0 release gate** (constitution + spec P1): also US10, US11, FR-M037 (`T071`), hosted encryption verification (`T102`), and quickstart validation (`T100`).

### Parallel Opportunities

- Phase 1: T004, T005, T006, T009 in parallel
- Phase 2: T016 parallel with T017–T20 after T012–T015 interfaces stable
- US1: T029–T032 parallel before T037 pipeline
- US10: T085, T086 parallel
- US12: T091, T092 parallel
- After Phase 2, teams may parallelise **US4+US5** while another works **US7** once US2+US3 land

---

## Parallel Example: User Story 1

```bash
# Fixtures and parser tests together:
T029 tests/Potsure.Statements.Tests/Fixtures/
T030 tests/Potsure.Statements.Tests/MonzoCsvParserTests.cs

# Parser building blocks in parallel:
T031 src/Potsure.Statements/Deduplication/DescriptionNormalizer.cs
T032 src/Potsure.Statements/MonzoCsvParser.cs
```

---

## Implementation Strategy

### MVP First (minimum demonstrable product)

1. Complete Phase 1 and Phase 2
2. Complete Phase 3 (US1), Phase 5 (US3), Phase 6 (US 2), Phase 9 (US6), Phase 10 (US7)
3. **STOP and VALIDATE** against quickstart Scenarios 1–5 and 7 (demo checkpoint)

### v1.0.0 release (full Must scope)

Before tagging v1.0.0, also complete Phase 4 (US11, including `T102`), Phase 13 (US10), one-offs (`T071`), Phase 15 (`T100`–`T101`), and remaining P1/P2 stories as scheduled in spec.

### Incremental Delivery

Deliver P2 stories (US4, US5, US8, US9) then US12 accessibility and Phase 15 E2E/docs.

### Parallel Team Strategy

- Developer A: Statements + import UI (US1)
- Developer B: Budgeting core + reports (US3, US6, US7)
- Developer C: Recurrence + attribution (US2)
- Converge on Infrastructure and Web integration

---

## Notes

- Income split percents **must sum to 100%** on save (FR-M010); splits are **display-only** for allocations and FR-M018
- **Under/over** = allocation − actual activity; recorded funding **must not** enter under/over (FR-M017)
- De-dupe normalisation rules quoted in T031 must match FR-M004 verbatim
- Do not add Redis, Blob staging, or Key Vault in v1 unless release gates change (research.md)
- Retire placeholder `src/Potsure/` when T002 completes
- **FR-S002** (transaction-level actual-spend drill-down) is Should-level; deferred unless explicitly pulled into scope — category health (`T073`) covers aggregate actuals only
