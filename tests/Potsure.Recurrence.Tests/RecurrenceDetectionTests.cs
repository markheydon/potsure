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
