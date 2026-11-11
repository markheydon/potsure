using Potsure.Budgeting;

namespace Potsure.Reporting;

/// <summary>
/// Builds budget balance reports using confirmed expense commitments only (FR-M018).
/// </summary>
public static class BudgetBalanceReportBuilder
{
    /// <summary>
    /// Computes required income, limiting category, and expected income timing for the report range.
    /// </summary>
    public static BudgetBalanceReport Build(BudgetBalanceReportInput input)
    {
        DateRange reportRange = new(input.StartDate, input.EndDate);
        Money actualReceipts = Money.FromRaw(input.ActualReceiptsInPeriod).RoundForReport();
        bool notSet = input.PlanIncome is null;

        Money? estimatedInRange = notSet
            ? null
            : ProrationCalculator.ProrateMonthlyEquivalent(
                ProrationCalculator.MonthlyEquivalentFromPlan(input.PlanIncome!.Value),
                reportRange.Start,
                reportRange.End);

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

        DateRange runwayWindow = new(
            reportRange.Start,
            ProrationCalculator.RunwayWindowEnd(reportRange.End, 3));

        BudgetingContext context = new(
            input.Preset,
            input.PlanIncome,
            reportRange,
            runwayWindow,
            categories,
            [],
            commitments,
            [],
            Money.Zero,
            3);

        RequiredIncomeResult requiredIncome = RequiredIncomeCalculator.Calculate(context);
        IReadOnlyList<ExpectedIncomePayment> expectedPayments = IncomePaymentSchedule.ListExpectedPayments(
            input.StartDate,
            input.EndDate,
            input.ConfirmedIncomeSchedules);

        return new BudgetBalanceReport(
            reportRange,
            estimatedInRange,
            notSet,
            actualReceipts,
            commitments,
            requiredIncome,
            expectedPayments);
    }
}
