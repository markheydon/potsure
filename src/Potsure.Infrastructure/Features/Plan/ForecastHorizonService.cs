using Microsoft.EntityFrameworkCore;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Features.Plan;

/// <summary>
/// Forecast horizon setting on the budget profile (FR-M035).
/// </summary>
public sealed class ForecastHorizonService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor,
    BudgetProfileSeeder profileSeeder)
{
    public const int DefaultHorizonMonths = 3;
    public const int MinimumHorizonMonths = 1;
    public const int MaximumHorizonMonths = 12;

    /// <summary>
    /// Returns the configured forecast horizon in months.
    /// </summary>
    public async Task<int> GetHorizonMonthsAsync(CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await LoadProfileAsync(cancellationToken);
        return profile.ForecastHorizonMonths;
    }

    /// <summary>
    /// Updates the user override for runway projection horizon.
    /// </summary>
    public async Task SetHorizonMonthsAsync(int months, CancellationToken cancellationToken = default)
    {
        if (months is < MinimumHorizonMonths or > MaximumHorizonMonths)
        {
            throw new ArgumentOutOfRangeException(
                nameof(months),
                $"Forecast horizon must be between {MinimumHorizonMonths} and {MaximumHorizonMonths} months.");
        }

        BudgetProfileEntity profile = await LoadProfileAsync(cancellationToken);
        profile.ForecastHorizonMonths = months;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<BudgetProfileEntity> LoadProfileAsync(CancellationToken cancellationToken)
    {
        await profileSeeder.EnsureProfileAsync(cancellationToken);
        BudgetProfileEntity profile = await dbContext.BudgetProfiles.SingleAsync(cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);
        return profile;
    }
}
