using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Persistence.Tenancy;
using Potsure.Recurrence;

namespace Potsure.Infrastructure.Services;

/// <summary>
/// Auto-attribution of debits to confirmed expense patterns with override persistence (FR-M010a).
/// </summary>
public sealed class TransactionAttributionService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    /// <summary>
    /// Assigns unattributed expense debits that match a confirmed pattern label to that pattern.
    /// </summary>
    public async Task<int> AttributeMatchingDebitsAsync(
        Guid recurringExpenseId,
        CancellationToken cancellationToken = default)
    {
        RecurringExpenseEntity expense = await dbContext.RecurringExpenses
            .SingleAsync(e => e.Id == recurringExpenseId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(expense.OwnerKey, ownerAccessor.OwnerKey);

        if (expense.State != PatternConfirmationState.Confirmed)
        {
            return 0;
        }

        string normalisedVendor = PatternLabelNormalizer.Normalize(expense.VendorLabel);
        if (string.IsNullOrEmpty(normalisedVendor))
        {
            return 0;
        }

        HashSet<Guid> assignedTransactionIds = await dbContext.Assignments
            .Where(a => a.TransactionId != null)
            .Select(a => a.TransactionId!.Value)
            .ToHashSetAsync(cancellationToken);

        List<TransactionEntity> candidates = await dbContext.Transactions
            .Where(t => t.Kind == TransactionKind.ExpenseDebit)
            .ToListAsync(cancellationToken);

        int attributed = 0;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (TransactionEntity transaction in candidates)
        {
            if (assignedTransactionIds.Contains(transaction.Id))
            {
                continue;
            }

            string label = PatternLabelNormalizer.Normalize(transaction.Name);
            if (!string.Equals(label, normalisedVendor, StringComparison.Ordinal))
            {
                continue;
            }

            transaction.CategoryId = expense.CategoryId;
            transaction.MappedPotId = expense.MappedPotId;

            dbContext.Assignments.Add(new AssignmentEntity
            {
                Id = Guid.NewGuid(),
                OwnerKey = ownerAccessor.OwnerKey,
                TransactionId = transaction.Id,
                RecurringExpenseId = expense.Id,
                CategoryId = expense.CategoryId,
                MappedPotId = expense.MappedPotId,
                Tag = expense.Tag,
                ConfirmedAt = now,
            });

            attributed++;
        }

        if (attributed > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return attributed;
    }

    /// <summary>
    /// Auto-attributes debits for every confirmed recurring expense pattern (FR-M010a).
    /// </summary>
    public async Task<int> AttributeAllConfirmedExpensesAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> expenseIds = await dbContext.RecurringExpenses
            .AsNoTracking()
            .Where(e => e.State == PatternConfirmationState.Confirmed)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        int total = 0;
        foreach (Guid expenseId in expenseIds)
        {
            total += await AttributeMatchingDebitsAsync(expenseId, cancellationToken);
        }

        return total;
    }

    /// <summary>
    /// Persists a manual attribution override for a single transaction.
    /// </summary>
    public async Task<AssignmentEntity> OverrideAttributionAsync(
        Guid transactionId,
        Guid categoryId,
        Guid? mappedPotId,
        Guid? recurringExpenseId,
        string? tag,
        CancellationToken cancellationToken = default)
    {
        TransactionEntity transaction = await dbContext.Transactions
            .SingleAsync(t => t.Id == transactionId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(transaction.OwnerKey, ownerAccessor.OwnerKey);

        transaction.CategoryId = categoryId;
        transaction.MappedPotId = mappedPotId;

        AssignmentEntity? existing = await dbContext.Assignments
            .SingleOrDefaultAsync(a => a.TransactionId == transactionId, cancellationToken);

        if (existing is null)
        {
            existing = new AssignmentEntity
            {
                Id = Guid.NewGuid(),
                OwnerKey = ownerAccessor.OwnerKey,
                TransactionId = transactionId,
            };
            dbContext.Assignments.Add(existing);
        }

        existing.RecurringExpenseId = recurringExpenseId;
        existing.CategoryId = categoryId;
        existing.MappedPotId = mappedPotId;
        existing.Tag = tag;
        existing.ConfirmedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }
}
