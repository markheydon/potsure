using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Persistence.Tenancy;

namespace Potsure.Infrastructure.Queries;

public sealed record CategoryPotBreakdownLine(
    BudgetCategoryKind Category,
    string PotName,
    decimal? AllocationToPot,
    decimal RecordedFunding);

/// <summary>
/// Optional mapped-pot detail for report UIs when pots are configured (US6/US7/US8).
/// </summary>
public sealed class CategoryPotBreakdownQuery(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    public async Task<IReadOnlyList<CategoryPotBreakdownLine>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity? profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .Include(p => p.Pots)
            .SingleOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return [];
        }

        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);

        Dictionary<Guid, BudgetCategoryKind> kinds = profile.Categories.ToDictionary(c => c.Id, c => c.Kind);

        return profile.Pots
            .Where(p => kinds.ContainsKey(p.CategoryId))
            .OrderBy(p => kinds[p.CategoryId])
            .ThenBy(p => p.Name)
            .Select(p => new CategoryPotBreakdownLine(
                kinds[p.CategoryId],
                p.Name,
                p.AllocationToPotAmount,
                p.RecordedFundingAmount))
            .ToList();
    }
}
