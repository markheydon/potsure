using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Persistence.Tenancy;

namespace Potsure.Infrastructure.Services;

public sealed record PatternResolution(
    Guid CategoryId,
    Guid? MappedPotId,
    string? Tag);

public sealed record MergeRecurringExpensesRequest(
    IReadOnlyList<Guid> SourceExpenseIds,
    string MergedVendorLabel,
    PatternResolution Resolution,
    ExpenseCadence Cadence,
    string AmountKind,
    decimal? FixedAmount,
    decimal? MonthlyEstimate,
    string? EstimateCoverageLabel);

public sealed record SplitRecurringExpenseRequest(
    Guid SourceExpenseId,
    IReadOnlyList<SplitExpensePart> Parts);

public sealed record SplitExpensePart(
    string VendorLabel,
    PatternResolution Resolution,
    ExpenseCadence Cadence,
    string AmountKind,
    decimal? FixedAmount,
    decimal? MonthlyEstimate,
    string? EstimateCoverageLabel);

/// <summary>
/// Merge and split workflows with explicit category, pot, and tag resolution (FR-M008).
/// </summary>
public sealed class PatternMergeSplitService(
    PotsureDbContext dbContext,
    ExpensePatternService expensePatternService,
    ICurrentOwnerAccessor ownerAccessor)
{
    /// <summary>
    /// Merges multiple suggested or confirmed expenses into one pattern after user resolution.
    /// </summary>
    public async Task<RecurringExpenseEntity> MergeAsync(
        MergeRecurringExpensesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SourceExpenseIds.Count < 2)
        {
            throw new InvalidOperationException("Merge requires at least two expense patterns.");
        }

        List<RecurringExpenseEntity> sources = await dbContext.RecurringExpenses
            .Where(e => request.SourceExpenseIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        if (sources.Count != request.SourceExpenseIds.Count)
        {
            throw new InvalidOperationException("One or more expense patterns could not be found.");
        }

        foreach (RecurringExpenseEntity source in sources)
        {
            EntityOwnershipValidator.EnsureOwnedBy(source.OwnerKey, ownerAccessor.OwnerKey);
        }

        RecurringExpenseEntity merged = await expensePatternService.SaveAsync(
            new SaveRecurringExpenseRequest(
                null,
                request.MergedVendorLabel,
                request.Resolution.CategoryId,
                request.Resolution.MappedPotId,
                request.Cadence,
                sources[0].PaymentType,
                request.AmountKind,
                request.FixedAmount,
                request.MonthlyEstimate,
                request.EstimateCoverageLabel,
                request.Resolution.Tag,
                PatternConfirmationState.Confirmed),
            cancellationToken);

        dbContext.RecurringExpenses.RemoveRange(sources);
        await dbContext.SaveChangesAsync(cancellationToken);
        return merged;
    }

    /// <summary>
    /// Splits one expense pattern into several after user resolution for each part.
    /// </summary>
    public async Task<IReadOnlyList<RecurringExpenseEntity>> SplitAsync(
        SplitRecurringExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Parts.Count < 2)
        {
            throw new InvalidOperationException("Split requires at least two resulting patterns.");
        }

        RecurringExpenseEntity source = await dbContext.RecurringExpenses
            .SingleAsync(e => e.Id == request.SourceExpenseId, cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(source.OwnerKey, ownerAccessor.OwnerKey);

        List<RecurringExpenseEntity> created = new();
        foreach (SplitExpensePart part in request.Parts)
        {
            RecurringExpenseEntity entity = await expensePatternService.SaveAsync(
                new SaveRecurringExpenseRequest(
                    null,
                    part.VendorLabel,
                    part.Resolution.CategoryId,
                    part.Resolution.MappedPotId,
                    part.Cadence,
                    source.PaymentType,
                    part.AmountKind,
                    part.FixedAmount,
                    part.MonthlyEstimate,
                    part.EstimateCoverageLabel,
                    part.Resolution.Tag,
                    PatternConfirmationState.Confirmed),
                cancellationToken);
            created.Add(entity);
        }

        dbContext.RecurringExpenses.Remove(source);
        await dbContext.SaveChangesAsync(cancellationToken);
        return created;
    }
}
