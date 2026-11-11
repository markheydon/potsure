using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Confirmed recurring expense used to build range commitments (FR-M018).
/// </summary>
public readonly record struct CommitmentExpenseInput(
    BudgetCategoryKind Category,
    ExpenseCadence Cadence,
    ExpenseAmountKind AmountKind,
    decimal? FixedAmount,
    decimal? StoredMonthlyEstimate,
    DateOnly? AnchorDate);

/// <summary>
/// Projects confirmed recurring expenses into an inclusive report range.
/// </summary>
public static class CommitmentProjector
{
    public static IReadOnlyList<CategoryCommitmentInput> ProjectIntoPeriod(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyList<CommitmentExpenseInput> expenses)
    {
        List<ExpenseCommitmentInput> mapped = expenses
            .Select(e => new ExpenseCommitmentInput(
                e.Category,
                e.Cadence,
                e.AmountKind,
                e.FixedAmount is decimal f ? Money.FromRaw(f) : null,
                e.AnchorDate,
                e.StoredMonthlyEstimate is decimal m ? Money.FromRaw(m) : null))
            .ToList();

        return Potsure.Budgeting.CommitmentProjector.ProjectIntoRange(periodStart, periodEnd, mapped);
    }
}
