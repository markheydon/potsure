using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Features.Plan;

public sealed record StandingPlanIncomeReadModel(
    decimal? EstimatedIncomeAmount,
    PlanIncomeCadence? EstimatedIncomeCadence,
    int ActualIncomeWindowMonths,
    bool IsComplete);

public sealed record UpdateStandingPlanIncomeRequest(
    decimal EstimatedIncomeAmount,
    PlanIncomeCadence EstimatedIncomeCadence,
    int ActualIncomeWindowMonths);

/// <summary>
/// CRUD for standing plan estimated income and cadence (FR-M031).
/// </summary>
public sealed class StandingPlanIncomeService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    public async Task<StandingPlanIncomeReadModel?> GetAsync(CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity? profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return null;
        }

        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);
        return ToReadModel(profile);
    }

    public async Task<StandingPlanIncomeReadModel> UpdateAsync(
        UpdateStandingPlanIncomeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EstimatedIncomeAmount < 0m)
        {
            throw new ArgumentException("Estimated income cannot be negative.");
        }

        if (request.ActualIncomeWindowMonths is < 1 or > 12)
        {
            throw new ArgumentException("Actual income window must be between 1 and 12 months.");
        }

        BudgetProfileEntity profile = await dbContext.BudgetProfiles
            .Include(p => p.Categories)
            .SingleAsync(cancellationToken);

        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);

        profile.EstimatedIncomeAmount = request.EstimatedIncomeAmount;
        profile.EstimatedIncomeCadence = request.EstimatedIncomeCadence;
        profile.ActualIncomeWindowMonths = request.ActualIncomeWindowMonths;
        CategoryAllocationSync.ApplyFromStandingPlan(profile);

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToReadModel(profile);
    }

    internal static StandingPlanIncomeReadModel ToReadModel(BudgetProfileEntity profile) =>
        new(
            profile.EstimatedIncomeAmount,
            profile.EstimatedIncomeCadence,
            profile.ActualIncomeWindowMonths,
            profile.EstimatedIncomeAmount is not null && profile.EstimatedIncomeCadence is not null);

    internal static PlanIncome? ToPlanIncome(BudgetProfileEntity profile) =>
        profile.EstimatedIncomeAmount is not null && profile.EstimatedIncomeCadence is not null
            ? new PlanIncome(
                Money.FromRaw(profile.EstimatedIncomeAmount.Value),
                profile.EstimatedIncomeCadence.Value)
            : null;
}
