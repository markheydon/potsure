using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Actual income monthly mean over an N-month window ending on a reference date (FR-M019).
/// </summary>
public static class ActualIncomeCalculator
{
    public static decimal MeanMonthlyReceipts(
        IReadOnlyList<PeriodTransactionInput> transactions,
        DateOnly windowEnd,
        int windowMonths,
        IReadOnlySet<Guid> attributedIncomeTransactionIds)
    {
        if (windowMonths is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(windowMonths));
        }

        ArgumentNullException.ThrowIfNull(attributedIncomeTransactionIds);

        DateOnly windowStart = WindowStart(windowEnd, windowMonths);

        decimal total = transactions
            .Where(t => t.Kind == TransactionKind.IncomeCredit)
            .Where(t => attributedIncomeTransactionIds.Contains(t.Id))
            .Where(t => t.TransactionDate >= windowStart && t.TransactionDate <= windowEnd)
            .Sum(t => Math.Abs(t.SignedAmount));

        return total == 0m
            ? 0m
            : Money.FromRaw(total / windowMonths).RoundForReport().Amount;
    }

    public static DateOnly WindowStart(DateOnly windowEnd, int windowMonths)
    {
        DateOnly monthStart = windowEnd.AddMonths(-(windowMonths - 1));
        return new DateOnly(monthStart.Year, monthStart.Month, 1);
    }
}
