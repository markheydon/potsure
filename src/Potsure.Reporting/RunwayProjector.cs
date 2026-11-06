using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Input for runway projection including variable estimates (FR-M020, FR-M034).
/// </summary>
public readonly record struct RunwayProjectionInput(
    AllocationPreset Preset,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int ForecastHorizonMonths,
    decimal BudgetedIncomeAmount,
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
        DateOnly horizonEnd = input.PeriodEnd.AddMonths(input.ForecastHorizonMonths);
        IReadOnlyList<CategoryCommitmentInput> horizonCommitments = CommitmentProjector.ProjectIntoPeriod(
            input.PeriodStart,
            horizonEnd,
            input.Expenses);

        Money budgetedIncome = Money.FromRaw(input.BudgetedIncomeAmount).RoundForReport();
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
            new DateRange(input.PeriodStart, input.PeriodEnd),
            budgetedIncome,
            categoryInputs,
            potInputs,
            horizonCommitments,
            input.Actuals,
            input.UncategorisedSpend,
            horizonEnd);

        Dictionary<BudgetCategoryKind, Money> projected = horizonCommitments
            .ToDictionary(c => c.Category, c => c.AmountInPeriod);

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
