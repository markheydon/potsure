using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Detects when recorded funding exceeds category allocation (FR-M017a).
/// </summary>
public static class OverAllocationEvaluator
{
    /// <summary>
    /// Returns whether each category has mapped-pot slices plus direct recorded funding above its prorated allocation.
    /// </summary>
    public static IReadOnlyDictionary<BudgetCategoryKind, bool> Evaluate(
        AllocationPreset preset,
        PlanIncome planIncome,
        DateRange reportRange,
        IReadOnlyList<CategoryFundingInput> categories)
    {
        IReadOnlyDictionary<BudgetCategoryKind, Money> allocations =
            AllocationCalculator.CalculateForReportRange(preset, planIncome, reportRange);

        Dictionary<BudgetCategoryKind, bool> results = new();
        foreach (CategoryFundingInput category in categories)
        {
            Money allocation = allocations[category.Kind];
            Money potAllocations = category.Pots
                .Where(p => p.AllocationToPot is not null)
                .Aggregate(Money.Zero, (sum, p) => sum + Money.FromRaw(p.AllocationToPot!.Value))
                .RoundForReport();
            Money directRecorded = Money.FromRaw(category.RecordedFundingDirect).RoundForReport();
            bool over = (potAllocations + directRecorded) > allocation;
            results[category.Kind] = over;
        }

        return results;
    }
}
