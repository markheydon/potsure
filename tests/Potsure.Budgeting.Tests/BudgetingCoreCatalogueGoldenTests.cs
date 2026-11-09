using Potsure.Budgeting;

namespace Potsure.Budgeting.Tests;

/// <summary>
/// Minimum golden catalogue from contracts/budgeting-core.md (eight scenarios).
/// </summary>
public sealed class BudgetingCoreCatalogueGoldenTests
{
    [Fact]
    public void Scenario1_CategoryOnlyBudget_FixedCommitments()
    {
        BudgetingContext context = BuildContext(
            preset: AllocationPreset.FiftyThirtyTwenty,
            budgetedIncome: 2000m,
            needsCommitment: 400m,
            wantsCommitment: 120m);

        RequiredIncomeResult required = RequiredIncomeCalculator.Calculate(context);
        Assert.Equal(800m, required.RequiredByCategory[BudgetCategoryKind.Needs].Amount);
        Assert.Equal(400m, required.RequiredByCategory[BudgetCategoryKind.Wants].Amount);
    }

    [Fact]
    public void Scenario2_CategoryNamedPots_NoDoubleCountFunding()
    {
        DateRange period = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));
        BudgetingContext context = new(
            AllocationPreset.FiftyThirtyTwenty,
            BudgetingTestHelpers.MonthlyPlan(1000m),
            period,
            period,
            [
                new(BudgetCategoryKind.Needs, Money.FromRaw(100m)),
                new(BudgetCategoryKind.Wants, Money.Zero),
                new(BudgetCategoryKind.SavingsAndDebt, Money.Zero),
            ],
            [new PotBudgetInput(BudgetCategoryKind.Needs, Money.FromRaw(50m))],
            [],
            [new(BudgetCategoryKind.Needs, Money.FromRaw(80m))],
            Money.Zero,
            3);

        CategoryHealthLine needs = CategoryHealthCalculator.Calculate(context)
            .Single(l => l.Kind == BudgetCategoryKind.Needs);

        Assert.Equal(150m, needs.RecordedFunding.Amount);
        decimal allocation = BudgetingTestHelpers.ProratedMonthlyAmount(500m, period.Start, period.End);
        Assert.Equal(allocation - 80m, needs.UnderOver!.Value.Amount);
    }

    [Fact]
    public void Scenario3_ExpenseSpecificPotsSameCategory_AggregateOnce()
    {
        DateRange period = new(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));
        BudgetingContext context = new(
            AllocationPreset.FiftyThirtyTwenty,
            BudgetingTestHelpers.MonthlyPlan(1000m),
            period,
            period,
            [
                new(BudgetCategoryKind.Wants, Money.FromRaw(20m)),
                new(BudgetCategoryKind.Needs, Money.Zero),
                new(BudgetCategoryKind.SavingsAndDebt, Money.Zero),
            ],
            [
                new PotBudgetInput(BudgetCategoryKind.Wants, Money.FromRaw(10m)),
                new PotBudgetInput(BudgetCategoryKind.Wants, Money.FromRaw(15m)),
            ],
            [],
            [new(BudgetCategoryKind.Wants, Money.FromRaw(50m))],
            Money.Zero,
            3);

        CategoryHealthLine wants = CategoryHealthCalculator.Calculate(context)
            .Single(l => l.Kind == BudgetCategoryKind.Wants);

        Assert.Equal(45m, wants.RecordedFunding.Amount);
        decimal allocation = BudgetingTestHelpers.ProratedMonthlyAmount(300m, period.Start, period.End);
        Assert.Equal(allocation - 50m, wants.UnderOver!.Value.Amount);
    }

    [Fact]
    public void Scenario4_PresetSwitch_RecalculatesAllocationsOnly()
    {
        Money income = Money.FromRaw(1000m);
        IReadOnlyDictionary<BudgetCategoryKind, Money> fifty =
            AllocationCalculator.CalculateMonthlyEquivalent(AllocationPreset.FiftyThirtyTwenty, income);
        IReadOnlyDictionary<BudgetCategoryKind, Money> seventyFive =
            AllocationCalculator.CalculateMonthlyEquivalent(AllocationPreset.SeventyFiveFifteenTen, income);

        Assert.Equal(500m, fifty[BudgetCategoryKind.Needs].Amount);
        Assert.Equal(750m, seventyFive[BudgetCategoryKind.Needs].Amount);
        Assert.Equal(300m, fifty[BudgetCategoryKind.Wants].Amount);
        Assert.Equal(150m, seventyFive[BudgetCategoryKind.Wants].Amount);
    }

    [Fact]
    public void Scenario5_NetRefundsReduceActualActivity()
    {
        DateRange period = new(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));
        BudgetingContext context = new(
            AllocationPreset.FiftyThirtyTwenty,
            BudgetingTestHelpers.MonthlyPlan(1000m),
            period,
            period,
            Categories(),
            [],
            [],
            [new(BudgetCategoryKind.Needs, Money.FromRaw(120m))],
            Money.Zero,
            3);

        decimal allocation = BudgetingTestHelpers.ProratedMonthlyAmount(500m, period.Start, period.End);
        Assert.Equal(
            allocation - 120m,
            CategoryHealthCalculator.Calculate(context)
                .Single(l => l.Kind == BudgetCategoryKind.Needs).UnderOver!.Value.Amount);
    }

    [Fact]
    public void Scenario6_LimitingCategoryTieBreaksToNeeds()
    {
        BudgetingContext context = BuildContext(
            preset: AllocationPreset.FiftyThirtyTwenty,
            budgetedIncome: 1000m,
            needsCommitment: 250m,
            wantsCommitment: 150m);

        RequiredIncomeResult result = RequiredIncomeCalculator.Calculate(context);
        Assert.Equal(BudgetCategoryKind.Needs, result.LimitingCategory);
    }

    [Fact]
    public void Scenario7_Runway_RecordedFundingCoversGap()
    {
        BudgetingContext context = BuildContext(
            preset: AllocationPreset.FiftyThirtyTwenty,
            budgetedIncome: 1000m,
            needsActual: 450m,
            needsRecorded: 200m);

        Dictionary<BudgetCategoryKind, Money> projected = new()
        {
            [BudgetCategoryKind.Needs] = Money.FromRaw(200m),
        };

        RunwayWarning warning = RunwayCalculator.Calculate(context, projected)
            .Single(w => w.Category == BudgetCategoryKind.Needs);

        Assert.False(warning.IsShortfall);
    }

    [Fact]
    public void Scenario8_VariableEstimateInputs_ProjectedRunwayThreshold()
    {
        BudgetingContext context = BuildContext(
            preset: AllocationPreset.FiftyThirtyTwenty,
            budgetedIncome: 1000m,
            needsActual: 400m,
            needsRecorded: 50m);

        Dictionary<BudgetCategoryKind, Money> projectedFromVariableEstimate = new()
        {
            [BudgetCategoryKind.Needs] = Money.FromRaw(220m),
        };

        RunwayWarning warning = RunwayCalculator.Calculate(context, projectedFromVariableEstimate)
            .Single(w => w.Category == BudgetCategoryKind.Needs);

        Assert.True(warning.IsShortfall);
        decimal threshold = BudgetingTestHelpers.ProratedMonthlyAmount(500m, new(2026, 1, 1), new(2026, 1, 31))
            - 400m
            + 50m;
        Assert.Equal(threshold, warning.Threshold.Amount);
    }

    private static List<CategoryBudgetInput> Categories() =>
    [
        new(BudgetCategoryKind.Needs, Money.Zero),
        new(BudgetCategoryKind.Wants, Money.Zero),
        new(BudgetCategoryKind.SavingsAndDebt, Money.Zero),
    ];

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
