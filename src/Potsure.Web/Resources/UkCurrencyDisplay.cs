using System.Globalization;

namespace Potsure.Web.Resources;

/// <summary>
/// Sterling formatting for UI copy (v1 UK-only product; matches <c>en-GB</c> locale in hosting).
/// </summary>
public static class UkCurrencyDisplay
{
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    /// <summary>
    /// Formats an amount as UK currency with grouping (e.g. £12,423.60).
    /// </summary>
    public static string Format(decimal amount) =>
        amount.ToString("C2", Uk);

    /// <summary>
    /// Formats the absolute value (e.g. debit magnitudes in tables).
    /// </summary>
    public static string FormatAbs(decimal amount) =>
        Format(Math.Abs(amount));
}
