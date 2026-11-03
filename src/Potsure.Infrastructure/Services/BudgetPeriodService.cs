using Microsoft.EntityFrameworkCore;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Services;

/// <summary>
/// CRUD for the single working budget period per plan (FR-M031).
/// </summary>
public sealed record BudgetPeriodReadModel(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal BudgetedIncomeAmount);

/// <summary>
/// Updates the inclusive working period dates and user-confirmed budgeted income.
/// </summary>
public sealed record UpdateBudgetPeriodRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal BudgetedIncomeAmount);

public sealed class BudgetPeriodService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    /// <summary>
    /// Returns the working period for the current owner's profile, if any.
    /// </summary>
    public async Task<BudgetPeriodReadModel?> GetWorkingPeriodAsync(CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity? profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.BudgetPeriod)
            .SingleOrDefaultAsync(cancellationToken);

        if (profile?.BudgetPeriod is null)
        {
            return null;
        }

        BudgetPeriodEntity period = profile.BudgetPeriod;
        EntityOwnershipValidator.EnsureOwnedBy(period.OwnerKey, ownerAccessor.OwnerKey);
        return ToReadModel(period);
    }

    /// <summary>
    /// Updates the working period in place and recalculates category allocations from budgeted income.
    /// </summary>
    public async Task<BudgetPeriodReadModel> UpdateWorkingPeriodAsync(
        UpdateBudgetPeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndDate < request.StartDate)
        {
            throw new ArgumentException("End date must be on or after start date.");
        }

        if (request.BudgetedIncomeAmount < 0m)
        {
            throw new ArgumentException("Budgeted income cannot be negative.");
        }

        BudgetProfileEntity profile = await dbContext.BudgetProfiles
            .Include(p => p.BudgetPeriod)
            .Include(p => p.Categories)
            .SingleAsync(cancellationToken);

        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);
        BudgetPeriodEntity period = profile.BudgetPeriod;
        EntityOwnershipValidator.EnsureOwnedBy(period.OwnerKey, ownerAccessor.OwnerKey);

        period.StartDate = request.StartDate;
        period.EndDate = request.EndDate;
        period.BudgetedIncomeAmount = request.BudgetedIncomeAmount;
        CategoryAllocationSync.ApplyFromBudgetedIncome(profile);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToReadModel(period);
    }

    private static BudgetPeriodReadModel ToReadModel(BudgetPeriodEntity period) =>
        new(period.Id, period.StartDate, period.EndDate, period.BudgetedIncomeAmount);
}
