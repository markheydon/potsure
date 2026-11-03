using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Persistence.Tenancy;

namespace Potsure.Infrastructure.Services;

public sealed record PlannedFundingReadModel(
    Guid CategoryId,
    BudgetCategoryKind Kind,
    decimal PlannedFutureFundingDirect,
    IReadOnlyList<PotPlannedFundingRow> Pots);

public sealed record PotPlannedFundingRow(Guid PotId, string PotName, decimal? PlannedFutureFunding);

public sealed record UpdatePlannedFundingRequest(
    Guid CategoryId,
    decimal PlannedFutureFundingDirect,
    IReadOnlyList<(Guid PotId, decimal? Amount)> PotAmounts);

/// <summary>
/// Planned future funding distinct from recorded funding and imports (US5).
/// </summary>
public sealed class PlannedFundingService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor,
    BudgetProfileSeeder profileSeeder)
{
    /// <summary>
    /// Loads planned future funding fields for categories and pots.
    /// </summary>
    public async Task<IReadOnlyList<PlannedFundingReadModel>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await LoadProfileAsync(cancellationToken);
        return profile.Categories
            .Select(c => new PlannedFundingReadModel(
                c.Id,
                c.Kind,
                c.PlannedFutureFundingDirectAmount,
                profile.Pots
                    .Where(p => p.CategoryId == c.Id)
                    .Select(p => new PotPlannedFundingRow(p.Id, p.Name, p.PlannedFutureFundingAmount))
                    .ToList()))
            .ToList();
    }

    /// <summary>
    /// Persists planned future funding without altering recorded funding or transactions.
    /// </summary>
    public async Task UpdateAsync(
        UpdatePlannedFundingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PlannedFutureFundingDirect < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Planned funding cannot be negative.");
        }

        BudgetProfileEntity profile = await LoadProfileAsync(cancellationToken);
        BudgetCategoryEntity category = profile.Categories.Single(c => c.Id == request.CategoryId);
        EntityOwnershipValidator.EnsureOwnedBy(category.OwnerKey, ownerAccessor.OwnerKey);
        category.PlannedFutureFundingDirectAmount = request.PlannedFutureFundingDirect;

        foreach ((Guid potId, decimal? amount) in request.PotAmounts)
        {
            if (amount is < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Pot planned funding cannot be negative.");
            }

            BankPotEntity pot = profile.Pots.Single(p => p.Id == potId && p.CategoryId == category.Id);
            pot.PlannedFutureFundingAmount = amount;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<BudgetProfileEntity> LoadProfileAsync(CancellationToken cancellationToken)
    {
        await profileSeeder.EnsureProfileAsync(cancellationToken);
        return await dbContext.BudgetProfiles
            .Include(p => p.Categories)
            .Include(p => p.Pots)
            .SingleAsync(cancellationToken);
    }
}
