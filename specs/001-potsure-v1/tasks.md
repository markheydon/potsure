---

description: "Task list for Potsure v1 implementation"
---

# Tasks: Potsure v1

**Plan sync**: 2026-11-04 (hosted Identity pivot: email/password primary, passkey-only path superseded per [plan.md](./plan.md), [spec.md](./spec.md) Session 2026-11-04, constitution 1.2.0). Prior sync 2026-11-03: Aspire SQL client, EF `AddEFMigrations`.

**Analyze gate (2026-11-04)**: Core artifacts align with constitution **1.2.0** (hosted Identity pivot, release gates, superseded-task mapping). **Unchecked** tasks below are the **implementation and release-evidence queue** (Phase 23 + **T149**–**T151** + superseded placeholders **T022** / **T042** / **T132** / **T136**) — not missing spec or plan coverage.

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
- [X] T003 Add central package versions in `Directory.Packages.props` for CsvHelper, EF Core 10 SQL Server, Aspire hosting integrations, BlazorBlueprint Components/Primitives/Icons, ASP.NET Core Identity passkeys package (optional/deferred — not v1.0.0 acceptance-critical), and Microsoft.Extensions.Http.Resilience (latest stable at introduction; constitution 1.2.0 + AGENTS.md outdated sweep before dependency work complete)
- [X] T004 [P] Wire project references: Web → Infrastructure, Reporting; Infrastructure → Statements, Recurrence, EF; Reporting → Budgeting; Recurrence → Budgeting; Statements standalone
- [X] T005 [P] Create test projects `tests/Potsure.Budgeting.Tests`, `tests/Potsure.Statements.Tests`, `tests/Potsure.Recurrence.Tests`, `tests/Potsure.Reporting.Tests`, `tests/Potsure.Infrastructure.Tests`, `tests/Potsure.Web.Tests`, `tests/Potsure.E2E` and add to `Potsure.slnx`
- [X] T006 [P] Add `samples/monzo/` with README stating anonymised fixtures only; no personal exports in repo
- [X] T007 Scaffold `src/Potsure.AppHost/Program.cs` with `AddAzureContainerAppEnvironment`, `AddAzureSqlServer`, `RunAsContainer`, `AddDatabase("potsure")`, web project reference, `WithReference`, `WaitFor`, `PublishAsAzureContainerApp` per plan.md (database reference name must match web `AddSqlServerDbContext` client name). **Superseded for schema readiness** by **T113** (plan 2026-11-03): add `AddEFMigrations` and web `WaitForCompletion(migrations)` — do not treat database `WaitFor` alone as sufficient after T113 lands
- [X] T008 Configure `src/Potsure.Web` for Blazor Server InteractiveServer, `net10.0`, nullable, analysers aligned with `CONVENTIONS.md`
- [X] T009 [P] Add `docs/` placeholder structure for self-host, encryption, and deployment stages (implement content in Polish phase)

**Checkpoint**: Solution builds with empty project shells.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Pure budgeting core, persistence, auth modes, and UI shell — **blocks all user stories**.

**⚠️ CRITICAL**: No user story feature work until this phase completes.

- [X] T010 Define shared enums and records in `src/Potsure.Budgeting/` matching data-model.md (`BudgetCategoryKind`, `AllocationPreset`, `TransactionKind`, `PatternConfirmationState`, etc.)
- [X] T011 Implement `Money` and rounding rules (GBP 2dp at report emission; banker's rounding) in `src/Potsure.Budgeting/Money.cs` per contracts/budgeting-core.md
- [X] T012 Implement preset allocation from budgeted income in `src/Potsure.Budgeting/AllocationCalculator.cs` (50/30/20 default, 75/15/10 alternative; category-level only)
- [X] T013 Implement category health lines (`UnderOver` = allocation − actual activity; recorded funding excluded) in `src/Potsure.Budgeting/CategoryHealthCalculator.cs` per FR-M017
- [X] T014 Implement required income, limiting category (tie: Needs → Wants → Savings & Debt), shortfall, yes/no conclusion in `src/Potsure.Budgeting/RequiredIncomeCalculator.cs` per contracts/budgeting-core.md and FR-M018
- [X] T015 Implement runway comparison (`ProjectedRemainingSpend > allocation − actual activity + recorded funding`) in `src/Potsure.Budgeting/RunwayCalculator.cs` per FR-M020
- [X] T016 [P] Add golden scenario fixtures and tests in `tests/Potsure.Budgeting.Tests/` covering contracts/budgeting-core.md minimum catalogue (8 scenarios)
- [X] T017 Define EF entity types and configurations in `src/Potsure.Infrastructure/Persistence/` from data-model.md (`BudgetProfile`, `BudgetPeriod`, `BudgetCategory`, `BankPot`, `Transaction`, `IncomeSource`, `RecurringExpense`, `Assignment`, `StatementUpload`, `MonzoAccountAssociation`)
- [X] T018 Add `UserId` on user-owned entities, global query filters, and ownership validation helpers in `src/Potsure.Infrastructure/Persistence/Tenancy/` per research.md
- [X] T019 Create initial EF migration in `src/Potsure.Infrastructure/Persistence/Migrations/` and document idempotent deploy approach in `docs/deployment-database.md`
- [X] T020 Configure ASP.NET Core Data Protection key ring persistence to SQL in `src/Potsure.Infrastructure/` reconciled with Aspire SQL resource
- [X] T021 Implement deployment mode options (local-only vs hosted) in `src/Potsure.Web/Configuration/DeploymentModeOptions.cs` with FR-M024 network non-exposure for local-only
- [ ] T022 Configure ASP.NET Core Identity with passkey support and SQL persistence in `src/Potsure.Web/` for hosted mode (FR-M025; no password fallback v1) — **Superseded 2026-11-04** (passkey-primary / no-password policy incompatible with constitution 1.2.0); **replaced by T143**–**T146** (not acceptance evidence while unchecked)
- [X] T023 Implement local-only single-user profile key path without remote account in `src/Potsure.Infrastructure/` when deployment mode is local-only
- [X] T024 Register `AddBlazorBlueprintComponents()`, `App.razor` theme assets, `_Imports.razor`, and `BbPortalHost` in interactive root layout under `src/Potsure.Web/`
- [X] T025 Create base interactive layout and navigation shell in `src/Potsure.Web/Components/Layout/` (UK English labels)
- [X] T026 Implement reauthentication gate service in `src/Potsure.Web/Services/ReauthenticationService.cs` for export, deletion, and credential changes
- [X] T027 Configure OpenTelemetry in `src/Potsure.Web/` excluding upload bodies, payee text, account refs, and export payloads per research.md
- [X] T028 Register DI composition root in `src/Potsure.Web/Program.cs` and `src/Potsure.Infrastructure/DependencyInjection.cs`
- [X] T107 [P] Register runtime SQL via `builder.AddSqlServerDbContext<PotsureDbContext>("potsure")` in `src/Potsure.Web/Program.cs` aligned with AppHost `AddDatabase("potsure")`; no runtime `UseSqlServer` or hand-authored connection strings in web startup (constitution 1.2.0, plan.md AppHost resource model)
- [X] T108 [P] Restrict `UseSqlServer(connectionString)` to `src/Potsure.Infrastructure/Persistence/PotsureDbContextFactory.cs` for EF design-time migrations only; document fallback connection for local `dotnet ef` in `docs/deployment-database.md`

### EF migration orchestration (plan amend 2026-11-03)

- [X] T111 Add `Aspire.Hosting.EntityFrameworkCore` to central `Directory.Packages.props` (13.6.4, aligned with existing Aspire packages) and reference from `src/Potsure.AppHost/Potsure.AppHost.csproj`
- [X] T112 Add AppHost project reference to `src/Potsure.Infrastructure/Potsure.Infrastructure.csproj` only as needed for `WithMigrationsProject<Projects.Potsure_Infrastructure>()` (no Infrastructure → AppHost reference)
- [X] T113 Wire `AddEFMigrations` on the web resource in `src/Potsure.AppHost/Program.cs`: `WithReference(database)`, `WaitFor(database)`, `WithMigrationsProject<Projects.Potsure_Infrastructure>()`, explicit DbContext `Potsure.Infrastructure.Persistence.PotsureDbContext`, `RunDatabaseUpdateOnStart()` for local AppHost only; web keeps `WithReference(database)` and uses `WaitForCompletion(migrations)` instead of schema readiness via `WaitFor(database)` alone; confirm no `Database.MigrateAsync()` (or equivalent) in `src/Potsure.Web/Program.cs`; confirm `BudgetProfileSeeder` and other app-data init are not invoked from the migration resource (plan.md AppHost resource model and EF Core migrations architecture)
- [X] T114 [P] Update `docs/deployment-database.md` and `docs/self-host.md` to distinguish local Aspire auto-apply vs hosted controlled migration job; document unchanged `dotnet ef migrations add` / `database update` commands (`--project` Infrastructure, `--startup-project` Web); note migration resource hidden in Aspire dashboard by default (`aspire describe --include-hidden`)
- [X] T116 [P] Wire hosted publish migration artefact path (`PublishAsMigrationScript()` and/or documented migration bundle) on AppHost publish model in `src/Potsure.AppHost/Program.cs` and single-runner apply steps in `docs/deployment-database.md` (no concurrent migrate on all replicas; `RunDatabaseUpdateOnStart` local-only) per plan.md hosted publish bullet

**Checkpoint**: AppHost starts; migrations complete before web; web resolves SQL via Aspire client DI; **local-only** shell loads; hosted mode blocks anonymous financial access (middleware). **Hosted email/password Identity UI** completes in **Phase 23** (**T143**–**T148**). Budgeting golden tests pass in CI.

---

## Phase 3: User Story 1 — Import Monzo statements safely (Priority: P1) 🎯

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
- [X] T037 [US1] Implement upload pipeline (stream parse → validate → stage → dedupe → atomic commit; temp cleanup on all exits) in `src/Potsure.Infrastructure/Imports/StatementImportService.cs` per research.md
- [X] T038 [US1] Persist `StatementUpload`, `ImportStatistics`, and remembered `MonzoColumnMapping` in `src/Potsure.Infrastructure/Persistence/Repositories/StatementUploadRepository.cs`
- [X] T039 [US1] Build import wizard UI (mapping confirm/adjust, account confirm, summary with skip reasons and transfer counts) in `src/Potsure.Web/Components/Import/`
- [X] T040 [US1] Ensure FR-M003: every skipped row reported; no silent drops in `StatementImportService` and import summary DTO

**Checkpoint**: US1 independent test passes via quickstart Scenario 1.

---

## Phase 4: User Story 11 — Local-only and hosted access modes (Priority: P1)

**Goal**: Constitution-mandated deployment paths with hosted Identity (email/password) and non-network local-only mode.

**Independent Test**: Local-only without network account; hosted requires Identity sign-in (passkeys not required); users isolated (spec US11). **Pivot**: passkey-only implementation from **T042** superseded — execute **Phase 23** (**T143**–**T148**).

- [X] T041 [P] [US11] Document local-only vs hosted behaviour in `docs/self-host.md` and `docs/local-only-mode.md`
- [ ] T042 [US11] Enforce hosted passkey-only sign-in UI and guidance when passkey unavailable in `src/Potsure.Web/Components/Account/` (FR-M025) — **Superseded 2026-11-04**; **replaced by T143** / **T145** (not acceptance evidence while unchecked)
- [X] T043 [US11] Block financial data endpoints and pages without authentication on hosted in `src/Potsure.Web/` middleware/filters
- [X] T044 [US11] Verify local-only mode does not bind Kestrel to public interfaces by default in `src/Potsure.Web/Program.cs` and document operator checklist in `docs/self-host.md`
- [X] T045 [US11] Add integration tests for tenancy isolation in `tests/Potsure.Infrastructure.Tests/TenancyIsolationTests.cs`
- [X] T102 [US11] Configure hosted encryption in transit (HTTPS/TLS ingress) and at rest (Azure SQL TDE/policy) via AppHost and web host; add verification checklist and acceptance tests in `docs/encryption.md` and `tests/Potsure.Infrastructure.Tests/HostedEncryptionAcceptanceTests.cs` (FR-M027, constitution IV)

**Checkpoint**: Tenancy isolation and encryption docs (`T102`); hosted middleware blocks anonymous financial access (`T043`). **US11 / SC-005 hosted acceptance is blocked** until **Phase 23** (**T143**–**T148**) — superseded passkey tasks **T042** do not satisfy FR-M025.

---

## Phase 5: User Story 3 — Choose preset and plan the period (Priority: P1)

**Goal**: Single working budget period, preset selection, user-confirmed budgeted income drives allocations only.

**Independent Test**: Switch presets; set inclusive date range; enter planned income; allocations = preset × budgeted income only (spec US3).

- [X] T046 [P] [US3] Seed fixed three `BudgetCategory` rows on profile create in `src/Potsure.Infrastructure/Persistence/BudgetProfileSeeder.cs` (FR-M012)
- [X] T047 [US3] Implement working period CRUD (inclusive `StartDate`/`EndDate`; single period only FR-M031) in `src/Potsure.Infrastructure/Services/BudgetPeriodService.cs`
- [X] T048 [US3] Implement preset switch with immediate allocation recalculation from unchanged budgeted income in `src/Potsure.Infrastructure/Services/AllocationPresetService.cs` (FR-M011)
- [X] T049 [US3] Implement optional budgeted income suggestion from confirmed income patterns without auto-apply in `src/Potsure.Infrastructure/Services/BudgetedIncomeSuggestionService.cs` (FR-M031)
- [X] T050 [US3] Build settings UI for preset, period dates, and budgeted income confirmation in `src/Potsure.Web/Components/Plan/PlanSettings.razor`
- [X] T051 [US3] Wire period-scoped date filtering helper (both start and end inclusive) in `src/Potsure.Reporting/PeriodScope.cs` for downstream reports
- [X] T103 [US3] Present `ActualIncomeInPeriod` separately from `BudgetedIncome` and forecast fields in plan settings UI and read models in `src/Potsure.Reporting/PeriodSummaryBuilder.cs` and `src/Potsure.Web/Components/Plan/PlanSettings.razor` (FR-M019)

**Checkpoint**: US3 independent test; quickstart Scenario 3.

---

## Phase 6: User Story 2 — Detect and confirm recurring patterns (Priority: P1)

**Goal**: Suggest, confirm, merge, split, reject recurring income/expenses; assignment memory and auto-attribution.

**Independent Test**: Import 1–3 months; confirm/merge/split; re-upload remembers assignments (spec US2).

### Tests for User Story 2

- [X] T052 [P] [US2] Add detection unit tests excluding internal transfers from input in `tests/Potsure.Recurrence.Tests/RecurrenceDetectionTests.cs`

### Implementation for User Story 2

- [X] T053 [P] [US2] Implement cadence detection (weekly, fortnightly, monthly) in `src/Potsure.Recurrence/RecurrenceDetector.cs` excluding `InternalTransfer` and `PotMovement` per FR-M007
- [X] T054 [US2] Implement variable expense monthly estimate with partial-month normalisation and coverage labels in `src/Potsure.Recurrence/VariableExpenseEstimator.cs` per FR-M034 and research.md
- [X] T055 [US2] Persist `IncomeSource` with splits must sum to 100% validation (display-only splits) in `src/Potsure.Infrastructure/Services/IncomePatternService.cs` per FR-M010
- [X] T056 [US2] Persist `RecurringExpense` with category required; optional pot and tag (tag free-text, no calc effect) in `src/Potsure.Infrastructure/Services/ExpensePatternService.cs` per FR-M009
- [X] T057 [US2] Implement merge/split workflows requiring user resolution of category, optional pot, and tag before save in `src/Potsure.Infrastructure/Services/PatternMergeSplitService.cs` per FR-M008
- [X] T058 [US2] Implement auto-attribution of matching debits to confirmed expense patterns with per-transaction override persistence in `src/Potsure.Infrastructure/Services/TransactionAttributionService.cs` per FR-M010a
- [X] T106 [US2] Add re-import integration test asserting remembered pattern assignments and auto-attribution without manual re-entry in `tests/Potsure.Infrastructure.Tests/ReimportAssignmentMemoryTests.cs` (SC-003)
- [X] T059 [US2] Build detection review UI (suggested vs confirmed, income vs expense) in `src/Potsure.Web/Components/Recurrence/`
- [X] T117 [P] [US2] Add UK English onboarding copy that three months of statement history is recommended for automatic recurring detection (**FR-S004**) in `src/Potsure.Web/Components/Recurrence/` and/or import completion summary without blocking one-month onboarding
- [X] T060 [US2] Project confirmed expense commitments cadence-aware into selected period in `src/Potsure.Reporting/CommitmentProjector.cs` for FR-M018 inputs

**Checkpoint**: US2 independent test; quickstart Scenario 2.

---

## Phase 7: User Story 4 — Map bank pots or budget by category only (Priority: P2)

**Goal**: Optional pots mapped 1:1 to categories; no double-counting.

**Independent Test**: Configure pots; verify combined totals count each amount once (spec US4).

- [X] T061 [P] [US4] Implement `BankPot` CRUD with exactly one `CategoryId` each in `src/Potsure.Infrastructure/Services/BankPotService.cs` (FR-M013; no hierarchy)
- [X] T062 [US4] Support `PotMappingMode` UX state (none, category-named, expense-specific) in `src/Potsure.Web/Components/Plan/PotMapping.razor`
- [X] T063 [US4] Roll pot recorded funding and allocations into category totals once in `src/Potsure.Reporting/CategoryRollup.cs` per FR-M017
- [X] T064 [US4] Allow category-only budgeting when user chooses no pots in `src/Potsure.Web/Components/Plan/` (FR-M014)

**Checkpoint**: US4 independent test with category-named and expense-specific pot fixtures.

---

## Phase 8: User Story 5 — Record funding already set aside (Priority: P2)

**Goal**: User-declared funding separate from imports and planned future funding.

**Independent Test**: Enter funding at category and pot; imports do not alter it (spec US5).

- [X] T065 [P] [US5] Implement recorded funding updates on category (`RecordedFundingDirect`) and pot (`RecordedFunding`) in `src/Potsure.Infrastructure/Services/RecordedFundingService.cs` (FR-M015, FR-M016)
- [X] T066 [US5] Implement planned future funding fields separately from recorded funding in `src/Potsure.Infrastructure/Services/PlannedFundingService.cs` (Key Entity: Planned future funding; US5 acceptance — distinct from imports and recorded funding)
- [X] T067 [US5] Build funding entry UI in `src/Potsure.Web/Components/Plan/RecordedFunding.razor` showing four measures context from FR-M017

**Checkpoint**: US5 independent test; funding unchanged after re-import.

---

## Phase 9: User Story 6 — Category health and actual spend (Priority: P1)

**Goal**: Four measures per category, uncategorised spend warning, one-offs, net refunds, over-allocation warning.

**Independent Test**: Category health shows allocation, funding, actual, under/over; orphan debits uncategorised (spec US6).

- [X] T068 [P] [US6] Implement net attributed actual activity (debits minus refunds/credits same rules) in `src/Potsure.Reporting/ActualActivityCalculator.cs` per FR-M017
- [X] T069 [US6] Implement uncategorised spend total excluding internal transfers unless one-off categorised in `src/Potsure.Reporting/UncategorisedSpendCalculator.cs` per FR-M017b
- [X] T070 [US6] Implement over-allocation persistent indicator when mapped pot plus unassigned recorded funding exceeds category allocation in `src/Potsure.Reporting/OverAllocationEvaluator.cs` (allow save; show warning FR-M017a)
- [X] T071 [US6] Implement one-off transaction categorisation (expense debits, refunds, credits) affecting actuals only in `src/Potsure.Infrastructure/Services/OneOffCategorisationService.cs` per FR-M037
- [X] T072 [US6] Assemble `PeriodSummary` per category in `src/Potsure.Reporting/PeriodSummaryBuilder.cs` using `Potsure.Budgeting` calculators
- [X] T073 [US6] Build category health report UI with optional pot breakdown in `src/Potsure.Web/Components/Reports/CategoryHealth.razor`
- [X] T104 [US6] Label actual receipt income separately from budgeted income and forecasts on category health and related report headers in `src/Potsure.Web/Components/Reports/CategoryHealth.razor` (FR-M019)
- [X] T074 [US6] Add reporting determinism test (repeat view identical totals) in `tests/Potsure.Reporting.Tests/DeterminismTests.cs` for FR-M036

**Checkpoint**: US6 independent test; quickstart Scenarios 4 and 7.

---

## Phase 10: User Story 7 — Required income, limiting category, and budget balance (Priority: P1)

**Goal**: FR-M018 report with commitments from confirmed recurring expenses only; actual receipts separate.

**Independent Test**: Shortfall shows limiting category, monetary gap, qualified yes/no (spec US7).

- [X] T075 [US7] Wire `RequiredIncomeCalculator` to cadence-projected commitments only in `src/Potsure.Reporting/BudgetBalanceReportBuilder.cs` (exclude income patterns, one-offs, funding)
- [X] T076 [US7] Build budget balance report UI with income timing from expected dates (not assumed four payments per month) in `src/Potsure.Web/Components/Reports/BudgetBalance.razor`
- [X] T077 [US7] Ensure messaging does not imply future costs covered by unreceived money (FR-M033) in report copy resources under `src/Potsure.Web/Resources/`
- [X] T105 [US7] Keep actual receipts and planned income visually and structurally separate on budget balance and runway report components in `src/Potsure.Web/Components/Reports/` (FR-M019; complements FR-M033)
- [X] T078 [US7] Extend golden tests for budget balance scenarios in `tests/Potsure.Budgeting.Tests/BudgetBalanceGoldenTests.cs`

**Checkpoint**: US7 independent test; quickstart Scenario 5.

---

## Phase 11: User Story 8 — Category runway warnings (Priority: P2)

**Goal**: Forecast horizon (default 3 months, user override); runway vs allocation − actual + recorded funding.

**Independent Test**: Trigger shortfall; see disclosure for history gaps (spec US8).

- [X] T079 [P] [US8] Implement forecast horizon setting on `BudgetProfile` (`ForecastHorizonMonths` default 3) in `src/Potsure.Infrastructure/Services/ForecastHorizonService.cs` per FR-M035 (Must: default 3 months and user override)
- [X] T080 [US8] Compute projected remaining spend including variable estimates in `src/Potsure.Reporting/RunwayProjector.cs` per FR-M020 and FR-M034
- [X] T081 [US8] Surface uncertainty flags for statement gaps and uncertain income in `src/Potsure.Reporting/UncertaintyFlagsBuilder.cs` per FR-M032
- [X] T082 [US8] Build runway warnings UI with optional pot detail in `src/Potsure.Web/Components/Reports/RunwayWarnings.razor`

**Checkpoint**: US8 independent test; quickstart partial-month labelling scenario.

---

## Phase 12: User Story 9 — Confirmed recurring summary (Priority: P2)

**Goal**: Audit view of confirmed patterns; suggestions separate.

**Independent Test**: Summary shows cadence, amounts, assignments, income splits; suggestions listed apart (spec US9).

- [X] T083 [P] [US9] Build read model query for confirmed vs suggested patterns in `src/Potsure.Infrastructure/Queries/RecurringSummaryQuery.cs`
- [X] T084 [US9] Build recurring summary page in `src/Potsure.Web/Components/Recurrence/RecurringSummary.razor` per FR-M021

**Checkpoint**: US9 independent test after US2 data present.

---

## Phase 13: User Story 10 — Export and permanent deletion (Priority: P1)

**Goal**: User-chosen JSON bundle or CSV ZIP export; irreversible deletion after reauth.

**Independent Test**: Export both formats; delete all financial data (spec US10).

- [X] T085 [P] [US10] Implement JSON bundle export per contracts/export-formats.md in `src/Potsure.Infrastructure/Export/JsonBundleExporter.cs`
- [X] T086 [P] [US10] Implement CSV ZIP export (`transactions.csv`, `plan.csv`, `metadata.json`) in `src/Potsure.Infrastructure/Export/CsvZipExporter.cs`
- [X] T087 [US10] Implement export orchestration with format choice and reauthentication in `src/Potsure.Infrastructure/Export/ExportService.cs` (FR-M022)
- [X] T088 [US10] Implement transactional permanent deletion of all user financial data in `src/Potsure.Infrastructure/Deletion/UserDataDeletionService.cs` per FR-M023 and data-model.md Deletion section
- [X] T089 [US10] Build export and delete UI with confirmation flows in `src/Potsure.Web/Components/Privacy/`
- [X] T090 [US10] Add export format contract tests in `tests/Potsure.Infrastructure.Tests/ExportFormatTests.cs`

**Checkpoint**: US10 independent test; quickstart Scenario 6.

---

## Phase 14: User Story 12 — Accessible, responsive review (Priority: P3)

**Goal**: Keyboard operability and usable mobile layout for core flows.

**Independent Test**: Keyboard-only import → recurring → category health → export; narrow viewport reports (spec US12).

- [X] T091 [P] [US12] Audit and fix focus order and keyboard activation on import wizard in `src/Potsure.Web/Components/Import/`
- [X] T092 [P] [US12] Audit and fix keyboard navigation on recurring review and reports in `src/Potsure.Web/Components/Recurrence/` and `Components/Reports/`
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
- [X] T100 Record quickstart.md validation log for scenarios exercised to date in `specs/001-potsure-v1/quickstart.md` (manual journeys may remain **Partial** until release QA). **Scenario 8b** (dependency hygiene): satisfied when **T110** last ran; re-run **T110** after **T111** adds packages. **Scenario 8c** (EF migration ordering): out of scope for T100 — close via **T115** after **T113**
- [X] T115 Re-run quickstart Scenario 8c (migration ordering: database healthy → migrations finished → web healthy) and refresh validation log in `specs/001-potsure-v1/quickstart.md` after T113; optionally verify hidden migration resource via `aspire describe --include-hidden`
- [X] T118 [P] Add reporting or integration test asserting variable recurring estimates expose coverage labelling when history is shorter than three months (SC-008) in `tests/Potsure.Recurrence.Tests/` or `tests/Potsure.Reporting.Tests/`; spot-check quickstart “Partial-month labelling” scenario
- [X] T110 Run `dotnet package list --outdated`, upgrade compatible versions in `Directory.Packages.props` where needed, then `dotnet test Potsure.slnx -c Release` (full suite, not `--no-build` only) after any package changes per constitution 1.2.0 and AGENTS.md
- [X] T101 Final `dotnet format Potsure.slnx --verify-no-changes`, Release build `-warnaserror`, and `dotnet test Potsure.slnx -c Release --no-build` per AGENTS.md PR workflow (re-run full test without `--no-build` when `Directory.Packages.props` changed since last build)

### Release evidence (constitution II / SC-007)

- [ ] T149 [P] Close Playwright E2E release evidence: `tests/Potsure.E2E/KeyJourneysTests.cs` exercises CSV upload, recurring confirmation, and export (not page-heading smoke only) when `POTSURE_E2E_BASE_URL` is set; supersedes partial closure notes on **T125**, **T128**, and **T133** for v1.0.0
- [ ] T150 [US12] Run quickstart Scenario 9 keyboard-only audit on category health, budget balance, and recurring summary; update validation log in `specs/001-potsure-v1/quickstart.md` (**SC-007**)
- [ ] T151 [P] Close **SC-003** release evidence: overlapping re-import scenarios (US2 AC7 / quickstart Scenario 2) demonstrate assignment memory without manual re-entry; non-partial integration coverage in **T106** / **T140**–**T142** or documented equivalent

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup** → **Phase 2 Foundational** → user story phases **3–14** → **Phase 15 Polish**
- **Phase 4 (US11)** can start after T021–T023 but should complete before hosted demo of later stories; **Phase 23 (T143–T148)** must complete for constitution 1.2.0 hosted acceptance after the Identity pivot
- **Phase 5 (US3)** can proceed in parallel with late US1 work once T017 profile entities exist (after T017)

### User Story Dependencies

| Story | Depends on | Notes |
|-------|------------|-------|
| US1 | Phase 2 | No other stories |
| US11 | Phase 2 T021–T023; **Phase 23** for pivot | Foundational auth; **T143–T148** supersede passkey-only **T022** / **T042** |
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

Suggested **MVP vertical slice** (demo only, not v1.0.0): Phase 1 → 2 → US1 → US3 → US2 → US6 → US7. **v1.0.0 release gate** (constitution v1.2.0 + spec P1): also US10, US11 (**Phase 23** hosted Identity pivot), FR-M037 (`T071`), hosted encryption verification (`T102`), Aspire SQL client consumption (`T107`–`T108`, re-smoke after **T144**–**T146**), EF migration orchestration (`T111`–`T114`, `T116`), dependency hygiene (`T110` after any package add including `T111`), migration orchestration validation (`T115`), Playwright E2E closure (`T149`; implementation partial on **T125**, **T128**, **T133**), **SC-003** closure (`T151`), moderated usability for **SC-001** / **SC-002** / **SC-009** (release QA — no CI tasks), and quickstart validation (`T100`, **T148** for hosted register/sign-in, **T150** for keyboard audit).

### Parallel Opportunities

- Phase 1: T004, T005, T006, T009 in parallel
- Phase 2: T016 parallel with T017–T20 after T012–T015 interfaces stable
- US1: T029–T032 parallel before T037 pipeline
- US10: T085, T086 parallel
- US12: T091, T092 parallel
- After Phase 2, teams may parallelise **US4+US5** while another works **US7** once US2+US3 land
- **Phase 23**: **T143** and **T146** touch Identity registration and pages — sequence before **T144**/**T145**; **T148** can run in parallel once **T143**–**T147** land

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

Before tagging v1.0.0, also complete Phase 4 infrastructure tasks (`T102`, `T043`–`T045`; **not** US11 acceptance without **Phase 23**), **Phase 23** (`T143`–`T148`), Phase 13 (US10; hosted reauth via **T147** supersedes passkey **T132** / **T136**), one-offs (`T071`), Phase 2 Aspire consumption (`T107`–`T108`) and migration orchestration (`T111`–`T114`, `T116`), US2 **FR-S004** copy (`T117`), Phase 15 (`T100`–`T101`, `T110`, `T115`, `T118`, **T149**–**T151**), and remaining P1/P2 stories as scheduled in spec.

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
- EF schema apply: AppHost `AddEFMigrations` + `WaitForCompletion` on web — not `MigrateAsync` at web startup; app seeding (`BudgetProfileSeeder`) stays in web/runtime, not the migration job (plan.md EF migrations architecture; [data-model.md](./data-model.md) schema evolution)
- Hosted schema: `PublishAsMigrationScript()` / controlled job (`T116`) — not `RunDatabaseUpdateOnStart` on publish
- **Hosted Identity pivot (2026-11-04)**: passkey-only tasks **T022**, **T042**, **T132**, **T136** are **unchecked** and **superseded**; implement **T143**–**T148** before re-asserting SC-005 / hosted quickstart acceptance
- Optional WebAuthn passkeys: deferred post–v1.0.0 (template-style optional submit), not mandatory sign-in
- Retire placeholder `src/Potsure/` when T002 completes
- **FR-S002** (transaction-level actual-spend drill-down) is Should-level; deferred unless explicitly pulled into scope — category health (`T073`) covers aggregate actuals only

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
- [ ] T132 [US10] Replace typed phrase reauthentication with hosted passkey step-up (Identity) before export and permanent deletion when deployment mode is hosted; keep an appropriate local-only challenge per `research.md`, T026, and `plan.md` reauthentication notes (partial) — **Superseded 2026-11-04** (passkey step-up not required for v1); **replaced by T147** (not acceptance evidence while unchecked)
- [X] T133 [P] [US12] Extend `tests/Potsure.E2E/KeyJourneysTests.cs` to confirm at least one suggested recurring pattern in the UI (not only navigate to review) before category health and export when `POTSURE_E2E_BASE_URL` is set per Constitution II and T128 (partial)

---

## Phase 19: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-03, post Phase 18) between spec intent and present codebase behaviour.

- [X] T134 [US1] Persist the user-confirmed column map from `ImportWizard` / `StatementImportService.ImportAsync` to `BudgetProfile.RememberedColumnMappingJson` and pre-fill mapping on subsequent uploads when the file header layout is unchanged per FR-M002, US1/AC1, and T038 (partial)
- [X] T135 [US2] Add optional mapped bank pot and free-text tag inputs on `DetectionReview` for expense confirm, merge, and split; when merging confirmed patterns with differing category, pot, or tag, require explicit resolution (and validate in `PatternMergeSplitService`) before save per FR-M008, FR-M009, and US2 acceptance scenarios 3, 5, and 6 (partial)
- [ ] T136 [US10] Wire hosted export and permanent deletion reauthentication to an Identity passkey step-up challenge (not self-attestation checkbox only) before granting `ReauthenticationService` per T132, `research.md`, and `plan.md` reauthentication notes (partial) — **Superseded 2026-11-04**; **replaced by T147** (not acceptance evidence while unchecked)

---

## Phase 20: Convergence

**Purpose**: Close gaps found by `/speckit-converge` (2026-11-03, post Phase 19) between spec intent and present codebase behaviour.

- [X] T137 [US2] Add UI to reassign individual imported transactions (including auto-attributed debits) via `TransactionAttributionService.OverrideAttributionAsync`, with optional pot and pattern link, and navigation from category health or a dedicated review list per FR-M010a and US2 acceptance scenario 7 (missing)
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

**Purpose**: Align implemented hosted auth with constitution 1.2.0 and amended spec — standard Identity email/password, Blazor account template + BlazorBlueprint, no passkey-only login or passkey step-up for v1 acceptance. Does **not** reopen completed import, budgeting, or report tasks.

**Goal**: Hosted users register and sign in with email/password; export/delete reauthenticate without passkeys; Aspire Hosted mode smoke-validates register/login.

**Independent Test**: AppHost with `Deployment__Mode=Hosted`; register a user, sign in, reach a gated page; export/delete prompts password reauth (not passkey); quickstart hosted Identity notes satisfied (**T148**).

**Post-Phase-23 smoke**: Re-run quickstart Scenario 8c (migration ordering) and Aspire SQL consumption smoke after **T144**–**T146** change `Program.cs` (constitution 1.2.0).

- [ ] T143 [US11] Scaffold .NET 10 Blazor Server Identity account pages under `src/Potsure.Web/Components/Account/Pages/` (Login, Register, ForgotPassword, Manage, and related shared components) with `IdentityRedirectManager` and `IdentityRevalidatingAuthenticationStateProvider`; convert template markup to BlazorBlueprint v4 (`BbCard`, `BbButton`, `BbInput`, `BbAlert`, Lucide per house patterns) per [plan.md](./plan.md), [research.md](./research.md), and `blazor-blueprint` skill
- [ ] T144 [US11] Call `MapAdditionalIdentityEndpoints()` from `src/Potsure.Web/Program.cs` and align account routes with template paths; register no-op or logging `IEmailSender<ApplicationUser>` for v1 self-host per [research.md](./research.md)
- [ ] T145 [US11] Remove or replace `src/Potsure.Web/Configuration/HostedPasskeySignInEndpoints.cs`, `src/Potsure.Web/Configuration/HostedPasskeyStepUpEndpoints.cs`, and passkey-only `src/Potsure.Web/Components/Account/PasskeySignIn.razor`; update `src/Potsure.Web/Components/Account/RedirectToLogin.razor` and `Program.cs` to use template sign-in/register flows instead of `MapHostedPasskeySignInEndpoints` / `MapHostedPasskeyStepUpEndpoints`
- [ ] T146 [US11] Refactor `src/Potsure.Web/Configuration/HostedIdentityConfiguration.cs` for standard email/password Identity (normal `IdentityOptions.Password` policy per constitution 1.2.0 — not passkey-style 64-character minimum); rename `AddHostedPasskeyIdentity` to hosted Identity registration used from `Program.cs`; defer mandatory passkey handler wiring to post–v1.0.0 optional milestone
- [ ] T147 [US10] Replace hosted passkey step-up in `src/Potsure.Web/Components/Privacy/PrivacyDataPanel.razor.cs` with current-password confirmation (preferred) or typed-phrase fallback per amended FR-M022/FR-M023 and [plan.md](./plan.md) reauthentication notes; ensure `ReauthenticationService` grants export/delete without passkey step-up
- [ ] T148 [P] [US11] Add smoke coverage for hosted register and sign-in under Aspire Hosted mode (`Deployment__Mode=Hosted` from AppHost) in `tests/Potsure.Web.Tests/` or `tests/Potsure.E2E/` and record the exercised path in `specs/001-potsure-v1/quickstart.md` (email/password — passkeys not required for acceptance)
