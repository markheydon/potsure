using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Services;

public sealed record RecordedFundingReadModel(
    Guid CategoryId,
    BudgetCategoryKind Kind,
    decimal RecordedFundingDirect,
    IReadOnlyList<PotRecordedFundingRow> Pots);

public sealed record PotRecordedFundingRow(Guid PotId, string PotName, decimal RecordedFunding);

public sealed record UpdateRecordedFundingRequest(
    Guid CategoryId,
    decimal RecordedFundingDirect,
    IReadOnlyList<(Guid PotId, decimal Amount)> PotAmounts);

/// <summary>
/// User-declared funding already set aside (FR-M015, FR-M016).
/// </summary>
public sealed class RecordedFundingService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor,
    BudgetProfileSeeder profileSeeder)
{
    /// <summary>
    /// Loads recorded funding for all categories and mapped pots.
    /// </summary>
    public async Task<IReadOnlyList<RecordedFundingReadModel>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await LoadProfileAsync(cancellationToken);
        return profile.Categories
            .Select(c => new RecordedFundingReadModel(
                c.Id,
                c.Kind,
                c.RecordedFundingDirectAmount,
                profile.Pots
                    .Where(p => p.CategoryId == c.Id)
                    .Select(p => new PotRecordedFundingRow(p.Id, p.Name, p.RecordedFundingAmount))
                    .ToList()))
            .ToList();
    }

    /// <summary>
    /// Updates direct category funding and per-pot recorded amounts.
    /// </summary>
    public async Task UpdateAsync(
        UpdateRecordedFundingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RecordedFundingDirect < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Recorded funding cannot be negative.");
        }

        BudgetProfileEntity profile = await LoadProfileAsync(cancellationToken);
        BudgetCategoryEntity category = profile.Categories.Single(c => c.Id == request.CategoryId);
        EntityOwnershipValidator.EnsureOwnedBy(category.OwnerKey, ownerAccessor.OwnerKey);
        category.RecordedFundingDirectAmount = request.RecordedFundingDirect;

        foreach ((Guid potId, decimal amount) in request.PotAmounts)
        {
            if (amount < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "Pot recorded funding cannot be negative.");
            }

            BankPotEntity pot = profile.Pots.Single(p => p.Id == potId && p.CategoryId == category.Id);
            pot.RecordedFundingAmount = amount;
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
