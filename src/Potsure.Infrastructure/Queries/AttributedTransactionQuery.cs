using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;

namespace Potsure.Infrastructure.Queries;

public sealed record AttributedTransactionRow(
    Guid TransactionId,
    DateOnly TransactionDate,
    string Name,
    decimal Amount,
    Guid CategoryId,
    Guid? MappedPotId,
    Guid? RecurringExpenseId,
    string? Tag);

/// <summary>
/// Lists attributed expense debits the user may override (FR-M010a).
/// </summary>
public sealed class AttributedTransactionQuery(PotsureDbContext dbContext)
{
    public async Task<IReadOnlyList<AttributedTransactionRow>> ListForWorkingPeriodAsync(
        CancellationToken cancellationToken = default)
    {
        BudgetPeriodEntity? period = await dbContext.BudgetPeriods
            .AsNoTracking()
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (period is null)
        {
            return [];
        }

        List<TransactionEntity> transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.Kind == TransactionKind.ExpenseDebit)
            .Where(t => t.TransactionDate >= period.StartDate && t.TransactionDate <= period.EndDate)
            .Where(t => t.CategoryId != null)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        if (transactions.Count == 0)
        {
            return [];
        }

        Dictionary<Guid, AssignmentEntity> assignments = await dbContext.Assignments
            .AsNoTracking()
            .Where(a => a.TransactionId != null)
            .ToDictionaryAsync(a => a.TransactionId!.Value, cancellationToken);

        List<AttributedTransactionRow> rows = new(transactions.Count);
        foreach (TransactionEntity transaction in transactions)
        {
            assignments.TryGetValue(transaction.Id, out AssignmentEntity? assignment);
            rows.Add(new AttributedTransactionRow(
                transaction.Id,
                transaction.TransactionDate,
                transaction.Name,
                transaction.Amount,
                transaction.CategoryId!.Value,
                transaction.MappedPotId,
                assignment?.RecurringExpenseId,
                assignment?.Tag));
        }

        return rows;
    }
}
