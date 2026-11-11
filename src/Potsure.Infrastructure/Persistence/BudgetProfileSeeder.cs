using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Persistence;

/// <summary>
/// Creates the budget profile and fixed three category rows (FR-M012).
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

    public async Task<BudgetProfileEntity> EnsureProfileAsync(CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity? existing = await dbContext.BudgetProfiles
            .Include(p => p.Categories)
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        return await CreateProfileAsync(cancellationToken);
    }

    public async Task<BudgetProfileEntity> CreateProfileAsync(CancellationToken cancellationToken = default)
    {
        string ownerKey = ownerAccessor.OwnerKey;

        BudgetProfileEntity profile = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = ownerKey,
            SelectedPreset = AllocationPreset.FiftyThirtyTwenty,
            ActualIncomeWindowMonths = 3,
            Categories = CreateFixedCategories(ownerKey, AllocationPreset.FiftyThirtyTwenty),
        };

        foreach (BudgetCategoryEntity category in profile.Categories)
        {
            category.BudgetProfileId = profile.Id;
        }

        dbContext.BudgetProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    public static List<BudgetCategoryEntity> CreateFixedCategories(
        string ownerKey,
        AllocationPreset preset)
    {
        List<BudgetCategoryEntity> categories = new();
        foreach (BudgetCategoryKind kind in FixedCategoryKinds)
        {
            categories.Add(new BudgetCategoryEntity
            {
                Id = Guid.NewGuid(),
                OwnerKey = ownerKey,
                Kind = kind,
                AllocationAmount = 0m,
                RecordedFundingDirectAmount = 0m,
                PlannedFutureFundingDirectAmount = 0m,
            });
        }

        return categories;
    }
}
