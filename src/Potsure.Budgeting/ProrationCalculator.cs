namespace Potsure.Budgeting;

/// <summary>
/// Day-based proration and plan-income monthly-equivalent factors (FR-M031, FR-M036).
/// </summary>
public static class ProrationCalculator
{
    public const decimal AverageMonthDays = 30.4375m;

    public static int InclusiveDayCount(DateOnly start, DateOnly end) =>
        end.DayNumber - start.DayNumber + 1;

    public static Money ProrateMonthlyEquivalent(Money monthly, DateOnly inclusiveStart, DateOnly inclusiveEnd)
    {
        int days = InclusiveDayCount(inclusiveStart, inclusiveEnd);
        return (monthly * (days / AverageMonthDays)).RoundForReport();
    }

    public static decimal MonthlyEquivalentFactor(PlanIncomeCadence cadence) =>
        cadence switch
        {
            PlanIncomeCadence.Weekly => 52m / 12m,
            PlanIncomeCadence.Fortnightly => 26m / 12m,
            PlanIncomeCadence.Monthly => 1m,
            PlanIncomeCadence.Quarterly => 1m / 3m,
            PlanIncomeCadence.Yearly => 1m / 12m,
            _ => throw new ArgumentOutOfRangeException(nameof(cadence)),
        };

    public static Money MonthlyEquivalentFromPlan(PlanIncome planIncome) =>
        (planIncome.EstimatedAmount * MonthlyEquivalentFactor(planIncome.Cadence)).RoundForReport();

    public static DateOnly RunwayWindowEnd(DateOnly reportEnd, int forecastHorizonMonths)
    {
        DateOnly cursor = reportEnd;
        for (int i = 0; i < forecastHorizonMonths; i++)
        {
            cursor = cursor.AddMonths(1);
        }

        int day = Math.Min(reportEnd.Day, DateTime.DaysInMonth(cursor.Year, cursor.Month));
        return new DateOnly(cursor.Year, cursor.Month, day);
    }
}
