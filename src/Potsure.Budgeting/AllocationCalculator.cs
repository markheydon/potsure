namespace Potsure.Budgeting;

/// <summary>
/// Computes category allocations from budgeted income and preset percentages.
/// </summary>
public static class AllocationCalculator
{
    public static IReadOnlyDictionary<BudgetCategoryKind, Money> Calculate(
        AllocationPreset preset,
        Money budgetedIncome)
    {
        Dictionary<BudgetCategoryKind, Money> result = new();
        foreach (BudgetCategoryKind kind in PresetPercentages.TieBreakOrder)
        {
            decimal percent = PresetPercentages.For(kind, preset);
            result[kind] = (budgetedIncome * percent).RoundForReport();
        }

        return result;
    }
}
