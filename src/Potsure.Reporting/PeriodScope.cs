namespace Potsure.Reporting;

public static class PeriodScope
{
    public static bool Contains(DateOnly start, DateOnly end, DateOnly date) =>
        date >= start && date <= end;
}
