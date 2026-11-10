using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Recurrence;

namespace Potsure.Infrastructure.Features.Recurrence;

public sealed record SaveRecurringExpenseRequest(
    Guid? Id,
    string VendorLabel,
    Guid CategoryId,
    Guid? MappedPotId,
    ExpenseCadence Cadence,
    string PaymentType,
    ExpenseAmountKind AmountKind,
    decimal? FixedAmount,
    decimal? StoredMonthlyEstimate,
    string? EstimateCoverageLabel,
    string? Tag,
    DateOnly? PaymentAnchorDate,
    bool PaymentAnchorUserOverride,
    PatternConfirmationState State);

/// <summary>
/// Persists recurring expense patterns with required category (FR-M009).
/// </summary>
public static class ExpenseAmountKindParser
{
    public static ExpenseAmountKind Parse(string? value) =>
        string.Equals(value, "Variable", StringComparison.OrdinalIgnoreCase)
            ? ExpenseAmountKind.Variable
            : ExpenseAmountKind.Fixed;

    public static string ToExportString(ExpenseAmountKind kind) =>
        kind == ExpenseAmountKind.Variable ? "Variable" : "Fixed";
}

public sealed class ExpensePatternService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    public async Task<RecurringExpenseEntity> SaveAsync(
        SaveRecurringExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CategoryId == Guid.Empty)
        {
            throw new InvalidOperationException("Category is required for a recurring expense.");
        }

        BudgetCategoryEntity category = await dbContext.BudgetCategories
            .SingleAsync(c => c.Id == request.CategoryId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(category.OwnerKey, ownerAccessor.OwnerKey);

        if (request.MappedPotId is Guid potId)
        {
            BankPotEntity pot = await dbContext.BankPots.SingleAsync(p => p.Id == potId, cancellationToken);
            EntityOwnershipValidator.EnsureOwnedBy(pot.OwnerKey, ownerAccessor.OwnerKey);
            if (pot.CategoryId != request.CategoryId)
            {
                throw new InvalidOperationException("Pot must belong to the selected category.");
            }
        }

        RecurringExpenseEntity entity;
        if (request.Id is Guid id)
        {
            entity = await dbContext.RecurringExpenses.SingleAsync(e => e.Id == id, cancellationToken);
            EntityOwnershipValidator.EnsureOwnedBy(entity.OwnerKey, ownerAccessor.OwnerKey);
        }
        else
        {
            entity = new RecurringExpenseEntity
            {
                Id = Guid.NewGuid(),
                OwnerKey = ownerAccessor.OwnerKey,
            };
            dbContext.RecurringExpenses.Add(entity);
        }

        entity.VendorLabel = request.VendorLabel.Trim();
        entity.CategoryId = request.CategoryId;
        entity.MappedPotId = request.MappedPotId;
        entity.Cadence = request.Cadence;
        entity.PaymentType = request.PaymentType;
        entity.AmountKind = request.AmountKind;
        entity.FixedAmount = request.AmountKind == ExpenseAmountKind.Fixed ? request.FixedAmount : null;
        entity.StoredMonthlyEstimate = request.AmountKind == ExpenseAmountKind.Variable
            ? request.StoredMonthlyEstimate
            : null;
        entity.EstimateCoverageLabel = request.EstimateCoverageLabel;
        entity.Tag = request.Tag;
        entity.State = request.State;

        if (request.PaymentAnchorDate is DateOnly anchor)
        {
            entity.PaymentAnchorDate = anchor;
            entity.PaymentAnchorUserOverride = request.PaymentAnchorUserOverride;
        }
        else if (entity.PaymentAnchorDate is null && request.State == PatternConfirmationState.Confirmed)
        {
            DateOnly resolved = await ResolveAnchorFromHistoryAsync(entity.VendorLabel, cancellationToken);
            entity.PaymentAnchorDate = resolved;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <summary>
    /// FR-M034 variable monthly estimate suggestion (window ends today; does not mutate stored patterns).
    /// </summary>
    public async Task<VariableExpenseEstimateResult> EstimateVariableMonthlyAsync(
        string vendorLabel,
        CancellationToken cancellationToken = default)
    {
        string normalised = PatternLabelNormalizer.Normalize(vendorLabel);
        List<TransactionEntity> transactions = await dbContext.Transactions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        List<(DateOnly Date, decimal Amount)> debits = transactions
            .Where(t => t.Kind == TransactionKind.ExpenseDebit)
            .Where(t => string.Equals(PatternLabelNormalizer.Normalize(t.Name), normalised, StringComparison.Ordinal))
            .Select(t => (t.TransactionDate, t.Amount))
            .ToList();

        List<DateOnly> coverageDays = transactions
            .Select(t => t.TransactionDate)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        DateOnly asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        return VariableExpenseEstimator.Estimate(debits, coverageDays, asOf);
    }

    /// <summary>
    /// Advances stored payment anchors after import when the user has not overridden them (FR-M008c).
    /// </summary>
    public async Task<int> RefreshNonOverriddenPaymentAnchorsAfterImportAsync(
        CancellationToken cancellationToken = default)
    {
        List<RecurringExpenseEntity> expenses = await dbContext.RecurringExpenses
            .Where(e => e.State == PatternConfirmationState.Confirmed)
            .Where(e => !e.PaymentAnchorUserOverride)
            .Where(e => e.AmountKind == ExpenseAmountKind.Fixed)
            .ToListAsync(cancellationToken);

        if (expenses.Count == 0)
        {
            return 0;
        }

        IReadOnlyDictionary<Guid, DateOnly> resolved = await CommitmentPaymentAnchorResolver.ResolveExpenseAnchorsAsync(
            dbContext,
            expenses,
            cancellationToken);

        int updated = 0;
        foreach (RecurringExpenseEntity entity in expenses)
        {
            if (!resolved.TryGetValue(entity.Id, out DateOnly latest))
            {
                continue;
            }

            if (entity.PaymentAnchorDate != latest)
            {
                entity.PaymentAnchorDate = latest;
                updated++;
            }
        }

        if (updated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return updated;
    }

    public async Task DeleteConfirmedAsync(Guid expenseId, CancellationToken cancellationToken = default)
    {
        RecurringExpenseEntity entity = await dbContext.RecurringExpenses
            .SingleAsync(e => e.Id == expenseId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(entity.OwnerKey, ownerAccessor.OwnerKey);
        if (entity.State != PatternConfirmationState.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed patterns can be deleted.");
        }

        dbContext.RecurringExpenses.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<DateOnly> ResolveAnchorFromHistoryAsync(
        string vendorLabel,
        CancellationToken cancellationToken)
    {
        List<TransactionEntity> debits = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.Kind == TransactionKind.ExpenseDebit)
            .ToListAsync(cancellationToken);

        string normalised = PatternLabelNormalizer.Normalize(vendorLabel);
        DateOnly? latest = debits
            .Where(t => string.Equals(PatternLabelNormalizer.Normalize(t.Name), normalised, StringComparison.Ordinal))
            .Select(t => t.TransactionDate)
            .MaxBy(d => d);

        return latest ?? DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
