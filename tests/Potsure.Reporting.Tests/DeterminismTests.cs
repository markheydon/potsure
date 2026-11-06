using Potsure.Budgeting;
using Potsure.Reporting;

namespace Potsure.Reporting.Tests;

public sealed class DeterminismTests
{
    [Fact]
    public void Build_RepeatedInput_ProducesIdenticalTotals()
    {
        PeriodSummaryInput input = SampleInput();

        PeriodSummary first = PeriodSummaryBuilder.Build(input);
        PeriodSummary second = PeriodSummaryBuilder.Build(input);

        Assert.Equal(first.BudgetedIncome, second.BudgetedIncome);
        Assert.Equal(first.UncategorisedSpend, second.UncategorisedSpend);
        Assert.Equal(first.Categories.Count, second.Categories.Count);

        for (int i = 0; i < first.Categories.Count; i++)
        {
            CategoryPeriodSummary a = first.Categories[i];
            CategoryPeriodSummary b = second.Categories[i];
            Assert.Equal(a.Kind, b.Kind);
            Assert.Equal(a.Allocation, b.Allocation);
            Assert.Equal(a.RecordedFunding, b.RecordedFunding);
            Assert.Equal(a.ActualActivity, b.ActualActivity);
            Assert.Equal(a.UnderOver, b.UnderOver);
            Assert.Equal(a.OverAllocationWarning, b.OverAllocationWarning);
        }
    }

    private static PeriodSummaryInput SampleInput() =>
        new(
            AllocationPreset.FiftyThirtyTwenty,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            BudgetedIncomeAmount: 2000m,
            ActualIncomeInPeriod: 2100m,
            ForecastHorizonMonths: 3,
            Categories:
            [
                new CategoryFundingInput(BudgetCategoryKind.Needs, 100m, []),
                new CategoryFundingInput(BudgetCategoryKind.Wants, 0m, []),
                new CategoryFundingInput(BudgetCategoryKind.SavingsAndDebt, 0m, []),
            ],
            Transactions:
            [
                new PeriodTransactionInput(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    new DateOnly(2026, 1, 10),
                    TransactionKind.ExpenseDebit,
                    -45.50m,
                    BudgetCategoryKind.Needs),
                new PeriodTransactionInput(
                    Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    new DateOnly(2026, 1, 15),
                    TransactionKind.ExpenseDebit,
                    -12.00m,
                    null),
            ],
            Commitments: []);
}
