using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Features.Plan;

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

public enum BankPotFundingDisposition
{
    NotApplicable,
    RollToCategory,
    Discard,
}

public sealed record DeleteBankPotRequest(
    Guid PotId,
    BankPotFundingDisposition FundingDisposition = BankPotFundingDisposition.NotApplicable);

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
        await profileSeeder.EnsureProfileAsync(cancellationToken);
        string ownerKey = ownerAccessor.OwnerKey;

        List<BankPotEntity> pots = await dbContext.BankPots
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.OwnerKey == ownerKey)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return pots
            .Select(MapPot)
            .OrderBy(p => p.CategoryKind)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
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
        EnsurePotCrudAllowed(profile.PotMappingMode);

        BudgetCategoryEntity category = RequireCategory(profile, request.CategoryId);
        EntityOwnershipValidator.EnsureOwnedBy(category.OwnerKey, ownerAccessor.OwnerKey);

        ValidatePotInventoryForMode(profile, additionalPot: true, targetCategoryId: request.CategoryId);

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
        EnsurePotCrudAllowed(profile.PotMappingMode);

        BankPotEntity entity = RequirePot(profile, request.Id);
        BudgetCategoryEntity category = RequireCategory(profile, request.CategoryId);

        if (entity.CategoryId != request.CategoryId)
        {
            ValidatePotInventoryForMode(
                profile,
                additionalPot: false,
                targetCategoryId: request.CategoryId,
                excludingPotId: entity.Id);
        }

        entity.Name = request.Name.Trim();
        entity.CategoryId = category.Id;
        entity.Category = category;
        entity.AllocationToPotAmount = request.AllocationToPotAmount;
        entity.PlannedFutureFundingAmount = request.PlannedFutureFundingAmount;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapPot(entity);
    }

    /// <summary>
    /// Removes a pot without affecting imported transactions (FR-M013c).
    /// </summary>
    public async Task DeleteAsync(DeleteBankPotRequest request, CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await LoadProfileAsync(cancellationToken);
        BankPotEntity entity = RequirePot(profile, request.PotId);
        EntityOwnershipValidator.EnsureOwnedBy(entity.OwnerKey, ownerAccessor.OwnerKey);

        if (entity.RecordedFundingAmount > 0m)
        {
            if (request.FundingDisposition is BankPotFundingDisposition.NotApplicable)
            {
                throw new InvalidOperationException(
                    "This pot has recorded funding. Choose roll-up to the parent category or discard before deleting.");
            }

            if (request.FundingDisposition == BankPotFundingDisposition.RollToCategory)
            {
                BudgetCategoryEntity category = RequireCategory(profile, entity.CategoryId);
                category.RecordedFundingDirectAmount += entity.RecordedFundingAmount;
            }
        }

        await ClearPotReferencesAsync(request.PotId, cancellationToken);

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
        if (profile.PotMappingMode == mode)
        {
            return;
        }

        if (mode == PotMappingMode.CategoryOnly && profile.Pots.Count > 0)
        {
            throw new InvalidOperationException(
                "Delete all mapped pots (including recorded funding roll-up or discard) before switching to category-only mode.");
        }

        if (mode == PotMappingMode.ThreePots)
        {
            ValidateThreePotModeInvariants(profile.Pots);
        }

        profile.PotMappingMode = mode;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    internal static void EnsurePotCrudAllowed(PotMappingMode mode)
    {
        if (mode == PotMappingMode.CategoryOnly)
        {
            throw new InvalidOperationException("Mapped pots are not available in category-only mode.");
        }
    }

    internal static void ValidateThreePotModeInvariants(IReadOnlyCollection<BankPotEntity> pots)
    {
        if (pots.Count > 3)
        {
            throw new InvalidOperationException("Three pots mode allows at most three mapped pots. Remove surplus pots first.");
        }

        if (pots.GroupBy(p => p.CategoryId).Any(g => g.Count() > 1))
        {
            throw new InvalidOperationException("Three pots mode allows at most one pot per category. Remove surplus mappings first.");
        }
    }

    internal static void ValidatePotInventoryForMode(
        BudgetProfileEntity profile,
        bool additionalPot,
        Guid targetCategoryId,
        Guid? excludingPotId = null)
    {
        if (profile.PotMappingMode != PotMappingMode.ThreePots)
        {
            return;
        }

        int potCount = profile.Pots.Count(p => excludingPotId is null || p.Id != excludingPotId);
        if (additionalPot && potCount >= 3)
        {
            throw new InvalidOperationException("Three pots mode allows at most three mapped pots.");
        }

        if (profile.Pots.Any(p =>
                (excludingPotId is null || p.Id != excludingPotId) && p.CategoryId == targetCategoryId))
        {
            throw new InvalidOperationException("Three pots mode allows at most one pot per category.");
        }
    }

    private async Task ClearPotReferencesAsync(Guid potId, CancellationToken cancellationToken)
    {
        List<RecurringExpenseEntity> expenses = await dbContext.RecurringExpenses
            .Where(e => e.MappedPotId == potId)
            .ToListAsync(cancellationToken);
        foreach (RecurringExpenseEntity expense in expenses)
        {
            expense.MappedPotId = null;
        }

        List<TransactionEntity> transactions = await dbContext.Transactions
            .Where(t => t.MappedPotId == potId)
            .ToListAsync(cancellationToken);
        foreach (TransactionEntity transaction in transactions)
        {
            transaction.MappedPotId = null;
        }
    }

    private static BankPotEntity RequirePot(BudgetProfileEntity profile, Guid potId)
    {
        if (profile.Pots.All(p => p.Id != potId))
        {
            throw new InvalidOperationException("This pot no longer exists. Refresh the page and try again.");
        }

        return profile.Pots.First(p => p.Id == potId);
    }

    private static BudgetCategoryEntity RequireCategory(BudgetProfileEntity profile, Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new InvalidOperationException("Choose a category for this pot.");
        }

        if (profile.Categories.All(c => c.Id != categoryId))
        {
            throw new InvalidOperationException("Choose a valid category for this pot.");
        }

        return profile.Categories.First(c => c.Id == categoryId);
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
