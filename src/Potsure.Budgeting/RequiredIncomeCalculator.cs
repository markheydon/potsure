namespace Potsure.Budgeting;

/// <summary>
/// Required income from confirmed recurring commitments and preset ratios.
/// </summary>
public static class RequiredIncomeCalculator
{
    public static RequiredIncomeResult Calculate(BudgetingContext context)
    {
        Dictionary<BudgetCategoryKind, Money> requiredByCategory = new();
        foreach (BudgetCategoryKind kind in PresetPercentages.TieBreakOrder)
        {
            Money commitments = context.Commitments
                .Where(c => c.Category == kind)
                .Aggregate(Money.Zero, (sum, c) => sum + c.AmountInPeriod)
                .RoundForReport();
            decimal percent = PresetPercentages.For(kind, context.Preset);
            requiredByCategory[kind] = percent == 0m
                ? Money.Zero
                : (commitments / percent).RoundForReport();
        }

        Money maxRequired = Money.Zero;
        BudgetCategoryKind limiting = BudgetCategoryKind.Needs;
        foreach (BudgetCategoryKind kind in PresetPercentages.TieBreakOrder)
        {
            Money required = requiredByCategory[kind];
            if (required > maxRequired)
            {
                maxRequired = required;
                limiting = kind;
            }
        }

        Money requiredTotal = maxRequired.RoundForReport();
        Money shortfall = (requiredTotal - context.BudgetedIncome).Amount > 0m
            ? (requiredTotal - context.BudgetedIncome).RoundForReport()
            : Money.Zero;
        bool meets = context.BudgetedIncome >= requiredTotal;

        return new RequiredIncomeResult(
            requiredByCategory,
            requiredTotal,
            limiting,
            shortfall,
            meets);
    }
}
