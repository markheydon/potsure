using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Totals unattributed expense debits in a period (FR-M017b).
/// </summary>
public static class UncategorisedSpendCalculator
{
    /// <summary>
    /// Sums expense debits in the period that are not attributed to any category, excluding internal transfers
    /// and pot movements unless one-off categorised.
    /// </summary>
    public static Money Calculate(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<PeriodTransactionInput> transactions)
    {
        Money total = Money.Zero;
        foreach (PeriodTransactionInput transaction in PeriodScope.FilterInclusive(
                     transactions,
                     startDate,
                     endDate,
                     t => t.TransactionDate))
        {
            if (transaction.Kind != TransactionKind.ExpenseDebit)
            {
                continue;
            }

            if (transaction.AttributedCategory is not null)
            {
                continue;
            }

            if (IsExcludedTransferWithoutAttribution(transaction.Kind))
            {
                continue;
            }

            total += ActualActivityCalculator.SpendAmount(transaction.SignedAmount);
        }

        return total.RoundForReport();
    }

    public static bool IsExcludedTransferWithoutAttribution(TransactionKind kind) =>
        kind is TransactionKind.InternalTransfer or TransactionKind.PotMovement;
}
