using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Uncertainty flags for statement gaps and uncertain income (FR-M032).
/// </summary>
public readonly record struct UncertaintyFlags(
    bool HasStatementGap,
    bool HasUncertainIncome,
    IReadOnlyList<string> Messages);

/// <summary>
/// Builds disclosure flags for runway and forecast views.
/// </summary>
public static class UncertaintyFlagsBuilder
{
    /// <summary>
    /// Evaluates coverage gaps and income estimate uncertainty.
    /// </summary>
    public static UncertaintyFlags Build(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyList<DateOnly> statementCoverageDays,
        IReadOnlyList<IncomeScheduleInput> incomeSchedules)
    {
        List<string> messages = [];
        bool gap = HasCoverageGap(periodStart, periodEnd, statementCoverageDays);
        if (gap)
        {
            messages.Add("Statement history has gaps inside the selected period; forecasts may be understated.");
        }

        bool uncertainIncome = incomeSchedules.Any(i =>
            i.Cadence is IncomeCadence.Variable or IncomeCadence.AdHoc || i.FixedAmount is null or <= 0m);
        if (uncertainIncome)
        {
            messages.Add("One or more income patterns use estimates or irregular cadence.");
        }

        return new UncertaintyFlags(gap, uncertainIncome, messages);
    }

    private static bool HasCoverageGap(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyList<DateOnly> statementCoverageDays)
    {
        if (statementCoverageDays.Count == 0)
        {
            return true;
        }

        HashSet<DateOnly> coverage = statementCoverageDays.ToHashSet();
        for (DateOnly day = periodStart; day <= periodEnd; day = day.AddDays(1))
        {
            if (!coverage.Contains(day))
            {
                return true;
            }
        }

        return false;
    }
}
