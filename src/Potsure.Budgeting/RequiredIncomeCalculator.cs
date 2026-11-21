namespace Potsure.Budgeting;

/// <summary>
/// Required income from confirmed recurring commitments and preset ratios (max-of-ratios).
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
                .Aggregate(Money.Zero, (sum, c) => sum + c.AmountInRange)
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

        if (context.PlanIncome is null)
        {
            return new RequiredIncomeResult(
                requiredByCategory,
                requiredTotal,
                limiting,
                null,
                null,
                null,
                true);
        }

        Money monthlyEquivalent = ProrationCalculator.MonthlyEquivalentFromPlan(context.PlanIncome.Value);
        Money estimatedInRange = ProrationCalculator.ProrateMonthlyEquivalent(
            monthlyEquivalent,
            context.ReportRange.Start,
            context.ReportRange.End);

        Money shortfall = estimatedInRange < requiredTotal
            ? (requiredTotal - estimatedInRange).RoundForReport()
            : Money.Zero;
        bool meets = estimatedInRange >= requiredTotal;

        return new RequiredIncomeResult(
            requiredByCategory,
            requiredTotal,
            limiting,
            estimatedInRange,
            shortfall,
            meets,
            false);
    }
}
