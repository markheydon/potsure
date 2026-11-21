using Potsure.Budgeting;
using Potsure.Recurrence;

namespace Potsure.Infrastructure.Features.Recurrence;

/// <summary>
/// Derived review display logic for suggested and persisted patterns.
/// </summary>
public static class RecurrenceReviewItemExtensions
{
    /// <summary>
    /// Whether an expense suggestion should be saved with variable monthly estimate semantics.
    /// </summary>
    public static bool ExpenseIsVariableAmount(this RecurrenceReviewItem item)
    {
        if (item.IsIncome)
        {
            return false;
        }

        return item.DetectedAmountBehaviour is DetectedAmountBehaviour.Variable or DetectedAmountBehaviour.Unknown
               || item.ExpenseCadence == ExpenseCadence.Variable;
    }

    /// <summary>
    /// Display line for amount behaviour on the review card (suggested patterns).
    /// </summary>
    public static string ExpenseAmountDescription(this RecurrenceReviewItem item)
    {
        if (item.ExpenseIsVariableAmount())
        {
            return item.TypicalAmount is decimal est && est > 0m
                ? $"Estimated monthly {est:C} (from history; editable before confirm)"
                : "Amount varies; enter a monthly estimate before confirming.";
        }

        return item.TypicalAmount is decimal fixedAmount && fixedAmount > 0m
            ? $"Typical per payment {fixedAmount:C}"
            : "Typical amount unknown; enter a fixed amount before confirming.";
    }

    /// <summary>
    /// Display line for income amount on the review card.
    /// </summary>
    public static string IncomeAmountDescription(this RecurrenceReviewItem item) =>
        item.TypicalAmount is decimal a && a > 0m
            ? $"Typical or estimated amount {a:C}"
            : "Enter an amount before confirming.";

    /// <summary>
    /// Stored vendor/source label; falls back to normalised key when empty.
    /// </summary>
    public static string SourceOrVendorLabel(this RecurrenceReviewItem item) =>
        string.IsNullOrWhiteSpace(item.Label)
            ? "(unnamed)"
            : item.Label.Trim();
}
