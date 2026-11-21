namespace Potsure.Reporting;

/// <summary>
/// Inclusive date-range helpers for period-scoped reporting (FR-M031).
/// </summary>
public static class PeriodScope
{
    /// <summary>
    /// Returns whether <paramref name="date"/> falls within the inclusive period.
    /// </summary>
    public static bool Contains(DateOnly start, DateOnly end, DateOnly date) =>
        date >= start && date <= end;

    /// <summary>
    /// Filters items whose date lies within the inclusive period boundaries.
    /// </summary>
    public static IEnumerable<T> FilterInclusive<T>(
        IEnumerable<T> items,
        DateOnly start,
        DateOnly end,
        Func<T, DateOnly> dateSelector) =>
        items.Where(item => Contains(start, end, dateSelector(item)));
}
