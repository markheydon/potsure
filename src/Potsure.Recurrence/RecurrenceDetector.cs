namespace Potsure.Recurrence;

public static class RecurrenceDetector
{
    public static bool ExcludesKind(Potsure.Budgeting.TransactionKind kind) =>
        kind is Potsure.Budgeting.TransactionKind.InternalTransfer
            or Potsure.Budgeting.TransactionKind.PotMovement;
}
