using Potsure.Budgeting;

namespace Potsure.Budgeting.Tests;

public sealed class GoldenScenarioTests
{
    [Fact]
    public void Allocation_FiftyThirtyTwenty_SplitsBudgetedIncome()
    {
        IReadOnlyDictionary<BudgetCategoryKind, Money> allocations =
            AllocationCalculator.CalculateMonthlyEquivalent(
                AllocationPreset.FiftyThirtyTwenty,
                Money.FromRaw(2000m));

        Assert.Equal(1000m, allocations[BudgetCategoryKind.Needs].Amount);
        Assert.Equal(600m, allocations[BudgetCategoryKind.Wants].Amount);
        Assert.Equal(400m, allocations[BudgetCategoryKind.SavingsAndDebt].Amount);
    }

    [Fact]
    public void CategoryHealth_UnderOver_ExcludesRecordedFunding()
    {
        BudgetingContext context = BuildContext(
            preset: AllocationPreset.FiftyThirtyTwenty,
            budgetedIncome: 1000m,
            needsActual: 200m,
            needsRecorded: 500m);

        CategoryHealthLine needs = CategoryHealthCalculator.Calculate(context)
            .Single(l => l.Kind == BudgetCategoryKind.Needs);

        DateOnly start = new(2026, 1, 1);
        DateOnly end = new(2026, 1, 31);
        decimal allocation = BudgetingTestHelpers.ProratedMonthlyAmount(500m, start, end);
        Assert.Equal(allocation, needs.Allocation!.Value.Amount);
        Assert.Equal(500m, needs.RecordedFunding.Amount);
        Assert.Equal(200m, needs.ActualActivity.Amount);
        Assert.Equal(allocation - 200m, needs.UnderOver!.Value.Amount);
    }

    [Fact]
    public void RequiredIncome_LimitingCategory_TieBreaksToNeeds()
    {
        BudgetingContext context = BuildContext(
            preset: AllocationPreset.FiftyThirtyTwenty,
            budgetedIncome: 1000m,
            needsCommitment: 250m,
            wantsCommitment: 150m);

        RequiredIncomeResult result = RequiredIncomeCalculator.Calculate(context);

        Assert.Equal(500m, result.RequiredByCategory[BudgetCategoryKind.Needs].Amount);
        Assert.Equal(500m, result.RequiredByCategory[BudgetCategoryKind.Wants].Amount);
        Assert.Equal(BudgetCategoryKind.Needs, result.LimitingCategory);
        Assert.True(result.MeetsRequirement);
    }

    [Fact]
    public void Runway_Warning_WhenProjectedSpendExceedsThreshold()
    {
        BudgetingContext context = BuildContext(
            preset: AllocationPreset.FiftyThirtyTwenty,
            budgetedIncome: 1000m,
            needsActual: 400m,
            needsRecorded: 50m);

        Dictionary<BudgetCategoryKind, Money> projected = new()
        {
            [BudgetCategoryKind.Needs] = Money.FromRaw(200m),
        };

        RunwayWarning needs = RunwayCalculator.Calculate(context, projected)
            .Single(w => w.Category == BudgetCategoryKind.Needs);

        decimal threshold = BudgetingTestHelpers.ProratedMonthlyAmount(500m, new(2026, 1, 1), new(2026, 1, 31))
            - 400m
            + 50m;
        Assert.Equal(threshold, needs.Threshold.Amount);
        Assert.True(needs.IsShortfall);
    }

    private static BudgetingContext BuildContext(
        AllocationPreset preset,
        decimal budgetedIncome,
        decimal needsActual = 0m,
        decimal needsRecorded = 0m,
        decimal needsCommitment = 0m,
        decimal wantsCommitment = 0m)
    {
        DateRange period = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));
        List<CategoryBudgetInput> categories =
        [
            new(BudgetCategoryKind.Needs, Money.FromRaw(needsRecorded)),
            new(BudgetCategoryKind.Wants, Money.Zero),
            new(BudgetCategoryKind.SavingsAndDebt, Money.Zero),
        ];
        List<CategoryActualInput> actuals =
        [
            new(BudgetCategoryKind.Needs, Money.FromRaw(needsActual)),
            new(BudgetCategoryKind.Wants, Money.Zero),
            new(BudgetCategoryKind.SavingsAndDebt, Money.Zero),
        ];
        List<CategoryCommitmentInput> commitments = new();
        if (needsCommitment > 0m)
        {
            commitments.Add(new(BudgetCategoryKind.Needs, Money.FromRaw(needsCommitment)));
        }

        if (wantsCommitment > 0m)
        {
            commitments.Add(new(BudgetCategoryKind.Wants, Money.FromRaw(wantsCommitment)));
        }

        return new BudgetingContext(
            preset,
            BudgetingTestHelpers.MonthlyPlan(budgetedIncome),
            period,
            period,
            categories,
            [],
            commitments,
            actuals,
            Money.Zero,
            3);
    }
}
