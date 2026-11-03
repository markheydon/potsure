namespace Potsure.Recurrence.Tests;

public sealed class RecurrenceDetectionTests
{
    [Fact]
    public void InternalTransfer_ExcludedFromDetectionInput()
    {
        Assert.True(RecurrenceDetector.ExcludesKind(Potsure.Budgeting.TransactionKind.InternalTransfer));
        Assert.True(RecurrenceDetector.ExcludesKind(Potsure.Budgeting.TransactionKind.PotMovement));
    }
}
