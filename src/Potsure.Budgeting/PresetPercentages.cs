namespace Potsure.Budgeting;

internal static class PresetPercentages
{
    public static decimal For(BudgetCategoryKind kind, AllocationPreset preset) =>
        (kind, preset) switch
        {
            (BudgetCategoryKind.Needs, AllocationPreset.FiftyThirtyTwenty) => 0.50m,
            (BudgetCategoryKind.Wants, AllocationPreset.FiftyThirtyTwenty) => 0.30m,
            (BudgetCategoryKind.SavingsAndDebt, AllocationPreset.FiftyThirtyTwenty) => 0.20m,
            (BudgetCategoryKind.Needs, AllocationPreset.SeventyFiveFifteenTen) => 0.75m,
            (BudgetCategoryKind.Wants, AllocationPreset.SeventyFiveFifteenTen) => 0.15m,
            (BudgetCategoryKind.SavingsAndDebt, AllocationPreset.SeventyFiveFifteenTen) => 0.10m,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    public static IReadOnlyList<BudgetCategoryKind> TieBreakOrder { get; } =
    [
        BudgetCategoryKind.Needs,
        BudgetCategoryKind.Wants,
        BudgetCategoryKind.SavingsAndDebt,
    ];
}
