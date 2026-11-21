namespace Potsure.Budgeting;

/// <summary>
/// Projects confirmed expense commitments into an inclusive date range (contracts/budgeting-core.md).
/// </summary>
public static class CommitmentProjector
{
    public static IReadOnlyList<CategoryCommitmentInput> ProjectIntoRange(
        DateOnly rangeStart,
        DateOnly rangeEnd,
        IReadOnlyList<ExpenseCommitmentInput> expenses)
    {
        Dictionary<BudgetCategoryKind, Money> totals = new()
        {
            [BudgetCategoryKind.Needs] = Money.Zero,
            [BudgetCategoryKind.Wants] = Money.Zero,
            [BudgetCategoryKind.SavingsAndDebt] = Money.Zero,
        };

        foreach (ExpenseCommitmentInput expense in expenses)
        {
            totals[expense.Category] += ProjectExpense(rangeStart, rangeEnd, expense);
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

    public static Money ProjectExpense(
        DateOnly rangeStart,
        DateOnly rangeEnd,
        ExpenseCommitmentInput expense)
    {
        if (expense.AmountKind == ExpenseAmountKind.Variable
            || expense.Cadence == ExpenseCadence.Variable)
        {
            decimal monthly = expense.StoredMonthlyEstimate?.Amount ?? 0m;
            return ProrationCalculator.ProrateMonthlyEquivalent(
                Money.FromRaw(monthly),
                rangeStart,
                rangeEnd);
        }

        decimal fixedAmount = expense.FixedAmountPerOccurrence?.Amount ?? 0m;
        if (fixedAmount == 0m || expense.PaymentAnchorDate is null)
        {
            return Money.Zero;
        }

        int occurrences = CountOccurrences(
            rangeStart,
            rangeEnd,
            expense.PaymentAnchorDate.Value,
            expense.Cadence);
        return Money.FromRaw(fixedAmount * occurrences).RoundForReport();
    }

    public static int CountOccurrences(
        DateOnly rangeStart,
        DateOnly rangeEnd,
        DateOnly anchor,
        ExpenseCadence cadence) =>
        cadence switch
        {
            ExpenseCadence.Weekly => CountIntervalOccurrences(rangeStart, rangeEnd, anchor, 7),
            ExpenseCadence.Fortnightly => CountIntervalOccurrences(rangeStart, rangeEnd, anchor, 14),
            ExpenseCadence.Monthly => CountMonthlyOccurrences(rangeStart, rangeEnd, anchor),
            ExpenseCadence.AnnualRare => CountAnnualOccurrences(rangeStart, rangeEnd, anchor),
            _ => 0,
        };

    private static int CountIntervalOccurrences(
        DateOnly rangeStart,
        DateOnly rangeEnd,
        DateOnly anchor,
        int intervalDays)
    {
        DateOnly cursor = anchor;
        while (cursor > rangeStart)
        {
            cursor = cursor.AddDays(-intervalDays);
        }

        while (cursor < rangeStart)
        {
            cursor = cursor.AddDays(intervalDays);
        }

        int count = 0;
        while (cursor <= rangeEnd)
        {
            if (cursor >= rangeStart)
            {
                count++;
            }

            cursor = cursor.AddDays(intervalDays);
        }

        return count;
    }

    private static int CountMonthlyOccurrences(DateOnly rangeStart, DateOnly rangeEnd, DateOnly anchor)
    {
        int count = 0;
        DateOnly cursor = new(rangeStart.Year, rangeStart.Month, 1);
        while (cursor <= rangeEnd)
        {
            int day = Math.Min(anchor.Day, DateTime.DaysInMonth(cursor.Year, cursor.Month));
            DateOnly candidate = new(cursor.Year, cursor.Month, day);
            if (candidate >= rangeStart && candidate <= rangeEnd)
            {
                count++;
            }

            cursor = cursor.AddMonths(1);
        }

        return count;
    }

    private static int CountAnnualOccurrences(DateOnly rangeStart, DateOnly rangeEnd, DateOnly anchor)
    {
        int count = 0;
        for (int year = rangeStart.Year; year <= rangeEnd.Year; year++)
        {
            DateOnly candidate = new(year, anchor.Month, anchor.Day);
            if (candidate >= rangeStart && candidate <= rangeEnd)
            {
                count++;
            }
        }

        return count;
    }
}
