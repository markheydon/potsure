using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Lists expected income payment dates within a period from confirmed cadences (not a flat four-per-month assumption).
/// </summary>
public static class IncomePaymentSchedule
{
    /// <summary>
    /// Returns expected payment dates and amounts for confirmed income patterns in the inclusive period.
    /// </summary>
    public static IReadOnlyList<ExpectedIncomePayment> ListExpectedPayments(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyList<IncomeScheduleInput> incomePatterns)
    {
        List<ExpectedIncomePayment> payments = new();
        foreach (IncomeScheduleInput pattern in incomePatterns)
        {
            Money amount = Money.FromRaw(pattern.FixedAmount ?? 0m).RoundForReport();
            if (amount.Amount == 0m)
            {
                continue;
            }

            ExpenseCadence cadence = MapCadence(pattern.Cadence);
            int occurrences = CommitmentProjector.CountOccurrences(
                periodStart,
                periodEnd,
                pattern.AnchorDate,
                cadence);

            if (occurrences == 0)
            {
                continue;
            }

            foreach (DateOnly date in EnumeratePaymentDates(periodStart, periodEnd, pattern.AnchorDate, cadence))
            {
                payments.Add(new ExpectedIncomePayment(date, amount));
            }
        }

        return payments
            .OrderBy(p => p.ExpectedDate)
            .ThenBy(p => p.Amount.Amount)
            .ToList();
    }

    private static ExpenseCadence MapCadence(IncomeCadence cadence) =>
        cadence switch
        {
            IncomeCadence.Weekly => ExpenseCadence.Weekly,
            IncomeCadence.Fortnightly => ExpenseCadence.Fortnightly,
            IncomeCadence.Monthly => ExpenseCadence.Monthly,
            _ => ExpenseCadence.Monthly,
        };

    private static IEnumerable<DateOnly> EnumeratePaymentDates(
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly anchor,
        ExpenseCadence cadence)
    {
        switch (cadence)
        {
            case ExpenseCadence.Weekly or ExpenseCadence.Fortnightly:
                int interval = cadence == ExpenseCadence.Weekly ? 7 : 14;
                DateOnly cursor = anchor;
                while (cursor > periodStart)
                {
                    cursor = cursor.AddDays(-interval);
                }

                while (cursor < periodStart)
                {
                    cursor = cursor.AddDays(interval);
                }

                while (cursor <= periodEnd)
                {
                    if (cursor >= periodStart)
                    {
                        yield return cursor;
                    }

                    cursor = cursor.AddDays(interval);
                }

                break;
            case ExpenseCadence.Monthly:
                DateOnly monthCursor = new(periodStart.Year, periodStart.Month, 1);
                while (monthCursor <= periodEnd)
                {
                    int day = Math.Min(anchor.Day, DateTime.DaysInMonth(monthCursor.Year, monthCursor.Month));
                    DateOnly candidate = new(monthCursor.Year, monthCursor.Month, day);
                    if (candidate >= periodStart && candidate <= periodEnd)
                    {
                        yield return candidate;
                    }

                    monthCursor = monthCursor.AddMonths(1);
                }

                break;
        }
    }
}
