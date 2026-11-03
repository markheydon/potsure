using System.Globalization;

namespace Potsure.Recurrence;

/// <summary>
/// Estimates monthly totals for variable recurring expenses with partial-month coverage labels (FR-M034).
/// </summary>
public static class VariableExpenseEstimator
{
    private const int MaxMonthsWindow = 3;

    /// <summary>
    /// Computes a monthly estimate from calendar-month totals and statement coverage days.
    /// Missing coverage days are treated as unknown spend, not zero.
    /// </summary>
    public static VariableExpenseEstimateResult Estimate(
        IReadOnlyList<(DateOnly Date, decimal Amount)> debits,
        IReadOnlyList<DateOnly> statementCoverageDays,
        DateOnly asOfDate)
    {
        if (debits.Count == 0 || statementCoverageDays.Count == 0)
        {
            return new VariableExpenseEstimateResult(
                0m,
                "No statement coverage for estimate.",
                HasIncompleteMonths: true);
        }

        HashSet<DateOnly> coverage = statementCoverageDays.ToHashSet();
        List<MonthAggregate> months = BuildRecentMonths(debits, coverage, asOfDate);

        if (months.Count == 0)
        {
            return new VariableExpenseEstimateResult(
                0m,
                "No overlapping months in the last three calendar months.",
                HasIncompleteMonths: true);
        }

        decimal average = months.Average(m => m.MonthTotal);
        bool incomplete = months.Any(m => m.IsPartialCoverage);

        string label = FormatCoverageLabel(months, incomplete);
        return new VariableExpenseEstimateResult(
            Math.Round(average, 2, MidpointRounding.ToEven),
            label,
            incomplete);
    }

    private static List<MonthAggregate> BuildRecentMonths(
        IReadOnlyList<(DateOnly Date, decimal Amount)> debits,
        HashSet<DateOnly> coverage,
        DateOnly asOfDate)
    {
        List<MonthAggregate> result = new();
        DateOnly cursor = new(asOfDate.Year, asOfDate.Month, 1);

        for (int i = 0; i < MaxMonthsWindow; i++)
        {
            int daysInMonth = DateTime.DaysInMonth(cursor.Year, cursor.Month);
            DateOnly monthEnd = new(cursor.Year, cursor.Month, daysInMonth);
            if (monthEnd > asOfDate)
            {
                monthEnd = asOfDate;
            }

            int coveredDays = 0;
            for (DateOnly day = cursor; day <= monthEnd; day = day.AddDays(1))
            {
                if (coverage.Contains(day))
                {
                    coveredDays++;
                }
            }

            if (coveredDays == 0)
            {
                cursor = cursor.AddMonths(-1);
                continue;
            }

            decimal monthTotal = debits
                .Where(d => d.Date.Year == cursor.Year && d.Date.Month == cursor.Month)
                .Sum(d => Math.Abs(d.Amount));

            bool partial = coveredDays < daysInMonth;
            result.Add(new MonthAggregate(cursor, monthTotal, coveredDays, daysInMonth, partial));
            cursor = cursor.AddMonths(-1);
        }

        result.Reverse();
        return result;
    }

    private static string FormatCoverageLabel(IReadOnlyList<MonthAggregate> months, bool incomplete)
    {
        string monthParts = string.Join(
            ", ",
            months.Select(m =>
                $"{m.Month.ToString("yyyy-MM", CultureInfo.InvariantCulture)}: £{m.MonthTotal.ToString("F2", CultureInfo.InvariantCulture)} ({m.CoveredDays}/{m.DaysInMonth} days covered)"));

        return incomplete
            ? $"Average of {months.Count} month(s) with partial coverage — {monthParts}. Missing days are not treated as £0 spend."
            : $"Average of {months.Count} month(s) — {monthParts}.";
    }

    private readonly record struct MonthAggregate(
        DateOnly Month,
        decimal MonthTotal,
        int CoveredDays,
        int DaysInMonth,
        bool IsPartialCoverage);
}
