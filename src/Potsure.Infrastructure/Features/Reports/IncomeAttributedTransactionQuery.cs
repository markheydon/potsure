using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;

namespace Potsure.Infrastructure.Features.Reports;

public sealed record IncomeAttributedTransactionRow(
    Guid TransactionId,
    DateOnly TransactionDate,
    string Name,
    decimal Amount,
    Guid? IncomeSourceId,
    string? IncomeSourceLabel);

/// <summary>
/// Lists income credits the user may attribute or override (FR-M010b).
/// </summary>
public sealed class IncomeAttributedTransactionQuery(PotsureDbContext dbContext)
{
    public async Task<IReadOnlyList<IncomeAttributedTransactionRow>> ListForReportRangeAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken = default)
    {
        List<TransactionEntity> transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.Kind == TransactionKind.IncomeCredit)
            .Where(t => t.TransactionDate >= start && t.TransactionDate <= end)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        if (transactions.Count == 0)
        {
            return [];
        }

        Dictionary<Guid, AssignmentEntity> assignments = await dbContext.Assignments
            .AsNoTracking()
            .Where(a => a.TransactionId != null && a.IsIncomeAttribution)
            .ToDictionaryAsync(a => a.TransactionId!.Value, cancellationToken);

        Dictionary<Guid, string> incomeLabels = await dbContext.IncomeSources
            .AsNoTracking()
            .ToDictionaryAsync(i => i.Id, i => i.SourceLabel, cancellationToken);

        List<IncomeAttributedTransactionRow> rows = new();
        foreach (TransactionEntity transaction in transactions)
        {
            assignments.TryGetValue(transaction.Id, out AssignmentEntity? assignment);
            Guid? sourceId = assignment?.IncomeSourceId;
            string? label = sourceId is Guid id && incomeLabels.TryGetValue(id, out string? sourceLabel)
                ? sourceLabel
                : null;

            rows.Add(new IncomeAttributedTransactionRow(
                transaction.Id,
                transaction.TransactionDate,
                transaction.Name,
                transaction.Amount,
                sourceId,
                label));
        }

        return rows;
    }
}
