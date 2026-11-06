using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Recurrence;

namespace Potsure.Infrastructure.Features.Recurrence;

public sealed record RecurrenceReviewItem(
    string Label,
    bool IsIncome,
    ExpenseCadence? ExpenseCadence,
    IncomeCadence? IncomeCadence,
    decimal? TypicalAmount,
    DetectedAmountBehaviour? DetectedAmountBehaviour,
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
                pattern.TypicalAmount,
                pattern.AmountBehaviour,
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
                source.Cadence is IncomeCadence.Variable or IncomeCadence.AdHoc
                    ? source.EstimateAmount ?? source.FixedAmount
                    : source.FixedAmount ?? source.EstimateAmount,
                null,
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
                string.Equals(expense.AmountKind, "Variable", StringComparison.OrdinalIgnoreCase)
                    ? expense.MonthlyEstimate ?? expense.FixedAmount
                    : expense.FixedAmount ?? expense.MonthlyEstimate,
                string.Equals(expense.AmountKind, "Variable", StringComparison.OrdinalIgnoreCase)
                    ? DetectedAmountBehaviour.Variable
                    : null,
                expense.State,
                expense.Id);
        }

        List<RecurrenceReviewItem> items = merged.Values.ToList();
        List<DateOnly> coverageDays = transactions
            .Select(t => t.TransactionDate)
            .Distinct()
            .OrderBy(d => d)
            .ToList();
        DateOnly asOf = coverageDays.Count > 0 ? coverageDays[^1] : DateOnly.FromDateTime(DateTime.UtcNow);

        for (int i = 0; i < items.Count; i++)
        {
            RecurrenceReviewItem item = items[i];
            if (item.IsIncome
                || item.State != PatternConfirmationState.Suggested
                || item.DetectedAmountBehaviour is not (DetectedAmountBehaviour.Variable or DetectedAmountBehaviour.Unknown))
            {
                continue;
            }

            if (item.TypicalAmount is not null && item.TypicalAmount > 0m)
            {
                continue;
            }

            string normalised = PatternLabelNormalizer.Normalize(item.Label);
            List<(DateOnly Date, decimal Amount)> debits = transactions
                .Where(t => t.Kind == TransactionKind.ExpenseDebit)
                .Where(t => string.Equals(PatternLabelNormalizer.Normalize(t.Name), normalised, StringComparison.Ordinal))
                .Select(t => (t.TransactionDate, t.Amount))
                .ToList();

            VariableExpenseEstimateResult estimate = VariableExpenseEstimator.Estimate(debits, coverageDays, asOf);
            items[i] = item with { TypicalAmount = estimate.MonthlyEstimate };
        }

        return items
            .OrderBy(i => i.State)
            .ThenBy(i => i.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
