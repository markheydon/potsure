using Potsure.Budgeting;
using Potsure.Data.Entities;

namespace Potsure.Infrastructure.Features.Plan;

internal static class CategoryAllocationSync
{
    internal static void ApplyFromStandingPlan(BudgetProfileEntity profile)
    {
        if (profile.EstimatedIncomeAmount is null || profile.EstimatedIncomeCadence is null)
        {
            foreach (BudgetCategoryEntity category in profile.Categories)
            {
                category.AllocationAmount = 0m;
            }

            return;
        }

        PlanIncome planIncome = new(
            Money.FromRaw(profile.EstimatedIncomeAmount.Value),
            profile.EstimatedIncomeCadence.Value);
        Money monthlyEquivalent = ProrationCalculator.MonthlyEquivalentFromPlan(planIncome);
        IReadOnlyDictionary<BudgetCategoryKind, Money> allocations =
            AllocationCalculator.CalculateMonthlyEquivalent(profile.SelectedPreset, monthlyEquivalent);

        foreach (BudgetCategoryEntity category in profile.Categories)
        {
            category.AllocationAmount = allocations[category.Kind].Amount;
        }
    }
}
