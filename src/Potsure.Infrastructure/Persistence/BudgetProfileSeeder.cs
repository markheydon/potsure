using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Persistence;

/// <summary>
/// Creates the budget profile, working period, and fixed three category rows (FR-M012).
/// </summary>
public sealed class BudgetProfileSeeder(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    private static readonly BudgetCategoryKind[] FixedCategoryKinds =
    [
        BudgetCategoryKind.Needs,
        BudgetCategoryKind.Wants,
        BudgetCategoryKind.SavingsAndDebt,
    ];

    /// <summary>
    /// Ensures a profile exists for the current owner, seeding period and three fixed categories when missing.
    /// </summary>
    public async Task<BudgetProfileEntity> EnsureProfileAsync(CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity? existing = await dbContext.BudgetProfiles
            .Include(p => p.BudgetPeriod)
            .Include(p => p.Categories)
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        return await CreateProfileAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new profile with default period and exactly three fixed <see cref="BudgetCategoryKind"/> rows.
    /// </summary>
    public async Task<BudgetProfileEntity> CreateProfileAsync(CancellationToken cancellationToken = default)
    {
        string ownerKey = ownerAccessor.OwnerKey;
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly periodStart = new(today.Year, today.Month, 1);
        DateOnly periodEnd = periodStart.AddMonths(1).AddDays(-1);

        BudgetPeriodEntity period = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = ownerKey,
            StartDate = periodStart,
            EndDate = periodEnd,
            BudgetedIncomeAmount = 0m,
        };

        BudgetProfileEntity profile = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = ownerKey,
            SelectedPreset = AllocationPreset.FiftyThirtyTwenty,
            BudgetPeriodId = period.Id,
            BudgetPeriod = period,
            Categories = CreateFixedCategories(ownerKey, AllocationPreset.FiftyThirtyTwenty, 0m),
        };

        foreach (BudgetCategoryEntity category in profile.Categories)
        {
            category.BudgetProfileId = profile.Id;
        }

        dbContext.BudgetPeriods.Add(period);
        dbContext.BudgetProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    /// <summary>
    /// Builds the three immutable category entities with allocations from preset and budgeted income.
    /// </summary>
    public static List<BudgetCategoryEntity> CreateFixedCategories(
        string ownerKey,
        AllocationPreset preset,
        decimal budgetedIncomeAmount)
    {
        IReadOnlyDictionary<BudgetCategoryKind, Money> allocations =
            AllocationCalculator.Calculate(preset, Money.FromRaw(budgetedIncomeAmount));

        List<BudgetCategoryEntity> categories = new();
        foreach (BudgetCategoryKind kind in FixedCategoryKinds)
        {
            categories.Add(new BudgetCategoryEntity
            {
                Id = Guid.NewGuid(),
                OwnerKey = ownerKey,
                Kind = kind,
                AllocationAmount = allocations[kind].Amount,
                RecordedFundingDirectAmount = 0m,
                PlannedFutureFundingDirectAmount = 0m,
            });
        }

        return categories;
    }
}
