---

description: "Task list for Potsure v1 implementation"
---

# Tasks: Potsure v1

**Plan sync**: 2026-11-09 (standing budget plan: estimated income + cadence, no plan period dates; report date range **FR-M031a**; pot mapping modes **FR-M013**–**FR-M013c**; **30.4375** proration and banker's rounding **FR-M036**; recurring edit/delete and payment anchor **FR-M008c**; fixed vs variable override **FR-M008d**). Prior sync 2026-11-04: hosted Identity pivot; 2026-11-03: Aspire SQL client, EF `AddEFMigrations`.

**Tasks gate (2026-11-09)**: Phases **1–26** reflect work through the Identity pivot and pre-pivot **BudgetPeriod** model. **Phase 27** is the primary **implementation queue** for the 2026-11-09 plan pivot; quickstart Scenarios 3, 4b, and 7 remain **Partial** until Phase 27 completes. **`[X]` on T012**–**T016**, **T074**, and **T078** is pre-pivot evidence only; **SC-004** / **FR-M036** acceptance requires **T163**–**T167** and **T184** after Phase 27. Superseded passkey tasks (**T022**, **T042**, **T132**, **T136**) are **N/A** (replaced by **T143**–**T147**); leave unchecked.

**Analyze gate (2026-11-09)**: EF model/migrations canonical project is **`Potsure.Data`** per [plan.md](./plan.md); AppHost already uses `WithMigrationsProject` → Data (**T112**/**T113** descriptions below corrected for audit traceability).

**Input**: Design documents from `/specs/001-potsure-v1/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Governance**: [constitution v1.2.0](../../.specify/memory/constitution.md) (Aspire resource consumption, central package versioning); operational steps in [AGENTS.md](../../AGENTS.md).

**Tests**: Constitution II requires golden tests for statement parsing and budgeting core, plus Playwright E2E for key journeys. Constitution 1.2.0 adds dependency hygiene (Scenario 8b), Aspire SQL client consumption smoke, and **EF migration ordering** (Scenario 8c) in quickstart local run. Test tasks are included with those areas (not strict TDD ordering unless noted). **E2E acceptance**: convergence tasks **T125**, **T128**, and **T133** remain **partial** until **T149** passes; keyboard audit via **T150** (quickstart Scenario 9). Do not treat `[X]` on partial convergence tasks alone as v1.0.0 release evidence.

**Superseded tasks**: Items marked **Superseded 2026-11-04** and left **unchecked** (**T022**, **T042**, **T132**, **T136**) are not v1.0.0 acceptance evidence; implement **Phase 23** (**T143**–**T148**), **T147**, and release evidence **T149**–**T151** instead.

**Organization**: Tasks grouped by user story for independent increments; Phase 2 covers shared auth, persistence, and pure core.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1–US12) on story-phase tasks only
- **Checkbox vs release**: `[X]` means work landed in the repo; **(partial)** in the description or release-gate notes means v1.0.0 acceptance evidence may still be open until **T149**–**T150** (and **T148** for hosted Identity) complete

## Path Conventions

Per [plan.md](./plan.md): `src/Potsure.*` projects, `tests/Potsure.*.Tests`, `samples/monzo/`, `docs/`.

**Task ID notes**: IDs **T107**–**T108**, **T102**–**T106**, and **T110**–**T116** were appended after the initial T001–T101 pass; document order follows phases, not numeric order. **T109** is intentionally unused (reserved).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Replace placeholder solution with layered projects and tooling.

- [X] T001 Create `src/Potsure.AppHost`, `src/Potsure.Web`, `src/Potsure.Budgeting`, `src/Potsure.Statements`, `src/Potsure.Recurrence`, `src/Potsure.Reporting`, `src/Potsure.Infrastructure` class-library/web projects per plan.md structure
- [X] T002 Update `Potsure.slnx` to reference all new projects and retire or migrate `src/Potsure/Potsure.csproj` placeholder
- [X] T003 Add central package versions in `Directory.Packages.props` for CsvHelper, EF Core 10 SQL Server, Aspire hosting integrations, BlazorBlueprint Components/Primitives/Icons, ASP.NET Core Identity passkeys package (optional/deferred  -  not v1.0.0 acceptance-critical), and Microsoft.Extensions.Http.Resilience (latest stable at introduction; constitution 1.2.0 + AGENTS.md outdated sweep before dependency work complete)
- [X] T004 [P] Wire project references: Web → Infrastructure, Reporting; Infrastructure → Statements, Recurrence, Data; Reporting → Budgeting; Recurrence → Budgeting; Statements → Budgeting
- [X] T005 [P] Create test projects `tests/Potsure.Budgeting.Tests`, `tests/Potsure.Statements.Tests`, `tests/Potsure.Recurrence.Tests`, `tests/Potsure.Reporting.Tests`, `tests/Potsure.Infrastructure.Tests`, `tests/Potsure.Web.Tests`, `tests/Potsure.E2E` and add to `Potsure.slnx`
- [X] T006 [P] Add `samples/monzo/` with README stating anonymised fixtures only; no personal exports in repo
- [X] T007 Scaffold `src/Potsure.AppHost/AppHost.cs` with `AddAzureContainerAppEnvironment`, `AddAzureSqlServer`, `RunAsContainer`, `AddDatabase("potsure")`, web project reference, `WithReference`, `WaitFor`, `PublishAsAzureContainerApp` per plan.md (database reference name must match web `AddSqlServerDbContext` client name). **Superseded for schema readiness** by **T113** (plan 2026-11-03): add `AddEFMigrations` and web `WaitForCompletion(migrations)`  -  do not treat database `WaitFor` alone as sufficient after T113 lands
- [X] T008 Configure `src/Potsure.Web` for Blazor Server InteractiveServer, `net10.0`, nullable, analysers aligned with `CONVENTIONS.md`
- [X] T009 [P] Add `docs/` placeholder structure for self-host, encryption, and deployment stages (implement content in Polish phase)

**Checkpoint**: Solution builds with empty project shells.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Pure budgeting core, persistence, auth modes, and UI shell  -  **blocks all user stories**.

**⚠️ CRITICAL**: No user story feature work until this phase completes.

- [X] T010 Define shared enums and records in `src/Potsure.Budgeting/` matching data-model.md (`BudgetCategoryKind`, `AllocationPreset`, `TransactionKind`, `PatternConfirmationState`, etc.)
- [X] T011 Implement `Money` and rounding rules (GBP 2dp at report emission; banker's rounding) in `src/Potsure.Budgeting/Money.cs` per contracts/budgeting-core.md
- [X] T012 Implement preset allocation from budgeted income in `src/Potsure.Budgeting/AllocationCalculator.cs` (50/30/20 default, 75/15/10 alternative; category-level only)
- [X] T013 Implement category health lines (`UnderOver` = allocation − actual activity; recorded funding excluded) in `src/Potsure.Budgeting/CategoryHealthCalculator.cs` per FR-M017
- [X] T014 Implement required income, limiting category (tie: Needs → Wants → Savings & Debt), shortfall, yes/no conclusion in `src/Potsure.Budgeting/RequiredIncomeCalculator.cs` per contracts/budgeting-core.md and FR-M018
- [X] T015 Implement runway comparison (`ProjectedRemainingSpend > allocation − actual activity + recorded funding`) in `src/Potsure.Budgeting/RunwayCalculator.cs` per FR-M020
- [X] T016 [P] Add golden scenario fixtures and tests in `tests/Potsure.Budgeting.Tests/` covering contracts/budgeting-core.md minimum catalogue (8 scenarios)
- [X] T017 Define EF entity types and configurations in `src/Potsure.Data/` from data-model.md (`BudgetProfile`, `BudgetPeriod`, `BudgetCategory`, `BankPot`, `Transaction`, `IncomeSource`, `RecurringExpense`, `Assignment`, `StatementUpload`, `MonzoAccountAssociation`)
- [X] T018 Add `UserId` on user-owned entities, global query filters in `src/Potsure.Data/` and ownership validation helpers in `src/Potsure.Infrastructure/Tenancy/` per research.md
- [X] T019 Create initial EF migration in `src/Potsure.Data/Migrations/` and document idempotent deploy approach in `docs/deployment-database.md`
- [X] T020 Configure ASP.NET Core Data Protection key ring persistence to SQL in `src/Potsure.Infrastructure/` reconciled with Aspire SQL resource
- [X] T021 Implement deployment mode options (local-only vs hosted) in `src/Potsure.Web/Configuration/DeploymentModeOptions.cs` with FR-M024 network non-exposure for local-only
- [ ] T022 **N/A (superseded 2026-11-04)** Passkey-primary Identity  -  **replaced by T143**–**T146**; do not implement
- [X] T023 Implement local-only single-user profile key path without remote account in `src/Potsure.Infrastructure/` when deployment mode is local-only
- [X] T024 Register `AddBlazorBlueprintComponents()`, `App.razor` theme assets, `_Imports.razor`, and `BbPortalHost` in interactive root layout under `src/Potsure.Web/`
- [X] T025 Create base interactive layout and navigation shell in `src/Potsure.Web/Components/Layout/` (UK English labels)
- [X] T026 Implement reauthentication gate service in `src/Potsure.Web/Services/ReauthenticationService.cs` for export, deletion, and credential changes
- [X] T027 Configure OpenTelemetry in `src/Potsure.Web/` excluding upload bodies, payee text, account refs, and export payloads per research.md
- [X] T028 Register DI composition root in `src/Potsure.Web/Program.cs` and `src/Potsure.Infrastructure/DependencyInjection.cs`
- [X] T107 [P] Register runtime SQL via `builder.AddSqlServerDbContext<PotsureDbContext>("potsure")` in `src/Potsure.Web/Program.cs` aligned with AppHost `AddDatabase("potsure")`; no runtime `UseSqlServer` or hand-authored connection strings in web startup (constitution 1.2.0, plan.md AppHost resource model)
- [X] T108 [P] Restrict `UseSqlServer(connectionString)` to `src/Potsure.Data/DesignTime/` for EF design-time migrations only; document fallback connection for local `dotnet ef` in `docs/deployment-database.md`

### EF migration orchestration (plan amend 2026-11-03)

- [X] T111 Add `Aspire.Hosting.EntityFrameworkCore` to central `Directory.Packages.props` (13.6.4, aligned with existing Aspire packages) and reference from `src/Potsure.AppHost/Potsure.AppHost.csproj`
- [X] T112 Add AppHost project reference to `src/Potsure.Data/Potsure.Data.csproj` for `WithMigrationsProject` (no Data → AppHost reference); historical note: early drafts referenced Infrastructure
- [X] T113 Wire `AddEFMigrations` on the web resource in `src/Potsure.AppHost/AppHost.cs`: `WithReference(database)`, `WaitFor(database)`, `WithMigrationsProject` → `src/Potsure.Data/Potsure.Data.csproj`, explicit `PotsureDbContext` in `Potsure.Data`, `RunDatabaseUpdateOnStart()` for local AppHost only; web keeps `WithReference(database)` and uses `WaitForCompletion(migrations)`; confirm no `Database.MigrateAsync()` in `src/Potsure.Web/Program.cs`; confirm `BudgetProfileSeeder` is not invoked from the migration resource (plan.md EF table; **T158** tracks Aspire ef-tool defects)
- [X] T114 [P] Update `docs/deployment-database.md` and `docs/self-host.md` to distinguish local Aspire auto-apply vs hosted controlled migration job; document unchanged `dotnet ef migrations add` / `database update` commands (`--project src/Potsure.Data/Potsure.Data.csproj`, `--startup-project` Web); errata: legacy wording said Infrastructure  -  corrected by **T186**; note migration resource hidden in Aspire dashboard by default (`aspire describe --include-hidden`)
- [X] T116 [P] Wire hosted publish migration artefact path (`PublishAsMigrationScript()` and/or documented migration bundle) on AppHost publish model in `src/Potsure.AppHost/AppHost.cs` and single-runner apply steps in `docs/deployment-database.md` (no concurrent migrate on all replicas; `RunDatabaseUpdateOnStart` local-only) per plan.md hosted publish bullet

**Checkpoint**: AppHost starts; migrations complete before web; web resolves SQL via Aspire client DI; **local-only** shell loads; hosted mode blocks anonymous financial access (middleware). **Hosted email/password Identity UI** completes in **Phase 23** (**T143**–**T148**). Budgeting golden tests pass in CI.

---

## Phase 3: User Story 1  -  Import Monzo statements safely (Priority: P1) 🎯

**Goal**: Trustworthy Monzo CSV ingest with mapping confirmation, de-dupe, GBP-only, account validation, and transfer exclusion.

**Independent Test**: Upload valid, overlapping, and invalid files; verify de-duplication, explicit errors, account confirmation when identity is missing, and rejection of other banks or different accounts (spec US1).

### Tests for User Story 1

- [X] T029 [P] [US1] Add anonymised golden CSV fixtures under `tests/Potsure.Statements.Tests/Fixtures/` per contracts/monzo-import.md
- [X] T030 [P] [US1] Implement golden-file parser tests in `tests/Potsure.Statements.Tests/MonzoCsvParserTests.cs` (constitution II)

### Implementation for User Story 1

- [X] T031 [P] [US1] Implement description normalisation for de-dupe in `src/Potsure.Statements/Deduplication/DescriptionNormalizer.cs` (trim; collapse internal whitespace to one space; strip non-alphanumeric; case-insensitive compare per FR-M004)
- [X] T032 [P] [US1] Implement Monzo column map and CsvHelper streaming reader in `src/Potsure.Statements/MonzoCsvParser.cs` per contracts/monzo-import.md and FR-M002 (auto-detect; reject unsupported layouts)
- [X] T033 [US1] Implement `TransactionKind` classification rules in `src/Potsure.Statements/TransactionClassifier.cs` per contracts/monzo-import.md classification table
- [X] T034 [US1] Reject non-GBP rows with clear errors (no user override) in `src/Potsure.Statements/MonzoImportValidator.cs` per FR-M026
- [X] T035 [US1] Implement account fingerprint validation and different-account rejection in `src/Potsure.Statements/MonzoAccountValidator.cs` per FR-M005 and FR-M006
- [X] T036 [US1] Implement de-duplication by Monzo Transaction ID else date+amount+normalised description in `src/Potsure.Statements/TransactionDeduplicator.cs` per FR-M004
- [X] T037 [US1] Implement upload pipeline (stream parse → validate → stage → dedupe → atomic commit; temp cleanup on all exits) in `src/Potsure.Infrastructure/Features/Import/StatementImportService.cs` per research.md
- [X] T038 [US1] Persist `StatementUpload`, `ImportStatistics`, and remembered `MonzoColumnMapping` in `src/Potsure.Infrastructure/Persistence/Repositories/StatementUploadRepository.cs`
- [X] T039 [US1] Build import wizard UI (mapping confirm/adjust, account confirm, summary with skip reasons and transfer counts) in `src/Potsure.Web/Features/Import/Pages/`
- [X] T040 [US1] Ensure FR-M003: every skipped row reported; no silent drops in `StatementImportService` and import summary DTO

**Checkpoint**: US1 independent test passes via quickstart Scenario 1.

---

## Phase 4: User Story 11  -  Local-only and hosted access modes (Priority: P1)

**Goal**: Constitution-mandated deployment paths with hosted Identity (email/password) and non-network local-only mode.

**Independent Test**: Local-only without network account; hosted requires Identity sign-in (passkeys not required); users isolated (spec US11). **Pivot**: passkey-only implementation from **T042** superseded  -  execute **Phase 23** (**T143**–**T148**).

- [X] T041 [P] [US11] Document local-only vs hosted behaviour in `docs/self-host.md` and `docs/local-only-mode.md`
- [ ] T042 **N/A (superseded 2026-11-04)** Passkey-only sign-in UI  -  **replaced by T143** / **T145**; do not implement
- [X] T043 [US11] Block financial data endpoints and pages without authentication on hosted in `src/Potsure.Web/` middleware/filters
- [X] T044 [US11] Verify local-only mode does not bind Kestrel to public interfaces by default in `src/Potsure.Web/Program.cs` and document operator checklist in `docs/self-host.md`
- [X] T045 [US11] Add integration tests for tenancy isolation in `tests/Potsure.Infrastructure.Tests/TenancyIsolationTests.cs`
- [X] T102 [US11] Configure hosted encryption in transit (HTTPS/TLS ingress) and at rest (Azure SQL TDE/policy) via AppHost and web host; add verification checklist and acceptance tests in `docs/encryption.md` and `tests/Potsure.Infrastructure.Tests/HostedEncryptionAcceptanceTests.cs` (FR-M027, constitution IV)

**Checkpoint**: Tenancy isolation and encryption docs (`T102`); hosted middleware blocks anonymous financial access (`T043`). **US11 / SC-005 hosted acceptance is blocked** until **Phase 23** (**T143**–**T148**)  -  superseded passkey tasks **T042** do not satisfy FR-M025.

---

## Phase 5: User Story 3  -  Choose preset and plan the period (Priority: P1)

**Legacy note (2026-11-09)**: Implemented against **`BudgetPeriod`** / budgeted income on a working period. Spec and plan now require a **standing plan** (estimated income + `PlanIncomeCadence` on `BudgetProfile`, **no** saved plan start/end) and a separate **report date range** (**FR-M031a**). Do not extend this phase; execute **Phase 27** for US3 acceptance.

**Goal (historical)**: Single working budget period, preset selection, user-confirmed budgeted income drives allocations only.

**Independent Test (target, post Phase 27)**: Switch presets; save estimated income and cadence; pick report date range; allocations = preset × monthly-equivalent estimated income; incomplete income shows **not set** (spec US3).

- [X] T046 [P] [US3] Seed fixed three `BudgetCategory` rows on profile create in `src/Potsure.Infrastructure/Persistence/BudgetProfileSeeder.cs` (FR-M012)
- [X] T047 [US3] Implement working period CRUD (inclusive `StartDate`/`EndDate`; single period only FR-M031) in `src/Potsure.Infrastructure/Features/Plan/BudgetPeriodService.cs`
- [X] T048 [US3] Implement preset switch with immediate allocation recalculation from unchanged budgeted income in `src/Potsure.Infrastructure/Features/Plan/AllocationPresetService.cs` (FR-M011)
- [X] T049 [US3] Implement optional budgeted income suggestion from confirmed income patterns without auto-apply in `src/Potsure.Infrastructure/Features/Plan/BudgetedIncomeSuggestionService.cs` (FR-M031)
- [X] T050 [US3] Build settings UI for preset, period dates, and budgeted income confirmation in `src/Potsure.Web/Features/Plan/Pages/PlanSettings.razor`
- [X] T051 [US3] Wire period-scoped date filtering helper (both start and end inclusive) in `src/Potsure.Reporting/PeriodScope.cs` for downstream reports
- [X] T103 [US3] Present `ActualIncomeInPeriod` separately from `BudgetedIncome` and forecast fields in plan settings UI and read models in `src/Potsure.Reporting/PeriodSummaryBuilder.cs` and `src/Potsure.Web/Features/Plan/Pages/PlanSettings.razor` (FR-M019)

**Checkpoint**: US3 independent test; quickstart Scenario 3.

---

## Phase 6: User Story 2  -  Detect and confirm recurring patterns (Priority: P1)

**Goal**: Suggest, confirm, merge, split, reject recurring income/expenses; assignment memory and auto-attribution.

**Independent Test**: Import 1–3 months; confirm/merge/split; re-upload remembers assignments (spec US2).

### Tests for User Story 2

- [X] T052 [P] [US2] Add detection unit tests excluding internal transfers from input in `tests/Potsure.Recurrence.Tests/RecurrenceDetectionTests.cs`

### Implementation for User Story 2

- [X] T053 [P] [US2] Implement cadence detection (weekly, fortnightly, monthly) in `src/Potsure.Recurrence/RecurrenceDetector.cs` excluding `InternalTransfer` and `PotMovement` per FR-M007
- [X] T054 [US2] Implement variable expense monthly estimate with partial-month normalisation and coverage labels in `src/Potsure.Recurrence/VariableExpenseEstimator.cs` per FR-M034 and research.md
- [X] T055 [US2] Persist `IncomeSource` with splits must sum to 100% validation (display-only splits) in `src/Potsure.Infrastructure/Features/Recurrence/IncomePatternService.cs` per FR-M010
- [X] T056 [US2] Persist `RecurringExpense` with category required; optional pot and tag (tag free-text, no calc effect) in `src/Potsure.Infrastructure/Features/Recurrence/ExpensePatternService.cs` per FR-M009
- [X] T057 [US2] Implement merge/split workflows requiring user resolution of category, optional pot, and tag before save in `src/Potsure.Infrastructure/Features/Recurrence/PatternMergeSplitService.cs` per FR-M008
- [X] T058 [US2] Implement auto-attribution of matching debits to confirmed expense patterns with per-transaction override persistence in `src/Potsure.Infrastructure/Features/Reports/TransactionAttributionService.cs` per **FR-M010a** and income credit attribution per **FR-M010b** (extend in **T137** for transaction-level overrides)
- [X] T106 [US2] Add re-import integration test asserting remembered pattern assignments and auto-attribution without manual re-entry in `tests/Potsure.Infrastructure.Tests/ReimportAssignmentMemoryTests.cs` (SC-003)
- [X] T059 [US2] Build detection review UI (suggested vs confirmed, income vs expense) in `src/Potsure.Web/Features/Recurrence/Pages/`
- [X] T117 [P] [US2] Add UK English onboarding copy that three months of statement history is recommended for automatic recurring detection (**FR-S004**) in `src/Potsure.Web/Features/Recurrence/Pages/` and/or import completion summary without blocking one-month onboarding
- [X] T060 [US2] Project confirmed expense commitments cadence-aware into selected period in `src/Potsure.Reporting/CommitmentProjector.cs` for FR-M018 inputs

**Checkpoint**: US2 independent test; quickstart Scenario 2.

---

## Phase 7: User Story 4  -  Map bank pots or budget by category only (Priority: P2)

**Goal**: Optional pots mapped 1:1 to categories; no double-counting.

**Independent Test**: Configure pots; verify combined totals count each amount once (spec US4).

- [X] T061 [P] [US4] Implement `BankPot` CRUD with exactly one `CategoryId` each in `src/Potsure.Infrastructure/Features/Plan/BankPotService.cs` (FR-M013; no hierarchy)
- [X] T062 [US4] Support `PotMappingMode` UX state (none, category-named, expense-specific) in `src/Potsure.Web/Features/Plan/Pages/PotMapping.razor`
- [X] T063 [US4] Roll pot recorded funding and allocations into category totals once in `src/Potsure.Reporting/CategoryRollup.cs` per FR-M017
- [X] T064 [US4] Allow category-only budgeting when user chooses no pots in `src/Potsure.Web/Features/Plan/Pages/` (FR-M014)

**Checkpoint**: US4 independent test with category-named and expense-specific pot fixtures.

---

## Phase 8: User Story 5  -  Record funding already set aside (Priority: P2)

**Goal**: User-declared funding separate from imports and planned future funding.

**Independent Test**: Enter funding at category and pot; imports do not alter it (spec US5).

- [X] T065 [P] [US5] Implement recorded funding updates on category (`RecordedFundingDirect`) and pot (`RecordedFunding`) in `src/Potsure.Infrastructure/Features/Plan/RecordedFundingService.cs` (FR-M015, FR-M016)
- [X] T066 [US5] Implement planned future funding fields separately from recorded funding in `src/Potsure.Infrastructure/Features/Plan/PlannedFundingService.cs` (Key Entity: Planned future funding; US5 acceptance  -  distinct from imports and recorded funding)
- [X] T067 [US5] Build funding entry UI in `src/Potsure.Web/Features/Plan/Pages/RecordedFunding.razor` showing four measures context from FR-M017

**Checkpoint**: US5 independent test; funding unchanged after re-import.

---

## Phase 9: User Story 6  -  Category health and actual spend (Priority: P1)

**Goal**: Four measures per category, uncategorised spend warning, one-offs, net refunds, over-allocation warning.

**Independent Test**: Category health shows allocation, funding, actual, under/over; orphan debits uncategorised (spec US6).

- [X] T068 [P] [US6] Implement net attributed actual activity (debits minus refunds/credits same rules) in `src/Potsure.Reporting/ActualActivityCalculator.cs` per FR-M017
- [X] T069 [US6] Implement uncategorised spend total excluding internal transfers unless one-off categorised in `src/Potsure.Reporting/UncategorisedSpendCalculator.cs` per FR-M017b
- [X] T070 [US6] Implement over-allocation persistent indicator when mapped pot plus unassigned recorded funding exceeds category allocation in `src/Potsure.Reporting/OverAllocationEvaluator.cs` (allow save; show warning FR-M017a)
- [X] T071 [US6] Implement one-off transaction categorisation (expense debits, refunds, credits) affecting actuals only in `src/Potsure.Infrastructure/Features/Reports/OneOffCategorisationService.cs` per FR-M037
- [X] T072 [US6] Assemble `PeriodSummary` per category in `src/Potsure.Reporting/PeriodSummaryBuilder.cs` using `Potsure.Budgeting` calculators
- [X] T073 [US6] Build category health report UI with optional pot breakdown in `src/Potsure.Web/Features/Reports/Pages/CategoryHealth.razor`
- [X] T104 [US6] Label actual receipt income separately from budgeted income and forecasts on category health and related report headers in `src/Potsure.Web/Features/Reports/Pages/CategoryHealth.razor` (FR-M019)
- [X] T074 [US6] Add reporting determinism test (repeat view identical totals) in `tests/Potsure.Reporting.Tests/DeterminismTests.cs` for FR-M036

**Checkpoint**: US6 independent test; quickstart Scenarios 4 and 7.

---

## Phase 10: User Story 7  -  Required income, limiting category, and budget balance (Priority: P1)

**Goal**: FR-M018 report with commitments from confirmed recurring expenses only; actual receipts separate.

**Independent Test**: Shortfall shows limiting category, monetary gap, qualified yes/no (spec US7).

- [X] T075 [US7] Wire `RequiredIncomeCalculator` to cadence-projected commitments only in `src/Potsure.Reporting/BudgetBalanceReportBuilder.cs` (exclude income patterns, one-offs, funding)
- [X] T076 [US7] Build budget balance report UI with income timing from expected dates (not assumed four payments per month) in `src/Potsure.Web/Features/Reports/Pages/BudgetBalance.razor`
- [X] T077 [US7] Ensure messaging does not imply future costs covered by unreceived money (FR-M033) in report copy resources under `src/Potsure.Web/Resources/`
- [X] T105 [US7] Keep actual receipts and planned income visually and structurally separate on budget balance and runway report components in `src/Potsure.Web/Features/Reports/Pages/` (FR-M019; complements FR-M033)
- [X] T078 [US7] Extend golden tests for budget balance scenarios in `tests/Potsure.Budgeting.Tests/BudgetBalanceGoldenTests.cs`

**Checkpoint**: US7 independent test; quickstart Scenario 5.

---

## Phase 11: User Story 8  -  Category runway warnings (Priority: P2)

**Goal**: Forecast horizon (default 3 months, user override); runway vs allocation − actual + recorded funding.

**Independent Test**: Trigger shortfall; see disclosure for history gaps (spec US8).

- [X] T079 [P] [US8] Implement forecast horizon setting on `BudgetProfile` (`ForecastHorizonMonths` default 3) in `src/Potsure.Infrastructure/Features/Plan/ForecastHorizonService.cs` per FR-M035 (Must: default 3 months and user override)
- [X] T080 [US8] Compute projected remaining spend including variable estimates in `src/Potsure.Reporting/RunwayProjector.cs` per FR-M020 and FR-M034
- [X] T081 [US8] Surface uncertainty flags for statement gaps and uncertain income in `src/Potsure.Reporting/UncertaintyFlagsBuilder.cs` per FR-M032
- [X] T082 [US8] Build runway warnings UI with optional pot detail in `src/Potsure.Web/Features/Reports/Pages/RunwayWarnings.razor`

**Checkpoint**: US8 independent test; quickstart partial-month labelling scenario.

---

## Phase 12: User Story 9  -  Confirmed recurring summary (Priority: P2)

**Goal**: Audit view of confirmed patterns; suggestions separate.

**Independent Test**: Summary shows cadence, amounts, assignments, income splits; suggestions listed apart (spec US9).

- [X] T083 [P] [US9] Build read model query for confirmed vs suggested patterns in `src/Potsure.Infrastructure/Features/Recurrence/RecurringSummaryQuery.cs`
- [X] T084 [US9] Build recurring summary page in `src/Potsure.Web/Features/Recurrence/Pages/RecurringSummary.razor` per FR-M021

**Checkpoint**: US9 independent test after US2 data present.

---

## Phase 13: User Story 10  -  Export and permanent deletion (Priority: P1)

**Goal**: User-chosen JSON bundle or CSV ZIP export; irreversible deletion after reauth.

**Independent Test**: Export both formats; delete all financial data (spec US10).

- [X] T085 [P] [US10] Implement JSON bundle export per contracts/export-formats.md in `src/Potsure.Infrastructure/Features/Export/JsonBundleExporter.cs`
- [X] T086 [P] [US10] Implement CSV ZIP export (`transactions.csv`, `plan.csv`, `metadata.json`) in `src/Potsure.Infrastructure/Features/Export/CsvZipExporter.cs`
- [X] T087 [US10] Implement export orchestration with format choice and reauthentication in `src/Potsure.Infrastructure/Features/Export/ExportService.cs` (FR-M022)
- [X] T088 [US10] Implement transactional permanent deletion of all user financial data in `src/Potsure.Infrastructure/Deletion/UserDataDeletionService.cs` per FR-M023 and data-model.md Deletion section
- [X] T089 [US10] Build export and delete UI with confirmation flows in `src/Potsure.Web/Features/Privacy/Pages/`
- [X] T090 [US10] Add export format contract tests in `tests/Potsure.Infrastructure.Tests/ExportFormatTests.cs`

**Checkpoint**: US10 independent test; quickstart Scenario 6.

---

## Phase 14: User Story 12  -  Accessible, responsive review (Priority: P3)

**Goal**: Keyboard operability and usable mobile layout for core flows.

**Independent Test**: Keyboard-only import → recurring → category health → export; narrow viewport reports (spec US12).

- [X] T091 [P] [US12] Audit and fix focus order and keyboard activation on import wizard in `src/Potsure.Web/Features/Import/Pages/`
- [X] T092 [P] [US12] Audit and fix keyboard navigation on recurring review and reports in `src/Potsure.Web/Features/Recurrence/Pages/` and `Features/Reports/Pages/`
- [X] T093 [US12] Apply responsive layout adjustments for primary report content without horizontal scroll in `src/Potsure.Web/wwwroot/app.css` and component markup
- [X] T094 [US12] Provide textual equivalents for chart data on category health and budget balance per SC-007 in report components

**Checkpoint**: quickstart Scenario 9.

---

## Phase 15: Polish & Cross-Cutting Concerns

**Purpose**: E2E, docs, security hardening, quickstart validation.

- [X] T095 [P] Add Playwright E2E tests for import → confirm recurring → category health → export in `tests/Potsure.E2E/KeyJourneysTests.cs` (constitution II)
- [X] T096 [P] XML documentation on public APIs in `src/Potsure.Budgeting/`, `src/Potsure.Statements/`, and `src/Potsure.Reporting/` per constitution V
- [X] T097 Complete `docs/self-host.md`, `docs/encryption.md`, and deployment stage notes aligned with AppHost SKU/replica policy in plan.md
- [X] T098 Review input validation and sanitisation for uploads and forms across `src/Potsure.Web/` and Infrastructure (FR-M028)
- [X] T099 Verify logging excludes raw statements, full account numbers, and secrets (FR-M029) in `src/Potsure.Web/` and Infrastructure
- [X] T100 Record quickstart.md validation log for scenarios exercised to date in `specs/001-potsure-v1/quickstart.md` (manual journeys may remain **Partial** until release QA). **Scenario 8b** (dependency hygiene): satisfied when **T110** last ran; re-run **T110** after **T111** adds packages. **Scenario 8c** (EF migration ordering): out of scope for T100  -  close via **T115** after **T113**
- [X] T115 Re-run quickstart Scenario 8c (migration ordering: database healthy → migrations finished → web healthy) and refresh validation log in `specs/001-potsure-v1/quickstart.md` after T113; optionally verify hidden migration resource via `aspire describe --include-hidden`
- [X] T118 [P] Add reporting or integration test asserting variable recurring estimates expose coverage labelling when history is shorter than three months (SC-008) in `tests/Potsure.Recurrence.Tests/` or `tests/Potsure.Reporting.Tests/`; spot-check quickstart “Partial-month labelling” scenario
- [X] T110 Run `dotnet package list --outdated`, upgrade compatible versions in `Directory.Packages.props` where needed, then `dotnet test Potsure.slnx -c Release` (full suite, not `--no-build` only) after any package changes per constitution 1.2.0 and AGENTS.md
- [X] T101 Final `dotnet format Potsure.slnx --verify-no-changes`, Release build `-warnaserror`, and `dotnet test Potsure.slnx -c Release --no-build` per AGENTS.md PR workflow (re-run full test without `--no-build` when `Directory.Packages.props` changed since last build)

### Release evidence (constitution II / SC-007)

- [X] T149 [P] Close Playwright E2E release evidence: `tests/Potsure.E2E/KeyJourneysTests.cs` exercises CSV upload, recurring confirmation, and export (not page-heading smoke only) when `POTSURE_E2E_BASE_URL` is set; supersedes partial closure notes on **T125**, **T128**, and **T133** for v1.0.0
- [ ] T150 [US12] Run quickstart Scenario 9 keyboard-only audit on category health, budget balance, and recurring summary; update validation log in `specs/001-potsure-v1/quickstart.md` (**SC-007**)
- [X] T151 [P] Close **SC-003** release evidence: overlapping re-import scenarios (US2 AC7 / quickstart Scenario 2) demonstrate assignment memory without manual re-entry; non-partial integration coverage in **T106** / **T140**–**T142** or documented equivalent (partial  -  `ReimportAssignmentMemoryTests` is release evidence; quickstart Scenario 2 log refresh in **T185**)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup** → **Phase 2 Foundational** → user story phases **3–14** → **Phase 15 Polish** → convergence **Phases 16–26** → **Phase 27 (2026-11-09 pivot)** before v1.0.0 release sign-off
- **Phase 4 (US11)** can start after T021–T023 but should complete before hosted demo of later stories; **Phase 23 (T143–T148)** must complete for constitution 1.2.0 hosted acceptance after the Identity pivot
- **Phase 5 (US3)** can proceed in parallel with late US1 work once T017 profile entities exist (after T017)

### User Story Dependencies

| Story | Depends on | Notes |
|-------|------------|-------|
| US1 | Phase 2 | No other stories |
| US11 | Phase 2 T021–T023; **Phase 23** for pivot | Foundational auth; **T143–T148** supersede passkey-only **T022** / **T042** |
| US3 | Phase 2, profile entities; **Phase 27** for spec acceptance | Legacy Phase 5 uses `BudgetPeriod`; standing plan requires **T159**–**T172** |
| US2 | US1 transactions | Detection needs history |
| US4 | US3 categories; **Phase 27** **T177**–**T179** for **FR-M013** modes | Legacy pot UX partial |
| US5 | US3, optional US4 | Funding UI |
| US6 | US1, US2, US3, **T168**–**T170** report range, **T163**–**T166** proration | Core lazy-believer view |
| US7 | US2 commitments, US3 standing income, **T182** max-of-ratios | Uses Budgeting core |
| US8 | US2, US5, US6 inputs, **T170** runway window | Runway projection |
| US9 | US2 | Read-only summary |
| US10 | Persistence populated | Export/delete |
| US12 | Core UI pages exist | Polish pass |

Suggested **MVP vertical slice** (demo only, not v1.0.0): Phase 1 → 2 → US1 → **Phase 27** (standing plan + report range + proration) → US2 → US6 → US7. **v1.0.0 release gate** (constitution v1.2.0 + spec P1): **Phase 27** complete (**T159**–**T186**), US10, US11 (**Phase 23**), FR-M037 (`T071`), hosted encryption (`T102`), Aspire SQL (`T107`–`T108`), EF migration orchestration (`T111`–`T116`, **`T158`** Scenario 8c **Pass**), dependency hygiene (`T110`), Playwright E2E (`T149`), **SC-003** (`T151`), **T155** hosted HTTP Identity smoke, quickstart (`T100`, **T150**, **T185**), and pot modes Scenario 4b.

### Parallel Opportunities

- Phase 1: T004, T005, T006, T009 in parallel
- Phase 2: T016 parallel with T017–T20 after T012–T015 interfaces stable
- US1: T029–T032 parallel before T037 pipeline
- US10: T085, T086 parallel
- US12: T091, T092 parallel
- After Phase 2, teams may parallelise **US4+US5** while another works **US7** once US2+US3 land
- **Phase 23**: **T143** and **T146** touch Identity registration and pages  -  sequence before **T144**/**T145**; **T148** can run in parallel once **T143**–**T147** land

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

Before tagging v1.0.0, complete **Phase 27** (**T159**–**T186**, standing plan pivot), Phase 4 (`T102`, `T043`–`T045`), **Phase 23** (`T143`–`T148`), Phase 13 (US10; **T147**), one-offs (`T071`), Aspire + migrations (`T107`–`T116`, **`T158`**, **`T186`**), US2 **FR-S004** (`T117`), Phase 15 (`T100`–`T101`, `T110`, `T115`, `T118`, **T149**–**T151**, **`T155`**), and remaining P1/P2 stories.

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
- AppHost is the source of truth for SQL configuration; use `aspire` / `aspireify` skills when changing AppHost wiring (AGENTS.md)
- EF schema apply: AppHost `AddEFMigrations` + `WaitForCompletion` on web  -  not `MigrateAsync` at web startup; app seeding (`BudgetProfileSeeder`) stays in web/runtime, not the migration job (plan.md EF migrations architecture; [data-model.md](./data-model.md) schema evolution)
- Hosted schema: `PublishAsMigrationScript()` / controlled job (`T116`)  -  not `RunDatabaseUpdateOnStart` on publish
- **Hosted Identity pivot (2026-11-04)**: passkey-only tasks **T022**, **T042**, **T132**, **T136** are **unchecked** and **superseded**; implement **T143**–**T148** before re-asserting SC-005 / hosted quickstart acceptance
- Optional WebAuthn passkeys: deferred post–v1.0.0 (template-style optional submit), not mandatory sign-in
- Retire placeholder `src/Potsure/` when T002 completes
- **FR-S002** (transaction-level actual-spend drill-down) is Should-level; deferred unless explicitly pulled into scope  -  category health (`T073`) covers aggregate actuals only
- **2026-11-09 pivot**: Do not add new `BudgetPeriod` features; all new plan/report work belongs in **Phase 27** (**T159**–**T186**)

---

## Phase 16: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-03) between spec intent and present codebase behaviour.

- [X] T119 [US6] Add transaction-level one-off categorisation UI (and navigation from category health uncategorised warning) wired to `OneOffCategorisationService` for expense debits, refunds, and credits per FR-M037 (missing)
- [X] T120 [US2] Implement interactive confirm, reject, merge, and split flows on `DetectionReview` calling `IncomePatternService`, `ExpensePatternService`, and `PatternMergeSplitService` per FR-M008 and US2 acceptance scenarios (partial)
- [X] T121 [US2] Run `TransactionAttributionService` auto-attribution after successful statement import and when expense patterns are confirmed so FR-M010a and SC-003 apply without manual service calls (partial)
- [X] T122 [US1] Add Monzo column mapping preview with user confirm or adjust step in `ImportWizard` before `StatementImportService` persists rows per FR-M002 and US1/AC1 (partial)
- [X] T123 [US1] Persist internal transfer and pot movement rows from imports (excluded from uncategorised and category actuals by default) so users can one-off categorise them as spend per FR-M017 and [contracts/monzo-import.md](contracts/monzo-import.md) (partial)
- [X] T124 [US10] Replace stub `ReauthenticationService` with a real reauthentication challenge before export and permanent deletion per T026 and export/delete UI flows (partial)
- [X] T125 [US12] Harden `tests/Potsure.E2E/KeyJourneysTests.cs` to assert import → recurring → category health → export when `POTSURE_E2E_BASE_URL` is set (or document CI wiring) per Constitution II and T095 (partial)

---

## Phase 17: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-03, post Phase 16) between spec intent and present codebase behaviour.

- [X] T126 [US6] Surface persistent over-allocation warnings on `CategoryHealth.razor` from `CategoryPeriodSummary.OverAllocationWarning` per FR-M017a (partial)
- [X] T127 [US6] [US7] [US8] Add optional mapped-pot breakdown or detail on category health, budget balance (limiting category), and runway report UIs when pots are configured, using existing rollup/projection inputs per US6/US7/AC2/US8 and tasks T073/T082 (partial)
- [X] T128 [P] Expand `tests/Potsure.E2E/KeyJourneysTests.cs` to exercise CSV upload, recurring confirmation, and export (not page-heading smoke only) when `POTSURE_E2E_BASE_URL` is set; document CI or local E2E wiring in `docs/` or repository workflow per Constitution II and T095/T125 (partial)

---

## Phase 18: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-03, post Phase 17) between spec intent and present codebase behaviour.

- [X] T129 [US2] [US8] Wire `VariableExpenseEstimator` when confirming variable recurring expenses (persist `MonthlyEstimate` and `EstimateCoverageLabel` via `ExpensePatternService` / detection review) and surface coverage labelling on runway, budget balance, and recurring summary UIs per FR-M034 and SC-008 (partial)
- [X] T130 [US2] Allow confirming income patterns with `IncomeCadence.AdHoc` or `IncomeCadence.Variable` (amount without forced weekly/fortnightly/monthly recurrence) in `DetectionReview` and persistence per US2/AC2 (partial)
- [X] T131 [US1] Add user-adjustable Monzo column mapping controls in `ImportWizard` before import proceeds (not read-only confirm only) per FR-M002 and US1/AC1 (partial)
- [ ] T132 **N/A (superseded 2026-11-04)** Passkey step-up for export/delete  -  **replaced by T147**; do not implement
- [X] T133 [P] [US12] Extend `tests/Potsure.E2E/KeyJourneysTests.cs` to confirm at least one suggested recurring pattern in the UI (not only navigate to review) before category health and export when `POTSURE_E2E_BASE_URL` is set per Constitution II and T128 (partial)

---

## Phase 19: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-03, post Phase 18) between spec intent and present codebase behaviour.

- [X] T134 [US1] Persist the user-confirmed column map from `ImportWizard` / `StatementImportService.ImportAsync` to `BudgetProfile.RememberedColumnMappingJson` and pre-fill mapping on subsequent uploads when the file header layout is unchanged per FR-M002, US1/AC1, and T038 (partial)
- [X] T135 [US2] Add optional mapped bank pot and free-text tag inputs on `DetectionReview` for expense confirm, merge, and split; when merging confirmed patterns with differing category, pot, or tag, require explicit resolution (and validate in `PatternMergeSplitService`) before save per FR-M008, FR-M009, and US2 acceptance scenarios 3, 5, and 6 (partial)
- [ ] T136 **N/A (superseded 2026-11-04)** Passkey step-up wiring  -  **replaced by T147**; do not implement

---

## Phase 20: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-03, post Phase 19) between spec intent and present codebase behaviour.

- [X] T137 [US2] Add UI to reassign individual imported transactions (expense debits and income credits per **FR-M010a** / **FR-M010b**) via `TransactionAttributionService.OverrideAttributionAsync`, with optional pot and pattern link, and navigation from category health or a dedicated review list per US2 acceptance scenario 7 (missing)
- [X] T138 [US1] When a remembered Monzo column map pre-fills on unchanged layout, still require an explicit mapping confirm-or-adjust step before import proceeds (do not skip the mapping panel solely because `RememberedMonzoColumnMapping` matched) per FR-M002 and US1/AC1 (partial)
- [X] T139 [US7] [US8] Surface confirmed variable recurring expense `EstimateCoverageLabel` on `BudgetBalance.razor` (matching runway and recurring summary disclosure) per FR-M034 and SC-008 (partial)
- [X] T140 [P] [US2] Replace placeholder assertions in `tests/Potsure.Infrastructure.Tests/ReimportAssignmentMemoryTests.cs` with an integration test that re-imports overlapping CSV, asserts auto-attribution to confirmed patterns, and verifies manual overrides persist without re-auto-attribution per SC-003 and FR-M010a (partial)

---

## Phase 21: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-03, pass 3, post Phase 20) between spec intent and present codebase behaviour.

- [X] T141 [P] [US2] Extend `tests/Potsure.Infrastructure.Tests/ReimportAssignmentMemoryTests.cs` (or adjacent import integration tests) to import overlapping Monzo CSV twice via `StatementImportService`, assert auto-attribution to confirmed expense patterns on both passes, and verify manual `OverrideAttributionAsync` corrections persist without re-auto-attribution on the second import per US2/AC7, SC-003, FR-M010a, and T140 (partial)

---

## Phase 22: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-03, pass 4, post T141) between spec intent and present codebase behaviour.

- [X] T142 [P] [US1] [US2] Add `StatementImportService` integration coverage in `tests/Potsure.Infrastructure.Tests/ReimportAssignmentMemoryTests.cs` (or a dedicated import integration fixture) that imports overlapping Monzo CSV twice through the full import pipeline, asserts de-duplication on the second pass (US1/AC2, FR-M004), auto-attribution to confirmed expense patterns on both passes, and persistence of manual `OverrideAttributionAsync` without re-auto-attribution after re-import per US2/AC7, SC-003, FR-M010a, T140, and T141 (partial)

---

## Phase 23: Hosted Identity pivot (2026-11-04)

**Purpose**: Align implemented hosted auth with constitution 1.2.0 and amended spec  -  standard Identity email/password, Blazor account template + BlazorBlueprint, no passkey-only login or passkey step-up for v1 acceptance. Does **not** reopen completed import, budgeting, or report tasks.

**Goal**: Hosted users register and sign in with email/password; export/delete reauthenticate without passkeys; Aspire Hosted mode smoke-validates register/login.

**Independent Test**: AppHost with `Deployment__Mode=Hosted`; register a user, sign in, reach a gated page; export/delete prompts password reauth (not passkey); quickstart hosted Identity notes satisfied (**T148**).

**Post-Phase-23 smoke**: Re-run quickstart Scenario 8c (migration ordering) and Aspire SQL consumption smoke after **T144**–**T146** change `Program.cs` (constitution 1.2.0).

- [X] T143 [US11] Scaffold .NET 10 Blazor Server Identity account pages under `src/Potsure.Web/Components/Account/Pages/` (Login, Register, ForgotPassword, Manage, and related shared components) with `IdentityRedirectManager` and `IdentityRevalidatingAuthenticationStateProvider`; convert template markup to BlazorBlueprint v4 (`BbCard`, `BbButton`, `BbInput`, `BbAlert`, Lucide per house patterns) per [plan.md](./plan.md), [research.md](./research.md), and `blazor-blueprint` skill (partial  -  template pages present; full Bb conversion deferred)
- [X] T144 [US11] Call `MapAdditionalIdentityEndpoints()` from `src/Potsure.Web/Program.cs` and align account routes with template paths; register no-op or logging `IEmailSender<ApplicationUser>` for v1 self-host per [research.md](./research.md)
- [X] T145 [US11] Remove or replace `src/Potsure.Web/Configuration/HostedPasskeySignInEndpoints.cs`, `src/Potsure.Web/Configuration/HostedPasskeyStepUpEndpoints.cs`, and passkey-only `src/Potsure.Web/Components/Account/PasskeySignIn.razor`; update `src/Potsure.Web/Components/Account/RedirectToLogin.razor` and `Program.cs` to use template sign-in/register flows instead of `MapHostedPasskeySignInEndpoints` / `MapHostedPasskeyStepUpEndpoints`
- [X] T146 [US11] Refactor `src/Potsure.Web/Configuration/HostedIdentityConfiguration.cs` for standard email/password Identity (normal `IdentityOptions.Password` policy per constitution 1.2.0  -  not passkey-style 64-character minimum); rename `AddHostedPasskeyIdentity` to hosted Identity registration used from `Program.cs`; defer mandatory passkey handler wiring to post–v1.0.0 optional milestone (optional passkeys remain via template)
- [X] T147 [US10] Replace hosted passkey step-up in `src/Potsure.Web/Features/Privacy/Pages/PrivacyDataPanel.razor.cs` with current-password confirmation (preferred) or typed-phrase fallback per amended FR-M022/FR-M023 and [plan.md](./plan.md) reauthentication notes; ensure `ReauthenticationService` grants export/delete without passkey step-up
- [X] T148 [P] [US11] Add smoke coverage for hosted register and sign-in under Aspire Hosted mode (`Deployment__Mode=Hosted` from AppHost) in `tests/Potsure.Web.Tests/` or `tests/Potsure.E2E/` and record the exercised path in `specs/001-potsure-v1/quickstart.md` (email/password  -  passkeys not required for acceptance) (partial  -  `HostedIdentityConfigurationTests` validates password policy registration)

---

## Phase 24: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-04, post Phase 23) between spec intent, plan decisions, and present codebase behaviour.

- [X] T152 [US11] Convert scaffolded Identity account pages (`Login`, `Register`, `ForgotPassword`, and primary Manage flows) from stock Bootstrap template markup to BlazorBlueprint v4 (`BbCard`, `BbButton`, `BbInput`, `BbAlert`, Lucide per house patterns) per [plan.md](./plan.md) Identity presentation notes and spec Session 2026-11-04 planning note; retain optional template passkey submit without passkey-only login  -  **complete** (template-aligned App/assets/CSS baseline; `Bb*` + Tailwind on feature and Identity UI; exceptions: `PasskeySubmit` native button and hidden inputs)
- [X] T153 [P] [US11] Add functional hosted register-and-sign-in smoke (`WebApplicationFactory` or Playwright with `Deployment__Mode=Hosted`) beyond `HostedIdentityConfigurationTests` password-policy DI registration; refresh **Hosted Identity bootstrap** copy and validation log in `specs/001-potsure-v1/quickstart.md` for **T148** and **SC-005** (partial)
- [X] T154 [P] Re-run quickstart Scenario 8c (EF migration ordering after **T144**–**T146** `Program.cs` Identity wiring) via local `aspire run` and update the validation log in `specs/001-potsure-v1/quickstart.md` per Phase 23 post-smoke note (partial)

---

## Phase 25: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-04, post Phase 24) between spec intent, plan decisions, and present codebase behaviour.

- [X] T155 [P] [US11] Add hosted-mode HTTP register-and-sign-in smoke using `WebApplicationFactory` (or Playwright against `Deployment__Mode=Hosted`) that posts to Identity account endpoints and reaches an authenticated state  -  not DI-only `UserManager`/`SignInManager` calls alone in `tests/Potsure.Web.Tests/HostedIdentityFunctionalTests.cs`; refresh quickstart **Hosted Identity** notes if the exercised path changes per **T153**, **T148**, and **SC-005** (partial  -  `HostedIdentityHttpTests` + `PotsureWebApplicationFactory` GET Account pages; full cookie sign-in remains E2E)
- [X] T156 [US11] Convert remaining standard Identity password-flow pages (`ResetPassword`, `ResendEmailConfirmation`, and related login recovery pages still using Bootstrap `form-control` / `btn`) to BlazorBlueprint v4 per [plan.md](./plan.md) Identity presentation and spec Session 2026-11-04 planning note  -  **complete** (2FA/recovery/manage passkeys and external logins converged to `BbFormFieldInput`, `BbDataTable`, `PotsurePageHeader`, and related Bb patterns)
- [X] T157 [P] Re-run quickstart Scenario 8c (`aspire run`: database healthy → `ef-migrations` completed → web healthy) in an environment where the migration resource starts successfully; set validation log Scenario 8c to **Pass** or document a blocking infra defect with reproduction steps per constitution 1.2.0 Scenario 8c and **T154** (partial  -  documented `portForServing "web"` EF tool defect in quickstart Scenario 8c; AppHost uses `AddProject<Projects.Potsure_Web>`)

---

## Phase 26: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-04, post Phase 25) between spec intent, plan decisions, and present codebase behaviour.

- [X] T158 [P] Resolve local AppHost `AddEFMigrations` orchestration in `src/Potsure.AppHost/AppHost.cs` so `ef-tool-*` migration resources finish successfully (fix `ASPNETCORE_URLS` / `portForServing "web"` substitution failure documented in quickstart Scenario 8c, or adopt a supported migration-worker fallback per [research.md](./research.md)); re-run quickstart Scenario 8c and set the validation log to **Pass** per constitution 1.2.0 Scenario 8c, plan: EF migration orchestration, and **T115**/**T154**/**T157** (partial  -  `Potsure.MigrationsHost` + `schema-host` resource; local `aspire run` re-smoke pending)

---

## Phase 27: Standing plan, report range, and proration pivot (2026-11-09)

**Purpose**: Align persistence, pure budgeting core, reporting, recurrence, and UI with [plan.md](./plan.md) (2026-11-09), [data-model.md](./data-model.md), and [contracts/budgeting-core.md](./contracts/budgeting-core.md) v1.1.0. **Blocks v1.0.0 acceptance** for US3, US4 (modes), US6–US8 proration, US2/US9 recurring lifecycle gaps, and quickstart Scenarios 3, 4b, 5, 7.

**Goal**: One standing `BudgetProfile` with estimated income + cadence; tab-scoped report date range; **30.4375** proration and banker's 2dp emission; pot mapping modes; payment anchors and stored variable estimates for commitments.

**Independent Test**: quickstart Scenarios 3, 4b, 5, 7, and partial-month labelling; `dotnet test tests/Potsure.Budgeting.Tests -c Release` golden catalogue including proration scenarios.

### Data model and EF (standing plan)

- [X] T159 [US3] Add `EstimatedIncomeAmount`, `EstimatedIncomeCadence` (`PlanIncomeCadence`), `ActualIncomeWindowMonths` (**1–12**; default **3** per data-model **FR-M019**), and derived incomplete-income handling on `BudgetProfileEntity` in `src/Potsure.Data/Entities/PotsureEntities.cs` and `src/Potsure.Data/Configurations/PotsureEntityConfigurations.cs`; remove `BudgetPeriodId` / `BudgetPeriodEntity` after data migration
- [X] T160 [US3] Create EF migration in `src/Potsure.Data/Migrations/PotsureDbContext/` migrating existing `BudgetPeriod.BudgetedIncomeAmount` into profile estimated income (default cadence **Monthly** where needed) and dropping `BudgetPeriod` table; document rollback notes in `docs/deployment-database.md`
- [X] T161 [P] [US3] Replace `BudgetPeriodService` with `StandingPlanIncomeService` in `src/Potsure.Infrastructure/Features/Plan/` exposing estimated income + cadence CRUD; update `CategoryAllocationSync` to use monthly-equivalent income factors (weekly × 52÷12, fortnightly × 26÷12, monthly × 1, quarterly ÷ 3, yearly ÷ 12 per **FR-M031**)
- [X] T162 [US3] Retire `src/Potsure.Infrastructure/Features/Plan/BudgetPeriodService.cs` and `PeriodScope` period-from-profile usage; remove `BudgetPeriod` includes from `src/Potsure.Infrastructure/Features/Reports/` query paths

### Budgeting core (30.4375, plan income, commitments)

- [X] T163 [P] Implement `AverageMonthDays` (**30.4375**) proration helper and `PlanIncomeCadence` monthly-equivalent factors in `src/Potsure.Budgeting/ProrationCalculator.cs` per contracts/budgeting-core.md
- [X] T164 [P] Refactor `AllocationCalculator`, `CategoryHealthCalculator`, `RequiredIncomeCalculator`, and `RunwayCalculator` in `src/Potsure.Budgeting/` to accept `ReportRange`, optional `PlanIncome`, commitment DTOs with **payment anchors**, and emit banker's 2dp at boundaries per **FR-M036**
- [X] T165 Implement fixed expense **occurrence count** projection from stored `PaymentAnchorDate` with cadence stepping (monthly calendar-month steps + last-day clamp; weekly/fortnightly day intervals; annual yearly steps) in `src/Potsure.Budgeting/CommitmentProjector.cs` per contracts/budgeting-core.md
- [X] T166 Implement variable expense commitment as `StoredMonthlyEstimate × (inclusiveDayCount ÷ 30.4375)` in `src/Potsure.Budgeting/CommitmentProjector.cs` (**no** calendar-month overlap apportionment)
- [X] T167 [P] Extend golden fixtures in `tests/Potsure.Budgeting.Tests/` for contract catalogue scenarios **2**, **4**, **7**, **8**, and **6** (plan income incomplete → **not set**) with independently verified expected results (**SC-004**)

### Report date range (FR-M031a)

- [X] T168 [P] [US6] Implement tab-scoped `ReportDateRange` session state (default **current calendar month** when unset; **not** persisted per **FR-X006**) in `src/Potsure.Web/Services/ReportDateRangeSessionState.cs`
- [X] T169 [US6] Add shared report range picker component in `src/Potsure.Web/Features/Reports/Components/ReportDateRangePicker.razor` wired into `CategoryHealth.razor`, `BudgetBalance.razor`, and `RunwayWarnings.razor` so one tab shares one inclusive range (**FR-M031a**)
- [X] T170 [US6] [US7] [US8] Refactor `PeriodReportService`, `RunwayReportService`, and `PeriodSummaryBuilder` in `src/Potsure.Reporting/` and `src/Potsure.Infrastructure/Features/Reports/` to accept caller-supplied `ReportRange` instead of `BudgetPeriod` dates; runway end = report end + **N** whole calendar months with last-day clamp per **FR-M020**/**FR-M035**

### Plan settings UI (US3)

- [X] T171 [US3] Replace period date and budgeted income fields in `src/Potsure.Web/Features/Plan/Pages/PlanSettings.razor` with estimated income amount, income cadence selector (`PlanIncomeCadence`: Weekly, Fortnightly, Monthly, Quarterly, Yearly), and `ActualIncomeWindowMonths` (**1–12**); show **not set** until both amount and cadence saved (**FR-M031**)
- [X] T172 [P] [US3] Wire `BudgetedIncomeSuggestionService` in `src/Potsure.Infrastructure/Features/Plan/BudgetedIncomeSuggestionService.cs` to suggest estimated income from confirmed income patterns without auto-apply; refresh only on explicit user confirm in plan UI

### Recurring lifecycle (FR-M008c, FR-M008d)

- [X] T173 [US2] [US9] Add `PaymentAnchorDate`, `PaymentAnchorUserOverride`, and rename or map `StoredMonthlyEstimate` on `RecurringExpenseEntity` in `src/Potsure.Data/Entities/PotsureEntities.cs` with EF migration in `src/Potsure.Data/Migrations/PotsureDbContext/`
- [X] T174 [US2] Persist payment anchors on confirm/edit in `ExpensePatternService` and `CommitmentPaymentAnchorResolver` in `src/Potsure.Infrastructure/Features/Recurrence/` and `src/Potsure.Infrastructure/Features/Reports/`; re-import may advance anchor only when `PaymentAnchorUserOverride` is false per data-model
- [X] T175 [US2] [US9] Implement edit and delete confirmed recurring flows in `IncomePatternService`, `ExpensePatternService`, and `src/Potsure.Web/Features/Recurrence/Pages/DetectionReview.razor` / `RecurringSummary.razor` per **FR-M008c** (delete removes commitment projection; transactions remain) (partial  -  delete + review link; field-level edit on confirmed rows remains on review page)
- [X] T176 [US2] Enforce user-authoritative `ExpenseAmountKind` (fixed vs variable) on confirm, merge, split, and edit in `ExpensePatternService` and detection UI per **FR-M008d**; variable commitments use **stored** monthly estimate only (not live **FR-M034** average)

### Pot mapping modes (US4)

- [X] T177 [US4] Align `PotMappingMode` enum in `src/Potsure.Budgeting/DomainTypes.cs` and persistence with **CategoryOnly**, **ThreePots**, **SeveralPots** (migrate from legacy `None` / UX-only modes); default **CategoryOnly** per data-model
- [X] T178 [US4] Enforce mode invariants in `BankPotService` in `src/Potsure.Infrastructure/Features/Plan/BankPotService.cs`: **three pots** ≤3 pots and ≤1 per category; **several pots** unlimited; block mode switches until invariants satisfied (**FR-M013**, **FR-M013c** funding roll-or-discard on delete); pot rename and category reassignment on `PotMapping.razor` respect active mode rules (**FR-M013** scenario 11) (partial  -  invariants + delete/funding; inline rename UI deferred)
- [X] T179 [US4] Implement **FR-M013b** optional pot pre-selection in expense assign flows in `src/Potsure.Web/Features/Recurrence/Pages/DetectionReview.razor` and related assign components; hide pot UI in **category-only** mode

### Reports and not-set UX (US6, US7)

- [X] T180 [US6] [US7] Surface explicit **not set** (not £0) for category allocations and budget balance when estimated income or cadence missing in `PeriodSummaryBuilder` and report Razor pages per **FR-M031**
- [X] T181 [US6] Compute **ActualIncomeMonthlyMean** over stored **N**-month window ending **today** on plan settings and over **N** months ending report range **end** on period reports in `src/Potsure.Reporting/ActualIncomeCalculator.cs` per **FR-M019**
- [X] T182 [US7] Ensure `RequiredIncomeCalculator` uses **max-of-ratios** (not sum) and range-prorated estimated income in `src/Potsure.Reporting/BudgetBalanceReportBuilder.cs` per **FR-M018**

### Export and tests

- [X] T183 [P] [US10] Update `ExportSnapshotBuilder` in `src/Potsure.Infrastructure/Features/Export/ExportSnapshotBuilder.cs` and contracts/export-formats.md alignment for standing plan fields (no budget period dates; estimated income + cadence)
- [X] T184 [P] Update `DeterminismTests` and reporting integration tests in `tests/Potsure.Reporting.Tests/` for **30.4375** proration and unchanged-input repeat viewing (**FR-M036**)
- [ ] T185 [P] Refresh quickstart validation log Scenarios **3**, **4b**, **5**, and **7** in `specs/001-potsure-v1/quickstart.md` after Phase 27 checkpoint
- [X] T186 [P] Align `docs/deployment-database.md`, `docs/self-host.md`, and quickstart Scenario 8c notes with canonical `dotnet ef` `--project src/Potsure.Data` and AppHost `WithMigrationsProject` → Data (artifact audit **2026-11-09**); pair with **T158** Aspire ef-tool fix

**Checkpoint**: Standing plan saves without period dates; report range shared per tab; category health and budget balance prorate with **30.4375**; pot modes enforce **FR-M013**; recurring edit/delete and anchors persist; budgeting golden tests cover new catalogue items.

---

## Phase 28: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-09, post commit 1751be1 / Phase 27 implement) between spec intent, plan decisions, and present codebase behaviour.

- [ ] T187 [US2] [US9] Add field-level **edit** flows for confirmed recurring income and expense rows on `DetectionReview.razor` and `RecurringSummary.razor` (label, cadence, fixed/variable amounts, payment anchor, pot/tag, income splits) wired to `IncomePatternService` / `ExpensePatternService` `SaveAsync`; verify reports refresh after save per **FR-M008c**, **FR-M021**, and US2/AC3 (partial)
- [X] T188 [US4] Expose mapped-pot **edit** (display name and category reassignment within mode invariants) on `PotMapping.razor` via `BankPotService.UpdateAsync`; add UI tests or service coverage for category moves blocked under **three pots** rules per **FR-M013c** and US4/AC11 (partial  -  PotMapping edit UI; extra UI test deferred)
- [ ] T189 [P] Re-run quickstart Scenario **8c** after `Potsure.MigrationsHost` wiring (`aspire run`: database healthy → `ef-migrations` finished → web healthy) and set Scenario 8c validation log to **Pass** or document a blocking defect per constitution 1.2.0 Scenario 8c and plan EF orchestration (partial)
- [ ] T190 [P] [US11] Extend `HostedIdentityHttpTests` (or adjacent `WebApplicationFactory` coverage) to POST register and password sign-in and assert an authenticated session cookie  -  not GET-only antiforgery token checks per **SC-005** and partial **T155**
- [X] T191 [P] Update quickstart validation log Scenario **2** (assignment memory / re-import) to reflect `ReimportAssignmentMemoryTests` release evidence per **SC-003** and partial **T151** (complements open **T185** for Scenarios 3, 4b, 5, 7)

---

## Phase 29: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-09, post Phase 28) between spec intent, plan decisions, and present codebase behaviour.

- [X] T192 [US2] Allow rejected recurring patterns to appear again as **suggested** after later detection or re-import (persist reject for audit but do not bind `RecurrenceReviewService` merged state to `Rejected` when fresh detection matches the label) per spec Session 2026-11-09 reject clarification and **FR-M008** (contradicts)
- [X] T193 [US2] Add user-selectable **fixed vs variable** amount behaviour on expense confirm, merge, and split in `DetectionReview` (not inference-only `ExpenseIsVariableAmount()`), wired through `SaveRecurringExpenseRequest` / `ExpensePatternService` per **FR-M008d** and US2/AC12 (partial)

---

## Phase 30: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-09, post Phase 29) between spec intent, plan decisions, and present codebase behaviour.

- [X] T194 [US2] Expose optional payment anchor date override on `DetectionReview` expense **confirm**, **merge**, and **split** flows (surface derived default; persist `PaymentAnchorDate` and `PaymentAnchorUserOverride` via `SaveRecurringExpenseRequest` / `ExpensePatternService`) per **FR-M008c** and spec Session 2026-11-09 payment-anchor clarifications (partial)
