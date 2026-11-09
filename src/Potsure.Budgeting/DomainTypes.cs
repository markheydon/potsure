namespace Potsure.Budgeting;

/// <summary>
/// Shared budgeting and import domain types for Potsure v1.
/// </summary>
public enum BudgetCategoryKind
{
    Needs,
    Wants,
    SavingsAndDebt,
}

public enum AllocationPreset
{
    FiftyThirtyTwenty,
    SeventyFiveFifteenTen,
}

public enum TransactionKind
{
    ExpenseDebit,
    IncomeCredit,
    RefundCredit,
    InternalTransfer,
    PotMovement,
    Unknown,
}

public enum PatternConfirmationState
{
    Suggested,
    Confirmed,
    Rejected,
}

public enum IncomeCadence
{
    Weekly,
    Fortnightly,
    Monthly,
    Variable,
    AdHoc,
}

public enum ExpenseCadence
{
    Weekly,
    Fortnightly,
    Monthly,
    Variable,
    AnnualRare,
}

public enum PotMappingMode
{
    CategoryOnly,
    ThreePots,
    SeveralPots,
}

public enum PlanIncomeCadence
{
    Weekly,
    Fortnightly,
    Monthly,
    Quarterly,
    Yearly,
}

public enum ExpenseAmountKind
{
    Fixed,
    Variable,
}

public readonly record struct DateRange(DateOnly Start, DateOnly End);

public readonly record struct PlanIncome(Money EstimatedAmount, PlanIncomeCadence Cadence);

public readonly record struct ExpenseCommitmentInput(
    BudgetCategoryKind Category,
    ExpenseCadence Cadence,
    ExpenseAmountKind AmountKind,
    Money? FixedAmountPerOccurrence,
    DateOnly? PaymentAnchorDate,
    Money? StoredMonthlyEstimate);

public readonly record struct CategoryBudgetInput(
    BudgetCategoryKind Kind,
    Money RecordedFundingDirect,
    Money? PlannedFutureFundingDirect = null);

public readonly record struct PotBudgetInput(
    BudgetCategoryKind CategoryKind,
    Money RecordedFunding,
    Money? AllocationToPot = null);

public readonly record struct CategoryCommitmentInput(
    BudgetCategoryKind Category,
    Money AmountInRange);

public readonly record struct CategoryActualInput(
    BudgetCategoryKind Category,
    Money NetActivity);

public readonly record struct BudgetingContext(
    AllocationPreset Preset,
    PlanIncome? PlanIncome,
    DateRange ReportRange,
    DateRange RunwayWindow,
    IReadOnlyList<CategoryBudgetInput> Categories,
    IReadOnlyList<PotBudgetInput> Pots,
    IReadOnlyList<CategoryCommitmentInput> Commitments,
    IReadOnlyList<CategoryActualInput> Actuals,
    Money UncategorisedSpend,
    int ForecastHorizonMonths);

public readonly record struct CategoryHealthLine(
    BudgetCategoryKind Kind,
    Money? Allocation,
    Money RecordedFunding,
    Money ActualActivity,
    Money? UnderOver,
    bool OverAllocationWarning,
    bool NotSet);

public readonly record struct RequiredIncomeResult(
    IReadOnlyDictionary<BudgetCategoryKind, Money> RequiredByCategory,
    Money RequiredTotal,
    BudgetCategoryKind LimitingCategory,
    Money? EstimatedIncomeInRange,
    Money? Shortfall,
    bool? MeetsRequirement,
    bool NotSet);

public readonly record struct RunwayWarning(
    BudgetCategoryKind Category,
    Money ProjectedRemainingSpend,
    Money Threshold,
    bool IsShortfall);
