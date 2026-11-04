using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Recurrence;

namespace Potsure.Infrastructure.Reporting;

/// <summary>
/// Derives payment anchor dates for commitment projection from attributed history.
/// </summary>
internal static class CommitmentPaymentAnchorResolver
{
    public static async Task<IReadOnlyDictionary<Guid, DateOnly>> ResolveExpenseAnchorsAsync(
        PotsureDbContext dbContext,
        IReadOnlyList<RecurringExpenseEntity> expenses,
        CancellationToken cancellationToken)
    {
        if (expenses.Count == 0)
        {
            return new Dictionary<Guid, DateOnly>();
        }

        HashSet<Guid> expenseIds = expenses.Select(e => e.Id).ToHashSet();

        List<(Guid ExpenseId, DateOnly Date)> assignmentDates = await (
            from assignment in dbContext.Assignments.AsNoTracking()
            join transaction in dbContext.Transactions.AsNoTracking()
                on assignment.TransactionId equals transaction.Id
            where assignment.RecurringExpenseId != null
                  && expenseIds.Contains(assignment.RecurringExpenseId.Value)
            select new ValueTuple<Guid, DateOnly>(
                assignment.RecurringExpenseId!.Value,
                transaction.TransactionDate))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, DateOnly> anchors = assignmentDates
            .GroupBy(x => x.ExpenseId)
            .ToDictionary(g => g.Key, g => g.Max(x => x.Date));

        List<TransactionEntity> debits = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.Kind == TransactionKind.ExpenseDebit)
            .ToListAsync(cancellationToken);

        foreach (RecurringExpenseEntity expense in expenses)
        {
            if (anchors.ContainsKey(expense.Id))
            {
                continue;
            }

            string normalisedVendor = PatternLabelNormalizer.Normalize(expense.VendorLabel);
            if (string.IsNullOrEmpty(normalisedVendor))
            {
                continue;
            }

            DateOnly? latest = debits
                .Where(t => string.Equals(
                    PatternLabelNormalizer.Normalize(t.Name),
                    normalisedVendor,
                    StringComparison.Ordinal))
                .Select(t => t.TransactionDate)
                .MaxBy(d => d);

            if (latest is DateOnly anchor)
            {
                anchors[expense.Id] = anchor;
            }
        }

        return anchors;
    }

    public static async Task<IReadOnlyDictionary<Guid, DateOnly>> ResolveIncomeAnchorsAsync(
        PotsureDbContext dbContext,
        IReadOnlyList<IncomeSourceEntity> sources,
        CancellationToken cancellationToken)
    {
        if (sources.Count == 0)
        {
            return new Dictionary<Guid, DateOnly>();
        }

        List<TransactionEntity> incomeCredits = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.Kind == TransactionKind.IncomeCredit)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, DateOnly> anchors = new();
        foreach (IncomeSourceEntity source in sources)
        {
            string normalisedLabel = PatternLabelNormalizer.Normalize(source.SourceLabel);
            if (string.IsNullOrEmpty(normalisedLabel))
            {
                continue;
            }

            DateOnly? latest = incomeCredits
                .Where(t => string.Equals(
                    PatternLabelNormalizer.Normalize(t.Name),
                    normalisedLabel,
                    StringComparison.Ordinal))
                .Select(t => t.TransactionDate)
                .MaxBy(d => d);

            if (latest is DateOnly anchor)
            {
                anchors[source.Id] = anchor;
            }
        }

        return anchors;
    }
}
