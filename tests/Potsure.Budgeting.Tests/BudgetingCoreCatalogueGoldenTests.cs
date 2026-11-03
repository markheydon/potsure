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
        BudgetingContext context = new(
            AllocationPreset.FiftyThirtyTwenty,
            new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)),
            Money.FromRaw(1000m),
            [
                new(BudgetCategoryKind.Needs, Money.FromRaw(100m)),
                new(BudgetCategoryKind.Wants, Money.Zero),
                new(BudgetCategoryKind.SavingsAndDebt, Money.Zero),
            ],
            [new PotBudgetInput(BudgetCategoryKind.Needs, Money.FromRaw(50m))],
            [],
            [new(BudgetCategoryKind.Needs, Money.FromRaw(80m))],
            Money.Zero,
            new DateOnly(2026, 1, 31));

        CategoryHealthLine needs = CategoryHealthCalculator.Calculate(context)
            .Single(l => l.Kind == BudgetCategoryKind.Needs);

        Assert.Equal(150m, needs.RecordedFunding.Amount);
        Assert.Equal(420m, needs.UnderOver.Amount);
    }

    [Fact]
    public void Scenario3_ExpenseSpecificPotsSameCategory_AggregateOnce()
    {
        BudgetingContext context = new(
            AllocationPreset.FiftyThirtyTwenty,
            new DateRange(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)),
            Money.FromRaw(1000m),
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
            new DateOnly(2026, 2, 28));

        CategoryHealthLine wants = CategoryHealthCalculator.Calculate(context)
            .Single(l => l.Kind == BudgetCategoryKind.Wants);

        Assert.Equal(45m, wants.RecordedFunding.Amount);
        Assert.Equal(250m, wants.UnderOver.Amount);
    }

    [Fact]
    public void Scenario4_PresetSwitch_RecalculatesAllocationsOnly()
    {
        Money income = Money.FromRaw(1000m);
        IReadOnlyDictionary<BudgetCategoryKind, Money> fifty =
            AllocationCalculator.Calculate(AllocationPreset.FiftyThirtyTwenty, income);
        IReadOnlyDictionary<BudgetCategoryKind, Money> seventyFive =
            AllocationCalculator.Calculate(AllocationPreset.SeventyFiveFifteenTen, income);

        Assert.Equal(500m, fifty[BudgetCategoryKind.Needs].Amount);
        Assert.Equal(750m, seventyFive[BudgetCategoryKind.Needs].Amount);
        Assert.Equal(300m, fifty[BudgetCategoryKind.Wants].Amount);
        Assert.Equal(150m, seventyFive[BudgetCategoryKind.Wants].Amount);
    }

    [Fact]
    public void Scenario5_NetRefundsReduceActualActivity()
    {
        BudgetingContext context = new(
            AllocationPreset.FiftyThirtyTwenty,
            new DateRange(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)),
            Money.FromRaw(1000m),
            Categories(),
            [],
            [],
            [new(BudgetCategoryKind.Needs, Money.FromRaw(120m))],
            Money.Zero,
            new DateOnly(2026, 3, 31));

        Assert.Equal(380m, CategoryHealthCalculator.Calculate(context)
            .Single(l => l.Kind == BudgetCategoryKind.Needs).UnderOver.Amount);
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
        Assert.Equal(150m, warning.Threshold.Amount);
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
            period,
            Money.FromRaw(budgetedIncome),
            categories,
            [],
            commitments,
            actuals,
            Money.Zero,
            period.End);
    }
}
