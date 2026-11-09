using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Input for runway projection including variable estimates (FR-M020, FR-M034).
/// </summary>
public readonly record struct RunwayProjectionInput(
    AllocationPreset Preset,
    DateOnly ReportStart,
    DateOnly ReportEnd,
    int ForecastHorizonMonths,
    PlanIncome? PlanIncome,
    IReadOnlyList<CategoryFundingInput> Categories,
    IReadOnlyList<CategoryActualInput> Actuals,
    Money UncategorisedSpend,
    IReadOnlyList<CommitmentExpenseInput> Expenses);

/// <summary>
/// Projected remaining spend per category over the forecast horizon.
/// </summary>
public readonly record struct RunwayProjectionLine(
    BudgetCategoryKind Category,
    Money ProjectedRemainingSpend,
    Money Threshold,
    bool IsShortfall);

/// <summary>
/// Computes runway warnings using confirmed commitments and variable estimates.
/// </summary>
public static class RunwayProjector
{
    /// <summary>
    /// Projects spend through the forecast horizon and compares to funding thresholds.
    /// </summary>
    public static IReadOnlyList<RunwayProjectionLine> Project(RunwayProjectionInput input)
    {
        DateRange reportRange = new(input.ReportStart, input.ReportEnd);
        DateRange runwayWindow = new(
            reportRange.Start,
            ProrationCalculator.RunwayWindowEnd(reportRange.End, input.ForecastHorizonMonths));

        IReadOnlyList<CategoryCommitmentInput> horizonCommitments = CommitmentProjector.ProjectIntoPeriod(
            runwayWindow.Start,
            runwayWindow.End,
            input.Expenses);

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

        BudgetingContext context = new(
            input.Preset,
            input.PlanIncome,
            reportRange,
            runwayWindow,
            categoryInputs,
            potInputs,
            horizonCommitments,
            input.Actuals,
            input.UncategorisedSpend,
            input.ForecastHorizonMonths);

        Dictionary<BudgetCategoryKind, Money> projected = horizonCommitments
            .ToDictionary(c => c.Category, c => c.AmountInRange);

        IReadOnlyList<RunwayWarning> warnings = RunwayCalculator.Calculate(context, projected);
        return warnings
            .Select(w => new RunwayProjectionLine(
                w.Category,
                w.ProjectedRemainingSpend,
                w.Threshold,
                w.IsShortfall))
            .ToList();
    }
}
