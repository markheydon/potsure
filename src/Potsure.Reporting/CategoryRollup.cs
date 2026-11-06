using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Rolls pot recorded funding and allocations into category totals once (FR-M017).
/// </summary>
public readonly record struct CategoryRollupLine(
    BudgetCategoryKind Kind,
    Money Allocation,
    Money RecordedFundingDirect,
    Money PotRecordedFunding,
    Money PotAllocations,
    Money TotalRecordedFunding);

/// <summary>
/// Aggregates category and pot funding without double-counting amounts.
/// </summary>
public static class CategoryRollup
{
    /// <summary>
    /// Combines direct and pot recorded funding for each category line.
    /// </summary>
    public static IReadOnlyList<CategoryRollupLine> RollUp(
        AllocationPreset preset,
        Money budgetedIncome,
        IReadOnlyList<CategoryFundingInput> categories)
    {
        IReadOnlyDictionary<BudgetCategoryKind, Money> allocations =
            AllocationCalculator.Calculate(preset, budgetedIncome);

        List<CategoryRollupLine> lines = new();
        BudgetCategoryKind[] order =
        [
            BudgetCategoryKind.Needs,
            BudgetCategoryKind.Wants,
            BudgetCategoryKind.SavingsAndDebt,
        ];

        foreach (BudgetCategoryKind kind in order)
        {
            CategoryFundingInput category = categories.Single(c => c.Kind == kind);
            Money potRecorded = category.Pots
                .Aggregate(Money.Zero, (sum, p) => sum + Money.FromRaw(p.RecordedFunding).RoundForReport());
            Money potAllocations = category.Pots
                .Where(p => p.AllocationToPot is not null)
                .Aggregate(Money.Zero, (sum, p) => sum + Money.FromRaw(p.AllocationToPot!.Value).RoundForReport());

            Money direct = Money.FromRaw(category.RecordedFundingDirect).RoundForReport();
            lines.Add(new CategoryRollupLine(
                kind,
                allocations[kind],
                direct,
                potRecorded,
                potAllocations,
                (direct + potRecorded).RoundForReport()));
        }

        return lines;
    }
}
