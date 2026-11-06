namespace Potsure.Budgeting;

/// <summary>
/// Category health lines: under/over excludes recorded funding from the formula.
/// </summary>
public static class CategoryHealthCalculator
{
    public static IReadOnlyList<CategoryHealthLine> Calculate(BudgetingContext context)
    {
        IReadOnlyDictionary<BudgetCategoryKind, Money> allocations =
            AllocationCalculator.Calculate(context.Preset, context.BudgetedIncome);

        List<CategoryHealthLine> lines = new();
        foreach (BudgetCategoryKind kind in PresetPercentages.TieBreakOrder)
        {
            CategoryBudgetInput category = context.Categories.Single(c => c.Kind == kind);
            Money potRecorded = context.Pots
                .Where(p => p.CategoryKind == kind)
                .Aggregate(Money.Zero, (sum, p) => sum + p.RecordedFunding);
            Money potAllocations = context.Pots
                .Where(p => p.CategoryKind == kind && p.AllocationToPot is not null)
                .Aggregate(Money.Zero, (sum, p) => sum + p.AllocationToPot!.Value);

            Money recordedFunding = (category.RecordedFundingDirect + potRecorded).RoundForReport();
            Money allocation = allocations[kind];
            Money actual = context.Actuals
                .Where(a => a.Category == kind)
                .Aggregate(Money.Zero, (sum, a) => sum + a.NetActivity)
                .RoundForReport();
            Money underOver = (allocation - actual).RoundForReport();
            bool overAllocation = (potAllocations + category.RecordedFundingDirect) > allocation;

            lines.Add(new CategoryHealthLine(
                kind,
                allocation,
                recordedFunding,
                actual,
                underOver,
                overAllocation));
        }

        return lines;
    }
}
