using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Net attributed actual activity per category for a period (FR-M017).
/// </summary>
public static class ActualActivityCalculator
{
    /// <summary>
    /// Computes net attributed spend per category: expense debits minus attributed refunds or credits.
    /// </summary>
    public static IReadOnlyList<CategoryActualInput> Calculate(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<PeriodTransactionInput> transactions)
    {
        Dictionary<BudgetCategoryKind, Money> totals = new()
        {
            [BudgetCategoryKind.Needs] = Money.Zero,
            [BudgetCategoryKind.Wants] = Money.Zero,
            [BudgetCategoryKind.SavingsAndDebt] = Money.Zero,
        };

        foreach (PeriodTransactionInput transaction in PeriodScope.FilterInclusive(
                     transactions,
                     startDate,
                     endDate,
                     t => t.TransactionDate))
        {
            if (transaction.AttributedCategory is not BudgetCategoryKind category)
            {
                continue;
            }

            if (!CountsTowardsActualActivity(transaction.Kind))
            {
                continue;
            }

            Money delta = ActivityDelta(transaction);
            if (delta == Money.Zero)
            {
                continue;
            }

            totals[category] += delta;
        }

        return
        [
            new CategoryActualInput(BudgetCategoryKind.Needs, totals[BudgetCategoryKind.Needs].RoundForReport()),
            new CategoryActualInput(BudgetCategoryKind.Wants, totals[BudgetCategoryKind.Wants].RoundForReport()),
            new CategoryActualInput(
                BudgetCategoryKind.SavingsAndDebt,
                totals[BudgetCategoryKind.SavingsAndDebt].RoundForReport()),
        ];
    }

    internal static bool CountsTowardsActualActivity(TransactionKind kind) =>
        kind is TransactionKind.ExpenseDebit
            or TransactionKind.RefundCredit
            or TransactionKind.InternalTransfer
            or TransactionKind.PotMovement;

    internal static Money ActivityDelta(PeriodTransactionInput transaction) =>
        transaction.Kind switch
        {
            TransactionKind.ExpenseDebit => SpendAmount(transaction.SignedAmount),
            TransactionKind.RefundCredit =>
                Money.FromRaw(-SpendAmount(transaction.SignedAmount).Amount).RoundForReport(),
            TransactionKind.InternalTransfer or TransactionKind.PotMovement =>
                SpendAmount(transaction.SignedAmount),
            _ => Money.Zero,
        };

    internal static Money SpendAmount(decimal signedAmount) =>
        Money.FromRaw(Math.Abs(signedAmount)).RoundForReport();
}
