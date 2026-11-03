using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Persistence.Tenancy;
using Potsure.Recurrence;

namespace Potsure.Infrastructure.Services;

public sealed record RecurrenceReviewItem(
    string Label,
    bool IsIncome,
    ExpenseCadence? ExpenseCadence,
    IncomeCadence? IncomeCadence,
    decimal TypicalAmount,
    PatternConfirmationState State,
    Guid? PersistedId);

/// <summary>
/// Loads suggested and persisted recurrence patterns for review UI (US2, US9).
/// </summary>
public sealed class RecurrenceReviewService(PotsureDbContext dbContext)
{
    /// <summary>
    /// Returns merged suggested and confirmed patterns for the review screen.
    /// </summary>
    public async Task<IReadOnlyList<RecurrenceReviewItem>> GetReviewItemsAsync(
        CancellationToken cancellationToken = default)
    {
        List<TransactionEntity> transactions = await dbContext.Transactions
            .AsNoTracking()
            .OrderBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        List<RecurrenceTransactionInput> inputs = transactions
            .Select(t => new RecurrenceTransactionInput(
                t.TransactionDate,
                t.Amount,
                t.Kind,
                t.Name))
            .ToList();

        IReadOnlyList<DetectedRecurrencePattern> detected = RecurrenceDetector.Detect(inputs);

        List<IncomeSourceEntity> income = await dbContext.IncomeSources.AsNoTracking().ToListAsync(cancellationToken);
        List<RecurringExpenseEntity> expenses = await dbContext.RecurringExpenses.AsNoTracking().ToListAsync(cancellationToken);

        Dictionary<string, RecurrenceReviewItem> merged = new(StringComparer.OrdinalIgnoreCase);

        foreach (DetectedRecurrencePattern pattern in detected)
        {
            bool isIncome = pattern.IsIncome;
            string normalisedLabel = PatternLabelNormalizer.Normalize(pattern.Label);
            PatternConfirmationState state = PatternConfirmationState.Suggested;
            Guid? id = null;

            if (isIncome)
            {
                IncomeSourceEntity? match = income.FirstOrDefault(i =>
                    string.Equals(
                        PatternLabelNormalizer.Normalize(i.SourceLabel),
                        normalisedLabel,
                        StringComparison.Ordinal));
                if (match is not null)
                {
                    state = match.State;
                    id = match.Id;
                }
            }
            else
            {
                RecurringExpenseEntity? match = expenses.FirstOrDefault(e =>
                    string.Equals(
                        PatternLabelNormalizer.Normalize(e.VendorLabel),
                        normalisedLabel,
                        StringComparison.Ordinal));
                if (match is not null)
                {
                    state = match.State;
                    id = match.Id;
                }
            }

            merged[normalisedLabel] = new RecurrenceReviewItem(
                pattern.Label,
                isIncome,
                pattern.IsIncome ? null : pattern.ExpenseCadence,
                pattern.IsIncome ? pattern.IncomeCadence : null,
                pattern.TypicalAmount ?? 0m,
                state,
                id);
        }

        foreach (IncomeSourceEntity source in income)
        {
            string key = PatternLabelNormalizer.Normalize(source.SourceLabel);
            if (merged.ContainsKey(key))
            {
                continue;
            }

            merged[key] = new RecurrenceReviewItem(
                source.SourceLabel,
                true,
                null,
                source.Cadence,
                source.FixedAmount ?? source.EstimateAmount ?? 0m,
                source.State,
                source.Id);
        }

        foreach (RecurringExpenseEntity expense in expenses)
        {
            string key = PatternLabelNormalizer.Normalize(expense.VendorLabel);
            if (merged.ContainsKey(key))
            {
                continue;
            }

            merged[key] = new RecurrenceReviewItem(
                expense.VendorLabel,
                false,
                expense.Cadence,
                null,
                expense.FixedAmount ?? expense.MonthlyEstimate ?? 0m,
                expense.State,
                expense.Id);
        }

        return merged.Values
            .OrderBy(i => i.State)
            .ThenBy(i => i.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
