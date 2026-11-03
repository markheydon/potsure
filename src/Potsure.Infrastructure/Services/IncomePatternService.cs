using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Services;

public sealed record SaveIncomeSourceRequest(
    Guid? Id,
    string SourceLabel,
    IncomeCadence Cadence,
    decimal? FixedAmount,
    decimal? EstimateAmount,
    decimal SplitNeedsPercent,
    decimal SplitWantsPercent,
    decimal SplitSavingsDebtPercent,
    PatternConfirmationState State);

/// <summary>
/// Persists confirmed income patterns with display-only split validation (FR-M010).
/// </summary>
public sealed class IncomePatternService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    private const decimal SplitTolerance = 0.01m;

    /// <summary>
    /// Creates or updates an income source after validating splits sum to 100%.
    /// </summary>
    public async Task<IncomeSourceEntity> SaveAsync(
        SaveIncomeSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateSplits(request.SplitNeedsPercent, request.SplitWantsPercent, request.SplitSavingsDebtPercent);

        IncomeSourceEntity entity;
        if (request.Id is Guid id)
        {
            entity = await dbContext.IncomeSources.SingleAsync(s => s.Id == id, cancellationToken);
            EntityOwnershipValidator.EnsureOwnedBy(entity.OwnerKey, ownerAccessor.OwnerKey);
        }
        else
        {
            entity = new IncomeSourceEntity
            {
                Id = Guid.NewGuid(),
                OwnerKey = ownerAccessor.OwnerKey,
            };
            dbContext.IncomeSources.Add(entity);
        }

        entity.SourceLabel = request.SourceLabel.Trim();
        entity.Cadence = request.Cadence;
        entity.FixedAmount = request.FixedAmount;
        entity.EstimateAmount = request.EstimateAmount;
        entity.SplitNeedsPercent = request.SplitNeedsPercent;
        entity.SplitWantsPercent = request.SplitWantsPercent;
        entity.SplitSavingsDebtPercent = request.SplitSavingsDebtPercent;
        entity.State = request.State;

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    internal static void ValidateSplits(decimal needs, decimal wants, decimal savingsDebt)
    {
        decimal total = needs + wants + savingsDebt;
        if (Math.Abs(total - 100m) > SplitTolerance)
        {
            throw new InvalidOperationException("Income splits must sum to 100%.");
        }
    }
}
