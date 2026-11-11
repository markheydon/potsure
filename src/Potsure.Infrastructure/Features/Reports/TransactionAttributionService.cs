using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Recurrence;

namespace Potsure.Infrastructure.Features.Reports;

/// <summary>
/// Auto-attribution of debits to confirmed expense patterns and credits to income patterns (FR-M010a, FR-M010b).
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
                IsIncomeAttribution = false,
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
    /// Assigns unattributed income credits that match a confirmed income pattern label (FR-M010b).
    /// </summary>
    public async Task<int> AttributeMatchingCreditsAsync(
        Guid incomeSourceId,
        CancellationToken cancellationToken = default)
    {
        IncomeSourceEntity source = await dbContext.IncomeSources
            .SingleAsync(i => i.Id == incomeSourceId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(source.OwnerKey, ownerAccessor.OwnerKey);

        if (source.State != PatternConfirmationState.Confirmed)
        {
            return 0;
        }

        string normalisedSource = PatternLabelNormalizer.Normalize(source.SourceLabel);
        if (string.IsNullOrEmpty(normalisedSource))
        {
            return 0;
        }

        HashSet<Guid> assignedTransactionIds = await dbContext.Assignments
            .Where(a => a.TransactionId != null)
            .Select(a => a.TransactionId!.Value)
            .ToHashSetAsync(cancellationToken);

        List<TransactionEntity> candidates = await dbContext.Transactions
            .Where(t => t.Kind == TransactionKind.IncomeCredit)
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
            if (!string.Equals(label, normalisedSource, StringComparison.Ordinal))
            {
                continue;
            }

            transaction.CategoryId = null;
            transaction.MappedPotId = null;

            dbContext.Assignments.Add(new AssignmentEntity
            {
                Id = Guid.NewGuid(),
                OwnerKey = ownerAccessor.OwnerKey,
                TransactionId = transaction.Id,
                IncomeSourceId = source.Id,
                IsIncomeAttribution = true,
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
    /// Auto-attributes credits for every confirmed income pattern (FR-M010b).
    /// </summary>
    public async Task<int> AttributeAllConfirmedIncomeAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> sourceIds = await dbContext.IncomeSources
            .AsNoTracking()
            .Where(i => i.State == PatternConfirmationState.Confirmed)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        int total = 0;
        foreach (Guid sourceId in sourceIds)
        {
            total += await AttributeMatchingCreditsAsync(sourceId, cancellationToken);
        }

        return total;
    }

    /// <summary>
    /// Persists a manual expense attribution override for a single transaction.
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

        if (transaction.Kind != TransactionKind.ExpenseDebit)
        {
            throw new InvalidOperationException("Expense overrides apply only to expense debits.");
        }

        BudgetCategoryEntity category = await dbContext.BudgetCategories
            .SingleAsync(c => c.Id == categoryId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(category.OwnerKey, ownerAccessor.OwnerKey);

        if (mappedPotId is Guid potId)
        {
            BankPotEntity pot = await dbContext.BankPots.SingleAsync(p => p.Id == potId, cancellationToken);
            EntityOwnershipValidator.EnsureOwnedBy(pot.OwnerKey, ownerAccessor.OwnerKey);
            if (pot.CategoryId != category.Id)
            {
                throw new InvalidOperationException("Pot must belong to the selected category.");
            }
        }

        transaction.CategoryId = categoryId;
        transaction.MappedPotId = mappedPotId;

        AssignmentEntity existing = await GetOrCreateAssignmentAsync(transactionId, cancellationToken);

        existing.RecurringExpenseId = recurringExpenseId;
        existing.IncomeSourceId = null;
        existing.IsIncomeAttribution = false;
        existing.CategoryId = categoryId;
        existing.MappedPotId = mappedPotId;
        existing.Tag = tag;
        existing.ConfirmedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    /// <summary>
    /// Persists a manual income attribution override for an income credit (FR-M010b).
    /// </summary>
    public async Task<AssignmentEntity> OverrideIncomeAttributionAsync(
        Guid transactionId,
        Guid? incomeSourceId,
        CancellationToken cancellationToken = default)
    {
        TransactionEntity transaction = await dbContext.Transactions
            .SingleAsync(t => t.Id == transactionId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(transaction.OwnerKey, ownerAccessor.OwnerKey);

        if (transaction.Kind != TransactionKind.IncomeCredit)
        {
            throw new InvalidOperationException("Income attribution applies only to income credits.");
        }

        if (incomeSourceId is Guid sourceId)
        {
            IncomeSourceEntity source = await dbContext.IncomeSources
                .SingleAsync(i => i.Id == sourceId, cancellationToken);
            EntityOwnershipValidator.EnsureOwnedBy(source.OwnerKey, ownerAccessor.OwnerKey);
            if (source.State != PatternConfirmationState.Confirmed)
            {
                throw new InvalidOperationException("Only confirmed income patterns can be linked.");
            }
        }

        transaction.CategoryId = null;
        transaction.MappedPotId = null;

        AssignmentEntity? existing = await dbContext.Assignments
            .SingleOrDefaultAsync(a => a.TransactionId == transactionId, cancellationToken);

        if (incomeSourceId is null)
        {
            if (existing is not null)
            {
                dbContext.Assignments.Remove(existing);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return existing ?? new AssignmentEntity { TransactionId = transactionId };
        }

        existing = await GetOrCreateAssignmentAsync(transactionId, cancellationToken);

        existing.RecurringExpenseId = null;
        existing.IncomeSourceId = incomeSourceId;
        existing.IsIncomeAttribution = true;
        existing.CategoryId = null;
        existing.MappedPotId = null;
        existing.Tag = null;
        existing.ConfirmedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private async Task<AssignmentEntity> GetOrCreateAssignmentAsync(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
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

        return existing;
    }
}
