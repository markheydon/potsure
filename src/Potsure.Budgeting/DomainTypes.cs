namespace Potsure.Budgeting;

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
    None,
    CategoryNamedPots,
    ExpenseSpecificPots,
}

public readonly record struct DateRange(DateOnly Start, DateOnly End);

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
    Money AmountInPeriod);

public readonly record struct CategoryActualInput(
    BudgetCategoryKind Category,
    Money NetActivity);

public readonly record struct BudgetingContext(
    AllocationPreset Preset,
    DateRange Period,
    Money BudgetedIncome,
    IReadOnlyList<CategoryBudgetInput> Categories,
    IReadOnlyList<PotBudgetInput> Pots,
    IReadOnlyList<CategoryCommitmentInput> Commitments,
    IReadOnlyList<CategoryActualInput> Actuals,
    Money UncategorisedSpend,
    DateOnly ForecastHorizonEnd);

public readonly record struct CategoryHealthLine(
    BudgetCategoryKind Kind,
    Money Allocation,
    Money RecordedFunding,
    Money ActualActivity,
    Money UnderOver,
    bool OverAllocationWarning);

public readonly record struct RequiredIncomeResult(
    IReadOnlyDictionary<BudgetCategoryKind, Money> RequiredByCategory,
    Money RequiredTotal,
    BudgetCategoryKind LimitingCategory,
    Money Shortfall,
    bool MeetsRequirement);

public readonly record struct RunwayWarning(
    BudgetCategoryKind Category,
    Money ProjectedRemainingSpend,
    Money Threshold,
    bool IsShortfall);
