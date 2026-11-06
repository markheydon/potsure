using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Features.Reports;

public sealed record OneOffCategorisationRequest(
    Guid TransactionId,
    Guid CategoryId,
    Guid? MappedPotId);

/// <summary>
/// One-off transaction categorisation affecting actuals only (FR-M037).
/// </summary>
public sealed class OneOffCategorisationService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor,
    BudgetProfileSeeder profileSeeder)
{
    private static readonly HashSet<TransactionKind> AllowedKinds =
    [
        TransactionKind.ExpenseDebit,
        TransactionKind.RefundCredit,
        TransactionKind.IncomeCredit,
        TransactionKind.InternalTransfer,
        TransactionKind.PotMovement,
    ];

    /// <summary>
    /// Assigns a category (and optional pot) to a single transaction without creating a recurring pattern.
    /// </summary>
    public async Task CategoriseAsync(
        OneOffCategorisationRequest request,
        CancellationToken cancellationToken = default)
    {
        await profileSeeder.EnsureProfileAsync(cancellationToken);
        TransactionEntity transaction = await dbContext.Transactions
            .SingleAsync(t => t.Id == request.TransactionId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(transaction.OwnerKey, ownerAccessor.OwnerKey);

        if (!AllowedKinds.Contains(transaction.Kind))
        {
            throw new InvalidOperationException("Only expense debits, refunds, and income credits can be categorised as one-offs.");
        }

        BudgetCategoryEntity category = await dbContext.BudgetCategories
            .SingleAsync(c => c.Id == request.CategoryId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(category.OwnerKey, ownerAccessor.OwnerKey);

        if (request.MappedPotId is Guid potId)
        {
            BankPotEntity pot = await dbContext.BankPots.SingleAsync(p => p.Id == potId, cancellationToken);
            EntityOwnershipValidator.EnsureOwnedBy(pot.OwnerKey, ownerAccessor.OwnerKey);
            if (pot.CategoryId != category.Id)
            {
                throw new InvalidOperationException("Pot must belong to the selected category.");
            }

            transaction.MappedPotId = potId;
        }
        else
        {
            transaction.MappedPotId = null;
        }

        transaction.CategoryId = category.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Clears one-off categorisation from a transaction.
    /// </summary>
    public async Task ClearAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        TransactionEntity transaction = await dbContext.Transactions
            .SingleAsync(t => t.Id == transactionId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(transaction.OwnerKey, ownerAccessor.OwnerKey);
        transaction.CategoryId = null;
        transaction.MappedPotId = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
