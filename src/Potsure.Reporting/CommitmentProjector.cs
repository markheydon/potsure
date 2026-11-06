using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Confirmed recurring expense used to build period commitments (FR-M018).
/// </summary>
public readonly record struct CommitmentExpenseInput(
    BudgetCategoryKind Category,
    ExpenseCadence Cadence,
    bool IsVariableAmount,
    decimal? FixedAmount,
    decimal? MonthlyEstimate,
    DateOnly AnchorDate);

/// <summary>
/// Projects confirmed recurring expenses into an inclusive budget period.
/// </summary>
public static class CommitmentProjector
{
    /// <summary>
    /// Returns per-category commitment totals for the inclusive period.
    /// </summary>
    public static IReadOnlyList<CategoryCommitmentInput> ProjectIntoPeriod(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyList<CommitmentExpenseInput> expenses)
    {
        Dictionary<BudgetCategoryKind, Money> totals = new()
        {
            [BudgetCategoryKind.Needs] = Money.Zero,
            [BudgetCategoryKind.Wants] = Money.Zero,
            [BudgetCategoryKind.SavingsAndDebt] = Money.Zero,
        };

        foreach (CommitmentExpenseInput expense in expenses)
        {
            Money projected = ProjectExpense(periodStart, periodEnd, expense);
            totals[expense.Category] += projected;
        }

        return
        [
            new CategoryCommitmentInput(
                BudgetCategoryKind.Needs,
                totals[BudgetCategoryKind.Needs].RoundForReport()),
            new CategoryCommitmentInput(
                BudgetCategoryKind.Wants,
                totals[BudgetCategoryKind.Wants].RoundForReport()),
            new CategoryCommitmentInput(
                BudgetCategoryKind.SavingsAndDebt,
                totals[BudgetCategoryKind.SavingsAndDebt].RoundForReport()),
        ];
    }

    internal static Money ProjectExpense(
        DateOnly periodStart,
        DateOnly periodEnd,
        CommitmentExpenseInput expense)
    {
        if (expense.IsVariableAmount || expense.Cadence == ExpenseCadence.Variable)
        {
            return ProjectVariableMonthly(expense.MonthlyEstimate ?? 0m, periodStart, periodEnd);
        }

        decimal fixedAmount = expense.FixedAmount ?? 0m;
        if (fixedAmount == 0m)
        {
            return Money.Zero;
        }

        int occurrences = CountOccurrences(periodStart, periodEnd, expense.AnchorDate, expense.Cadence);
        return Money.FromRaw(fixedAmount * occurrences).RoundForReport();
    }

    internal static int CountOccurrences(
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly anchor,
        ExpenseCadence cadence) =>
        cadence switch
        {
            ExpenseCadence.Weekly => CountIntervalOccurrences(periodStart, periodEnd, anchor, 7),
            ExpenseCadence.Fortnightly => CountIntervalOccurrences(periodStart, periodEnd, anchor, 14),
            ExpenseCadence.Monthly => CountMonthlyOccurrences(periodStart, periodEnd, anchor),
            ExpenseCadence.AnnualRare => CountAnnualOccurrences(periodStart, periodEnd, anchor),
            _ => 0,
        };

    private static int CountIntervalOccurrences(
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly anchor,
        int intervalDays)
    {
        DateOnly cursor = anchor;
        while (cursor > periodStart)
        {
            cursor = cursor.AddDays(-intervalDays);
        }

        while (cursor < periodStart)
        {
            cursor = cursor.AddDays(intervalDays);
        }

        int count = 0;
        while (cursor <= periodEnd)
        {
            if (cursor >= periodStart)
            {
                count++;
            }

            cursor = cursor.AddDays(intervalDays);
        }

        return count;
    }

    private static int CountMonthlyOccurrences(DateOnly periodStart, DateOnly periodEnd, DateOnly anchor)
    {
        int count = 0;
        DateOnly cursor = new(periodStart.Year, periodStart.Month, 1);
        while (cursor <= periodEnd)
        {
            int day = Math.Min(anchor.Day, DateTime.DaysInMonth(cursor.Year, cursor.Month));
            DateOnly candidate = new(cursor.Year, cursor.Month, day);
            if (candidate >= periodStart && candidate <= periodEnd)
            {
                count++;
            }

            cursor = cursor.AddMonths(1);
        }

        return count;
    }

    private static int CountAnnualOccurrences(DateOnly periodStart, DateOnly periodEnd, DateOnly anchor)
    {
        int count = 0;
        for (int year = periodStart.Year; year <= periodEnd.Year; year++)
        {
            DateOnly candidate = new(year, anchor.Month, anchor.Day);
            if (candidate >= periodStart && candidate <= periodEnd)
            {
                count++;
            }
        }

        return count;
    }

    private static Money ProjectVariableMonthly(decimal monthlyEstimate, DateOnly periodStart, DateOnly periodEnd)
    {
        if (monthlyEstimate == 0m)
        {
            return Money.Zero;
        }

        Money total = Money.Zero;
        DateOnly cursor = new(periodStart.Year, periodStart.Month, 1);
        while (cursor <= periodEnd)
        {
            int daysInMonth = DateTime.DaysInMonth(cursor.Year, cursor.Month);
            DateOnly monthStart = cursor;
            DateOnly monthEnd = new(cursor.Year, cursor.Month, daysInMonth);
            DateOnly overlapStart = periodStart > monthStart ? periodStart : monthStart;
            DateOnly overlapEnd = periodEnd < monthEnd ? periodEnd : monthEnd;
            if (overlapStart <= overlapEnd)
            {
                int overlapDays = overlapEnd.DayNumber - overlapStart.DayNumber + 1;
                total += Money.FromRaw(monthlyEstimate * overlapDays / daysInMonth);
            }

            cursor = monthEnd.AddDays(1);
        }

        return total.RoundForReport();
    }
}
