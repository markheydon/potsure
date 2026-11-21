using Potsure.Budgeting;

namespace Potsure.Recurrence;

/// <summary>
/// Normalised transaction row used as recurrence detection input.
/// </summary>
public readonly record struct RecurrenceTransactionInput(
    DateOnly Date,
    decimal SignedAmount,
    TransactionKind Kind,
    string Label);

/// <summary>
/// Whether a detected pattern looks fixed or variable in amount.
/// </summary>
public enum DetectedAmountBehaviour
{
    Unknown,
    Fixed,
    Variable,
}

/// <summary>
/// A suggested recurring income or expense pattern before user confirmation.
/// </summary>
public sealed record DetectedRecurrencePattern(
    string Label,
    bool IsIncome,
    IncomeCadence? IncomeCadence,
    ExpenseCadence? ExpenseCadence,
    DetectedAmountBehaviour AmountBehaviour,
    decimal? TypicalAmount,
    IReadOnlyList<DateOnly> ObservedDates);

/// <summary>
/// Result of variable expense monthly estimation (FR-M034).
/// </summary>
public sealed record VariableExpenseEstimateResult(
    decimal MonthlyEstimate,
    string CoverageLabel,
    bool HasIncompleteMonths);
