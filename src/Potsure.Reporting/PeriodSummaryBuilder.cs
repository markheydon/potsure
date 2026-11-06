using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Assembles <see cref="PeriodSummary"/> using budgeting calculators and reporting inputs.
/// </summary>
public static class PeriodSummaryBuilder
{
    /// <summary>
    /// Builds a deterministic period summary from the supplied inputs.
    /// </summary>
    public static PeriodSummary Build(PeriodSummaryInput input)
    {
        DateRange period = new(input.StartDate, input.EndDate);
        Money budgetedIncome = Money.FromRaw(input.BudgetedIncomeAmount).RoundForReport();
        Money actualIncome = Money.FromRaw(input.ActualIncomeInPeriod).RoundForReport();

        IReadOnlyList<CategoryActualInput> actuals = ActualActivityCalculator.Calculate(
            input.StartDate,
            input.EndDate,
            input.Transactions);
        Money uncategorised = UncategorisedSpendCalculator.Calculate(
            input.StartDate,
            input.EndDate,
            input.Transactions);

        IReadOnlyDictionary<BudgetCategoryKind, bool> overAllocation =
            OverAllocationEvaluator.Evaluate(input.Preset, budgetedIncome, input.Categories);

        List<CategoryBudgetInput> categoryInputs = input.Categories
            .Select(c => new CategoryBudgetInput(
                c.Kind,
                Money.FromRaw(c.RecordedFundingDirect).RoundForReport()))
            .ToList();

        List<PotBudgetInput> potInputs = input.Categories
            .SelectMany(c => c.Pots.Select(p => new PotBudgetInput(
                c.Kind,
                Money.FromRaw(p.RecordedFunding).RoundForReport(),
                p.AllocationToPot is null ? null : Money.FromRaw(p.AllocationToPot.Value).RoundForReport())))
            .ToList();

        DateOnly forecastEnd = input.EndDate.AddMonths(input.ForecastHorizonMonths);

        BudgetingContext context = new(
            input.Preset,
            period,
            budgetedIncome,
            categoryInputs,
            potInputs,
            input.Commitments,
            actuals,
            uncategorised,
            forecastEnd);

        IReadOnlyList<CategoryHealthLine> healthLines = CategoryHealthCalculator.Calculate(context);

        List<CategoryPeriodSummary> categories = healthLines
            .Select(line => new CategoryPeriodSummary(
                line.Kind,
                line.Allocation,
                line.RecordedFunding,
                line.ActualActivity,
                line.UnderOver,
                overAllocation.TryGetValue(line.Kind, out bool over) ? over : line.OverAllocationWarning))
            .ToList();

        return new PeriodSummary(
            period,
            budgetedIncome,
            actualIncome,
            categories,
            uncategorised,
            uncategorised.Amount > 0m);
    }
}
