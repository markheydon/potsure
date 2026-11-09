using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Features.Recurrence;

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
    string? EstimateCoverageLabel,
    bool UserResolvedAssignments);

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

        if (SourcesHaveAssignmentConflict(sources) && !request.UserResolvedAssignments)
        {
            throw new InvalidOperationException(
                "Merged patterns disagree on category, pot, or tag. Choose assignments before saving.");
        }

        RecurringExpenseEntity merged = await expensePatternService.SaveAsync(
            new SaveRecurringExpenseRequest(
                null,
                request.MergedVendorLabel,
                request.Resolution.CategoryId,
                request.Resolution.MappedPotId,
                request.Cadence,
                sources[0].PaymentType,
                ExpenseAmountKindParser.Parse(request.AmountKind),
                request.FixedAmount,
                request.MonthlyEstimate,
                request.EstimateCoverageLabel,
                request.Resolution.Tag,
                null,
                false,
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
                    ExpenseAmountKindParser.Parse(part.AmountKind),
                    part.FixedAmount,
                    part.MonthlyEstimate,
                    part.EstimateCoverageLabel,
                    part.Resolution.Tag,
                    null,
                    false,
                    PatternConfirmationState.Confirmed),
                cancellationToken);
            created.Add(entity);
        }

        dbContext.RecurringExpenses.Remove(source);
        await dbContext.SaveChangesAsync(cancellationToken);
        return created;
    }

    public static bool SourcesHaveAssignmentConflict(IReadOnlyList<RecurringExpenseEntity> sources)
    {
        if (sources.Count < 2)
        {
            return false;
        }

        Guid firstCategory = sources[0].CategoryId;
        Guid? firstPot = sources[0].MappedPotId;
        string? firstTag = sources[0].Tag;

        foreach (RecurringExpenseEntity source in sources.Skip(1))
        {
            if (source.CategoryId != firstCategory
                || source.MappedPotId != firstPot
                || !string.Equals(source.Tag, firstTag, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
