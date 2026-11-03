using Potsure.Budgeting;
using Potsure.Reporting;

namespace Potsure.Budgeting.Tests;

public sealed class BudgetBalanceGoldenTests
{
    [Fact]
    public void BudgetBalance_UsesConfirmedExpenseCommitmentsOnly()
    {
        DateOnly start = new(2026, 1, 1);
        DateOnly end = new(2026, 1, 31);
        CommitmentExpenseInput rent = new(
            BudgetCategoryKind.Needs,
            ExpenseCadence.Monthly,
            IsVariableAmount: false,
            FixedAmount: 500m,
            MonthlyEstimate: null,
            AnchorDate: new DateOnly(2026, 1, 5));

        BudgetBalanceReport report = BudgetBalanceReportBuilder.Build(new BudgetBalanceReportInput(
            AllocationPreset.FiftyThirtyTwenty,
            start,
            end,
            BudgetedIncomeAmount: 2000m,
            ActualReceiptsInPeriod: 1800m,
            [rent],
            []));

        Assert.Equal(500m, report.PeriodCommitments.Single(c => c.Category == BudgetCategoryKind.Needs).AmountInPeriod.Amount);
        Assert.Equal(1000m, report.RequiredIncome.RequiredTotal.Amount);
        Assert.Equal(BudgetCategoryKind.Needs, report.RequiredIncome.LimitingCategory);
        Assert.True(report.RequiredIncome.MeetsRequirement);
        Assert.Equal(1800m, report.ActualReceiptsInPeriod.Amount);
        Assert.Equal(2000m, report.BudgetedIncome.Amount);
    }

    [Fact]
    public void BudgetBalance_Shortfall_WhenBudgetedIncomeBelowRequired()
    {
        CommitmentExpenseInput needs = new(
            BudgetCategoryKind.Needs,
            ExpenseCadence.Monthly,
            false,
            600m,
            null,
            new DateOnly(2026, 2, 1));

        BudgetBalanceReport report = BudgetBalanceReportBuilder.Build(new BudgetBalanceReportInput(
            AllocationPreset.FiftyThirtyTwenty,
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 2, 28),
            1000m,
            0m,
            [needs],
            []));

        Assert.Equal(1200m, report.RequiredIncome.RequiredTotal.Amount);
        Assert.Equal(200m, report.RequiredIncome.Shortfall.Amount);
        Assert.False(report.RequiredIncome.MeetsRequirement);
    }

    [Fact]
    public void BudgetBalance_ExpectedIncomeDates_FollowMonthlyCadence()
    {
        IncomeScheduleInput salary = new(
            IncomeCadence.Monthly,
            2500m,
            new DateOnly(2026, 1, 28));

        BudgetBalanceReport report = BudgetBalanceReportBuilder.Build(new BudgetBalanceReportInput(
            AllocationPreset.FiftyThirtyTwenty,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            2500m,
            0m,
            [],
            [salary]));

        Assert.Single(report.ExpectedIncomePayments);
        Assert.Equal(new DateOnly(2026, 1, 28), report.ExpectedIncomePayments[0].ExpectedDate);
        Assert.Equal(2500m, report.ExpectedIncomePayments[0].Amount.Amount);
    }
}
