using Potsure.Recurrence;

namespace Potsure.Recurrence.Tests;

public sealed class RecurrenceDetectionTests
{
    [Fact]
    public void InternalTransfer_ExcludedFromDetectionInput()
    {
        Assert.True(RecurrenceDetector.ExcludesKind(Potsure.Budgeting.TransactionKind.InternalTransfer));
        Assert.True(RecurrenceDetector.ExcludesKind(Potsure.Budgeting.TransactionKind.PotMovement));
    }

    [Fact]
    public void Detect_DisplayLabelUsesLastRawNameWhenCasingVaries()
    {
        DateOnly start = new(2026, 1, 1);
        List<RecurrenceTransactionInput> rows =
        [
            new(start, -10m, Potsure.Budgeting.TransactionKind.ExpenseDebit, "Coffee SHOP"),
            new(start.AddDays(7), -10m, Potsure.Budgeting.TransactionKind.ExpenseDebit, "coffee shop"),
            new(start.AddDays(14), -10m, Potsure.Budgeting.TransactionKind.ExpenseDebit, "Coffee Shop"),
        ];

        IReadOnlyList<DetectedRecurrencePattern> patterns = RecurrenceDetector.Detect(rows);

        Assert.Single(patterns);
        Assert.Equal("Coffee Shop", patterns[0].Label);
    }

    [Fact]
    public void Detect_VariableAmountBehaviour_HasNullTypicalAmount()
    {
        DateOnly start = new(2026, 1, 1);
        List<RecurrenceTransactionInput> rows =
        [
            new(start, -10m, Potsure.Budgeting.TransactionKind.ExpenseDebit, "Energy Co"),
            new(start.AddDays(7), -20m, Potsure.Budgeting.TransactionKind.ExpenseDebit, "Energy Co"),
            new(start.AddDays(14), -30m, Potsure.Budgeting.TransactionKind.ExpenseDebit, "Energy Co"),
        ];

        IReadOnlyList<DetectedRecurrencePattern> patterns = RecurrenceDetector.Detect(rows);

        Assert.Single(patterns);
        Assert.Equal(DetectedAmountBehaviour.Variable, patterns[0].AmountBehaviour);
        Assert.Null(patterns[0].TypicalAmount);
    }

    [Fact]
    public void Detect_WeeklyExpenses_ReturnsPattern()
    {
        DateOnly start = new(2026, 1, 1);
        List<RecurrenceTransactionInput> rows =
        [
            new(start, -10m, Potsure.Budgeting.TransactionKind.ExpenseDebit, "Coffee Shop"),
            new(start.AddDays(7), -10m, Potsure.Budgeting.TransactionKind.ExpenseDebit, "Coffee Shop"),
            new(start.AddDays(14), -10m, Potsure.Budgeting.TransactionKind.ExpenseDebit, "Coffee Shop"),
        ];

        IReadOnlyList<DetectedRecurrencePattern> patterns = RecurrenceDetector.Detect(rows);

        Assert.Single(patterns);
        Assert.Equal(Potsure.Budgeting.ExpenseCadence.Weekly, patterns[0].ExpenseCadence);
    }
}
