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
        int windowMonths)
    {
        if (windowMonths is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(windowMonths));
        }

        DateOnly windowStart = windowEnd.AddMonths(-(windowMonths - 1));
        windowStart = new DateOnly(windowStart.Year, windowStart.Month, 1);

        decimal total = transactions
            .Where(t => t.Kind == TransactionKind.IncomeCredit)
            .Where(t => t.TransactionDate >= windowStart && t.TransactionDate <= windowEnd)
            .Sum(t => Math.Abs(t.SignedAmount));

        return total == 0m
            ? 0m
            : Money.FromRaw(total / windowMonths).RoundForReport().Amount;
    }
}
