namespace Potsure.Budgeting;

/// <summary>
/// Computes category allocations from budgeted income and preset percentages.
/// </summary>
public static class AllocationCalculator
{
    public static IReadOnlyDictionary<BudgetCategoryKind, Money> CalculateMonthlyEquivalent(
        AllocationPreset preset,
        Money monthlyEquivalentIncome)
    {
        Dictionary<BudgetCategoryKind, Money> result = new();
        foreach (BudgetCategoryKind kind in PresetPercentages.TieBreakOrder)
        {
            decimal percent = PresetPercentages.For(kind, preset);
            result[kind] = (monthlyEquivalentIncome * percent).RoundForReport();
        }

        return result;
    }

    public static IReadOnlyDictionary<BudgetCategoryKind, Money> CalculateForReportRange(
        AllocationPreset preset,
        PlanIncome? planIncome,
        DateRange reportRange)
    {
        if (planIncome is null)
        {
            return new Dictionary<BudgetCategoryKind, Money>();
        }

        Money monthlyEquivalent = ProrationCalculator.MonthlyEquivalentFromPlan(planIncome.Value);
        IReadOnlyDictionary<BudgetCategoryKind, Money> monthly =
            CalculateMonthlyEquivalent(preset, monthlyEquivalent);

        Dictionary<BudgetCategoryKind, Money> prorated = new();
        foreach (BudgetCategoryKind kind in PresetPercentages.TieBreakOrder)
        {
            prorated[kind] = ProrationCalculator.ProrateMonthlyEquivalent(
                monthly[kind],
                reportRange.Start,
                reportRange.End);
        }

        return prorated;
    }
}
