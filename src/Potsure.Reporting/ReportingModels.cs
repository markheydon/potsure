using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// A transaction row scoped to period reporting and attribution.
/// </summary>
public readonly record struct PeriodTransactionInput(
    Guid Id,
    DateOnly TransactionDate,
    TransactionKind Kind,
    decimal SignedAmount,
    BudgetCategoryKind? AttributedCategory);

/// <summary>
/// Category-level funding inputs for reporting rollups.
/// </summary>
public readonly record struct CategoryFundingInput(
    BudgetCategoryKind Kind,
    decimal RecordedFundingDirect,
    IReadOnlyList<PotFundingInput> Pots);

/// <summary>
/// Pot-level funding and optional allocation slice within a category.
/// </summary>
public readonly record struct PotFundingInput(
    decimal? AllocationToPot,
    decimal RecordedFunding);

/// <summary>
/// Inputs required to assemble a period summary report.
/// </summary>
public readonly record struct PeriodSummaryInput(
    AllocationPreset Preset,
    DateOnly StartDate,
    DateOnly EndDate,
    PlanIncome? PlanIncome,
    decimal ActualIncomeInPeriod,
    int ForecastHorizonMonths,
    IReadOnlyList<CategoryFundingInput> Categories,
    IReadOnlyList<PeriodTransactionInput> Transactions,
    IReadOnlyList<CategoryCommitmentInput> Commitments);

/// <summary>
/// Per-category measures for a period summary.
/// </summary>
public readonly record struct CategoryPeriodSummary(
    BudgetCategoryKind Kind,
    Money? Allocation,
    Money RecordedFunding,
    Money ActualActivity,
    Money? UnderOver,
    bool OverAllocationWarning,
    bool NotSet);

/// <summary>
/// Computed period report DTO (not persisted).
/// </summary>
public readonly record struct PeriodSummary(
    DateRange ReportRange,
    Money? EstimatedIncomeInRange,
    bool PlanIncomeNotSet,
    Money ActualIncomeInPeriod,
    IReadOnlyList<CategoryPeriodSummary> Categories,
    Money UncategorisedSpend,
    bool UncategorisedSpendWarning);
