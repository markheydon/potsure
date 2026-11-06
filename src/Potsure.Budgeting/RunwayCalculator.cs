namespace Potsure.Budgeting;

/// <summary>
/// Runway warnings when projected spend exceeds allocation minus actual plus recorded funding.
/// </summary>
public static class RunwayCalculator
{
    public static IReadOnlyList<RunwayWarning> Calculate(
        BudgetingContext context,
        IReadOnlyDictionary<BudgetCategoryKind, Money> projectedRemainingByCategory)
    {
        IReadOnlyList<CategoryHealthLine> health = CategoryHealthCalculator.Calculate(context);
        List<RunwayWarning> warnings = new();

        foreach (CategoryHealthLine line in health)
        {
            if (!projectedRemainingByCategory.TryGetValue(line.Kind, out Money projected))
            {
                projected = Money.Zero;
            }

            Money threshold = (line.Allocation - line.ActualActivity + line.RecordedFunding).RoundForReport();
            bool shortfall = projected > threshold;
            warnings.Add(new RunwayWarning(line.Kind, projected.RoundForReport(), threshold, shortfall));
        }

        return warnings;
    }
}
