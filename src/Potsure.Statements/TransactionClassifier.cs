using Potsure.Budgeting;

namespace Potsure.Statements;

/// <summary>
/// Maps Monzo export types to Potsure transaction kinds (contracts/monzo-import.md).
/// </summary>
public static class TransactionClassifier
{
    /// <summary>
    /// Classifies a parsed Monzo row using type, counterparty name, and signed amount.
    /// </summary>
    public static TransactionKind Classify(string monzoType, string name, decimal amount)
    {
        string type = monzoType.Trim();

        if (type.Contains("Pot transfer", StringComparison.OrdinalIgnoreCase))
        {
            return TransactionKind.PotMovement;
        }

        if (type.Contains("Monzo-to-Monzo", StringComparison.OrdinalIgnoreCase))
        {
            return TransactionKind.InternalTransfer;
        }

        if (type.Contains("Flex", StringComparison.OrdinalIgnoreCase))
        {
            return TransactionKind.InternalTransfer;
        }

        if (type.Contains("monzo_paid", StringComparison.OrdinalIgnoreCase))
        {
            return TransactionKind.ExpenseDebit;
        }

        if (type.Contains("Card payment", StringComparison.OrdinalIgnoreCase)
            || type.Contains("Direct Debit", StringComparison.OrdinalIgnoreCase))
        {
            return TransactionKind.ExpenseDebit;
        }

        if (type.Contains("Faster payment", StringComparison.OrdinalIgnoreCase))
        {
            return amount > 0m ? TransactionKind.IncomeCredit : TransactionKind.ExpenseDebit;
        }

        if (amount > 0m)
        {
            return TransactionKind.IncomeCredit;
        }

        if (amount == 0m)
        {
            return TransactionKind.Unknown;
        }

        return TransactionKind.ExpenseDebit;
    }
}
