using Potsure.Budgeting;
using Potsure.Data.Entities;

namespace Potsure.Infrastructure.Services;

internal static class CategoryAllocationSync
{
    internal static void ApplyFromBudgetedIncome(BudgetProfileEntity profile)
    {
        Money budgetedIncome = Money.FromRaw(profile.BudgetPeriod.BudgetedIncomeAmount);
        IReadOnlyDictionary<BudgetCategoryKind, Money> allocations =
            AllocationCalculator.Calculate(profile.SelectedPreset, budgetedIncome);

        foreach (BudgetCategoryEntity category in profile.Categories)
        {
            category.AllocationAmount = allocations[category.Kind].Amount;
        }
    }
}
