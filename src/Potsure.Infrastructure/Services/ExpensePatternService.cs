using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Persistence.Tenancy;

namespace Potsure.Infrastructure.Services;

public sealed record SaveRecurringExpenseRequest(
    Guid? Id,
    string VendorLabel,
    Guid CategoryId,
    Guid? MappedPotId,
    ExpenseCadence Cadence,
    string PaymentType,
    string AmountKind,
    decimal? FixedAmount,
    decimal? MonthlyEstimate,
    string? EstimateCoverageLabel,
    string? Tag,
    PatternConfirmationState State);

/// <summary>
/// Persists recurring expense patterns with required category (FR-M009).
/// </summary>
public sealed class ExpensePatternService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    /// <summary>
    /// Creates or updates a recurring expense after validating category ownership.
    /// </summary>
    public async Task<RecurringExpenseEntity> SaveAsync(
        SaveRecurringExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CategoryId == Guid.Empty)
        {
            throw new InvalidOperationException("Category is required for a recurring expense.");
        }

        BudgetCategoryEntity category = await dbContext.BudgetCategories
            .SingleAsync(c => c.Id == request.CategoryId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(category.OwnerKey, ownerAccessor.OwnerKey);

        if (request.MappedPotId is Guid potId)
        {
            BankPotEntity pot = await dbContext.BankPots.SingleAsync(p => p.Id == potId, cancellationToken);
            EntityOwnershipValidator.EnsureOwnedBy(pot.OwnerKey, ownerAccessor.OwnerKey);
        }

        RecurringExpenseEntity entity;
        if (request.Id is Guid id)
        {
            entity = await dbContext.RecurringExpenses.SingleAsync(e => e.Id == id, cancellationToken);
            EntityOwnershipValidator.EnsureOwnedBy(entity.OwnerKey, ownerAccessor.OwnerKey);
        }
        else
        {
            entity = new RecurringExpenseEntity
            {
                Id = Guid.NewGuid(),
                OwnerKey = ownerAccessor.OwnerKey,
            };
            dbContext.RecurringExpenses.Add(entity);
        }

        entity.VendorLabel = request.VendorLabel.Trim();
        entity.CategoryId = request.CategoryId;
        entity.MappedPotId = request.MappedPotId;
        entity.Cadence = request.Cadence;
        entity.PaymentType = request.PaymentType;
        entity.AmountKind = request.AmountKind;
        entity.FixedAmount = request.FixedAmount;
        entity.MonthlyEstimate = request.MonthlyEstimate;
        entity.EstimateCoverageLabel = request.EstimateCoverageLabel;
        entity.Tag = request.Tag;
        entity.State = request.State;

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
