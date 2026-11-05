using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

using Potsure.Recurrence;

namespace Potsure.Infrastructure.Features.Recurrence;

public sealed record ConfirmedIncomeSummaryRow(
    Guid Id,
    string SourceLabel,
    IncomeCadence Cadence,
    decimal? FixedAmount,
    decimal? EstimateAmount,
    decimal SplitNeedsPercent,
    decimal SplitWantsPercent,
    decimal SplitSavingsDebtPercent);

public sealed record ConfirmedExpenseSummaryRow(
    Guid Id,
    string VendorLabel,
    ExpenseCadence Cadence,
    string AmountKind,
    decimal? FixedAmount,
    decimal? MonthlyEstimate,
    string? EstimateCoverageLabel,
    BudgetCategoryKind CategoryKind,
    string? Tag);

public sealed record SuggestedPatternSummaryRow(
    string Label,
    bool IsIncome,
    decimal TypicalAmount,
    string CadenceDescription);

public sealed record RecurringSummaryReadModel(
    IReadOnlyList<ConfirmedIncomeSummaryRow> ConfirmedIncome,
    IReadOnlyList<ConfirmedExpenseSummaryRow> ConfirmedExpenses,
    IReadOnlyList<SuggestedPatternSummaryRow> Suggestions);

/// <summary>
/// Read model separating confirmed patterns from suggestions (FR-M021).
/// </summary>
public sealed class RecurringSummaryQuery(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor,
    RecurrenceReviewService reviewService)
{
    /// <summary>
    /// Builds the recurring summary audit view for the current owner.
    /// </summary>
    public async Task<RecurringSummaryReadModel> LoadAsync(CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .SingleAsync(cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);

        Dictionary<Guid, BudgetCategoryKind> categoryKinds = profile.Categories.ToDictionary(c => c.Id, c => c.Kind);

        List<IncomeSourceEntity> income = await dbContext.IncomeSources
            .AsNoTracking()
            .Where(i => i.State == PatternConfirmationState.Confirmed)
            .OrderBy(i => i.SourceLabel)
            .ToListAsync(cancellationToken);

        List<RecurringExpenseEntity> expenses = await dbContext.RecurringExpenses
            .AsNoTracking()
            .Where(e => e.State == PatternConfirmationState.Confirmed)
            .OrderBy(e => e.VendorLabel)
            .ToListAsync(cancellationToken);

        IReadOnlyList<RecurrenceReviewItem> review = await reviewService.GetReviewItemsAsync(cancellationToken);
        List<SuggestedPatternSummaryRow> suggestions = review
            .Where(r => r.State == PatternConfirmationState.Suggested)
            .Select(r => new SuggestedPatternSummaryRow(
                r.Label,
                r.IsIncome,
                r.TypicalAmount,
                r.IsIncome
                    ? r.IncomeCadence?.ToString() ?? "Unknown"
                    : r.ExpenseCadence?.ToString() ?? "Unknown"))
            .ToList();

        return new RecurringSummaryReadModel(
            income.Select(i => new ConfirmedIncomeSummaryRow(
                i.Id,
                i.SourceLabel,
                i.Cadence,
                i.FixedAmount,
                i.EstimateAmount,
                i.SplitNeedsPercent,
                i.SplitWantsPercent,
                i.SplitSavingsDebtPercent)).ToList(),
            expenses.Select(e => new ConfirmedExpenseSummaryRow(
                e.Id,
                e.VendorLabel,
                e.Cadence,
                e.AmountKind,
                e.FixedAmount,
                e.MonthlyEstimate,
                e.EstimateCoverageLabel,
                categoryKinds[e.CategoryId],
                e.Tag)).ToList(),
            suggestions);
    }
}
