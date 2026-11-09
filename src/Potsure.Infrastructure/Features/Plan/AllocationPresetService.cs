using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Features.Plan;

/// <summary>
/// Switches allocation preset and recalculates category allocations from unchanged budgeted income (FR-M011).
/// </summary>
public sealed class AllocationPresetService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    /// <summary>
    /// Returns the preset currently selected on the profile.
    /// </summary>
    public async Task<AllocationPreset?> GetSelectedPresetAsync(CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity? profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        return profile?.SelectedPreset;
    }

    /// <summary>
    /// Applies a new preset and immediately recalculates stored category allocation amounts.
    /// </summary>
    public async Task<AllocationPreset> SwitchPresetAsync(
        AllocationPreset preset,
        CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await dbContext.BudgetProfiles
            .Include(p => p.Categories)
            .SingleAsync(cancellationToken);

        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);
        profile.SelectedPreset = preset;
        CategoryAllocationSync.ApplyFromStandingPlan(profile);

        await dbContext.SaveChangesAsync(cancellationToken);
        return profile.SelectedPreset;
    }
}
