using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Reporting;

namespace Potsure.Infrastructure.Queries;

public sealed record UncategorisedTransactionRow(
    Guid TransactionId,
    DateOnly TransactionDate,
    string Name,
    decimal Amount,
    TransactionKind Kind);

/// <summary>
/// Lists transactions the user can assign via one-off categorisation (FR-M037, FR-M017b).
/// </summary>
public sealed class UncategorisedTransactionQuery(PotsureDbContext dbContext)
{
    /// <summary>
    /// Returns expense debits without category assignment for the working budget period.
    /// </summary>
    public async Task<IReadOnlyList<UncategorisedTransactionRow>> ListForWorkingPeriodAsync(
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

        List<TransactionEntity> rows = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.TransactionDate >= period.StartDate && t.TransactionDate <= period.EndDate)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        List<UncategorisedTransactionRow> results = new();
        foreach (TransactionEntity row in rows)
        {
            if (row.CategoryId is not null)
            {
                continue;
            }

            if (UncategorisedSpendCalculator.IsExcludedTransferWithoutAttribution(row.Kind))
            {
                results.Add(new UncategorisedTransactionRow(
                    row.Id,
                    row.TransactionDate,
                    row.Name,
                    row.Amount,
                    row.Kind));
                continue;
            }

            if (row.Kind is TransactionKind.ExpenseDebit or TransactionKind.RefundCredit or TransactionKind.IncomeCredit)
            {
                results.Add(new UncategorisedTransactionRow(
                    row.Id,
                    row.TransactionDate,
                    row.Name,
                    row.Amount,
                    row.Kind));
            }
        }

        return results;
    }
}
