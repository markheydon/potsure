using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Builds budget balance reports using confirmed expense commitments only (FR-M018).
/// </summary>
public static class BudgetBalanceReportBuilder
{
    /// <summary>
    /// Computes required income, limiting category, and expected income timing for the period.
    /// </summary>
    public static BudgetBalanceReport Build(BudgetBalanceReportInput input)
    {
        DateRange period = new(input.StartDate, input.EndDate);
        Money budgetedIncome = Money.FromRaw(input.BudgetedIncomeAmount).RoundForReport();
        Money actualReceipts = Money.FromRaw(input.ActualReceiptsInPeriod).RoundForReport();

        IReadOnlyList<CategoryCommitmentInput> commitments = CommitmentProjector.ProjectIntoPeriod(
            input.StartDate,
            input.EndDate,
            input.ConfirmedExpenseCommitments);

        List<CategoryBudgetInput> categories =
        [
            new(BudgetCategoryKind.Needs, Money.Zero),
            new(BudgetCategoryKind.Wants, Money.Zero),
            new(BudgetCategoryKind.SavingsAndDebt, Money.Zero),
        ];

        BudgetingContext context = new(
            input.Preset,
            period,
            budgetedIncome,
            categories,
            [],
            commitments,
            [],
            Money.Zero,
            period.End);

        RequiredIncomeResult requiredIncome = RequiredIncomeCalculator.Calculate(context);
        IReadOnlyList<ExpectedIncomePayment> expectedPayments = IncomePaymentSchedule.ListExpectedPayments(
            input.StartDate,
            input.EndDate,
            input.ConfirmedIncomeSchedules);

        return new BudgetBalanceReport(
            period,
            budgetedIncome,
            actualReceipts,
            commitments,
            requiredIncome,
            expectedPayments);
    }
}
