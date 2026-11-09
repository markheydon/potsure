namespace Potsure.Web.Services;

/// <summary>
/// Tab-scoped inclusive report date range (FR-M031a, FR-X006). Defaults to the current calendar month; not persisted.
/// </summary>
public sealed class ReportDateRangeSessionState
{
    private DateOnly? _start;
    private DateOnly? _end;

    public event Action? Changed;

    public (DateOnly Start, DateOnly End) GetRange()
    {
        if (_start is not null && _end is not null)
        {
            return (_start.Value, _end.Value);
        }

        return DefaultCurrentCalendarMonth();
    }

    public void SetRange(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentException("Report end date must be on or after the start date.");
        }

        _start = start;
        _end = end;
        Changed?.Invoke();
    }

    private static (DateOnly Start, DateOnly End) DefaultCurrentCalendarMonth()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly start = new(today.Year, today.Month, 1);
        DateOnly end = start.AddMonths(1).AddDays(-1);
        return (start, end);
    }
}
