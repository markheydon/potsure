using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Persistence.Tenancy;

namespace Potsure.Infrastructure.Services;

public sealed record BankPotReadModel(
    Guid Id,
    string Name,
    Guid CategoryId,
    BudgetCategoryKind CategoryKind,
    decimal? AllocationToPotAmount,
    decimal RecordedFundingAmount,
    decimal? PlannedFutureFundingAmount);

public sealed record CreateBankPotRequest(string Name, Guid CategoryId);

public sealed record UpdateBankPotRequest(
    Guid Id,
    string Name,
    Guid CategoryId,
    decimal? AllocationToPotAmount,
    decimal? PlannedFutureFundingAmount);

/// <summary>
/// CRUD for bank pots mapped exactly one category each (FR-M013).
/// </summary>
public sealed class BankPotService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor,
    BudgetProfileSeeder profileSeeder)
{
    /// <summary>
    /// Lists pots for the current profile.
    /// </summary>
    public async Task<IReadOnlyList<BankPotReadModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await LoadProfileAsync(cancellationToken);
        return profile.Pots
            .Select(MapPot)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Creates a pot linked to a single category.
    /// </summary>
    public async Task<BankPotReadModel> CreateAsync(
        CreateBankPotRequest request,
        CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await LoadProfileAsync(cancellationToken);
        BudgetCategoryEntity category = profile.Categories.Single(c => c.Id == request.CategoryId);
        EntityOwnershipValidator.EnsureOwnedBy(category.OwnerKey, ownerAccessor.OwnerKey);

        BankPotEntity entity = new()
        {
            Id = Guid.NewGuid(),
            OwnerKey = ownerAccessor.OwnerKey,
            BudgetProfileId = profile.Id,
            CategoryId = category.Id,
            Name = request.Name.Trim(),
            RecordedFundingAmount = 0m,
        };
        dbContext.BankPots.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        entity.Category = category;
        return MapPot(entity);
    }

    /// <summary>
    /// Updates pot metadata and optional allocation fields.
    /// </summary>
    public async Task<BankPotReadModel> UpdateAsync(
        UpdateBankPotRequest request,
        CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await LoadProfileAsync(cancellationToken);
        BankPotEntity entity = profile.Pots.Single(p => p.Id == request.Id);
        BudgetCategoryEntity category = profile.Categories.Single(c => c.Id == request.CategoryId);

        entity.Name = request.Name.Trim();
        entity.CategoryId = category.Id;
        entity.Category = category;
        entity.AllocationToPotAmount = request.AllocationToPotAmount;
        entity.PlannedFutureFundingAmount = request.PlannedFutureFundingAmount;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapPot(entity);
    }

    /// <summary>
    /// Removes a pot without affecting imported transactions.
    /// </summary>
    public async Task DeleteAsync(Guid potId, CancellationToken cancellationToken = default)
    {
        BankPotEntity entity = await dbContext.BankPots.SingleAsync(p => p.Id == potId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(entity.OwnerKey, ownerAccessor.OwnerKey);
        dbContext.BankPots.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Sets how pots are presented in the plan UI (FR-M014).
    /// </summary>
    public async Task SetPotMappingModeAsync(
        PotMappingMode mode,
        CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await LoadProfileAsync(cancellationToken);
        profile.PotMappingMode = mode;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<BudgetProfileEntity> LoadProfileAsync(CancellationToken cancellationToken)
    {
        await profileSeeder.EnsureProfileAsync(cancellationToken);
        return await dbContext.BudgetProfiles
            .Include(p => p.Categories)
            .Include(p => p.Pots)
            .ThenInclude(p => p.Category)
            .SingleAsync(cancellationToken);
    }

    private static BankPotReadModel MapPot(BankPotEntity entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.CategoryId,
            entity.Category.Kind,
            entity.AllocationToPotAmount,
            entity.RecordedFundingAmount,
            entity.PlannedFutureFundingAmount);
}
