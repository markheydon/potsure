using Potsure.Budgeting;

namespace Potsure.Budgeting.Tests;

internal static class BudgetingTestHelpers
{
    internal static decimal ProratedMonthlyAmount(decimal monthly, DateOnly start, DateOnly end) =>
        ProrationCalculator
            .ProrateMonthlyEquivalent(Money.FromRaw(monthly), start, end)
            .Amount;

    internal static PlanIncome MonthlyPlan(decimal amount) =>
        new(Money.FromRaw(amount), PlanIncomeCadence.Monthly);
}
