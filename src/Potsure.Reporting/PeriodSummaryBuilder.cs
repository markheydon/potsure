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
        DateRange reportRange = new(input.StartDate, input.EndDate);
        Money actualIncome = Money.FromRaw(input.ActualIncomeInPeriod).RoundForReport();
        bool notSet = input.PlanIncome is null;

        Money? estimatedInRange = notSet
            ? null
            : ProrationCalculator.ProrateMonthlyEquivalent(
                ProrationCalculator.MonthlyEquivalentFromPlan(input.PlanIncome!.Value),
                reportRange.Start,
                reportRange.End);

        IReadOnlyList<CategoryActualInput> actuals = ActualActivityCalculator.Calculate(
            input.StartDate,
            input.EndDate,
            input.Transactions);
        Money uncategorised = UncategorisedSpendCalculator.Calculate(
            input.StartDate,
            input.EndDate,
            input.Transactions);

        IReadOnlyDictionary<BudgetCategoryKind, bool> overAllocation = notSet
            ? new Dictionary<BudgetCategoryKind, bool>()
            : OverAllocationEvaluator.Evaluate(
                input.Preset,
                input.PlanIncome!.Value,
                reportRange,
                input.Categories);

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

        DateRange runwayWindow = new(
            reportRange.Start,
            ProrationCalculator.RunwayWindowEnd(reportRange.End, input.ForecastHorizonMonths));

        BudgetingContext context = new(
            input.Preset,
            input.PlanIncome,
            reportRange,
            runwayWindow,
            categoryInputs,
            potInputs,
            input.Commitments,
            actuals,
            uncategorised,
            input.ForecastHorizonMonths);

        IReadOnlyList<CategoryHealthLine> healthLines = CategoryHealthCalculator.Calculate(context);

        List<CategoryPeriodSummary> categories = healthLines
            .Select(line => new CategoryPeriodSummary(
                line.Kind,
                line.Allocation,
                line.RecordedFunding,
                line.ActualActivity,
                line.UnderOver,
                overAllocation.TryGetValue(line.Kind, out bool over) ? over : line.OverAllocationWarning,
                line.NotSet))
            .ToList();

        return new PeriodSummary(
            reportRange,
            estimatedInRange,
            notSet,
            actualIncome,
            categories,
            uncategorised,
            uncategorised.Amount > 0m);
    }
}
