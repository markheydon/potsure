using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Entities;
using Potsure.Infrastructure.Persistence.Tenancy;

namespace Potsure.Infrastructure.Services;

/// <summary>
/// Optional budgeted income suggestion from confirmed income patterns (FR-M031); never auto-applied.
/// </summary>
public sealed record BudgetedIncomeSuggestion(
    decimal SuggestedAmount,
    string Rationale,
    IReadOnlyList<string> SourceLabels);

/// <summary>
/// Derives a display-only income suggestion from confirmed recurring income sources.
/// </summary>
public sealed class BudgetedIncomeSuggestionService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    /// <summary>
    /// Returns a monthly-normalised suggestion when confirmed income patterns exist; otherwise null.
    /// </summary>
    public async Task<BudgetedIncomeSuggestion?> GetSuggestionAsync(
        CancellationToken cancellationToken = default)
    {
        List<IncomeSourceEntity> confirmed = await dbContext.IncomeSources
            .AsNoTracking()
            .Where(i => i.State == PatternConfirmationState.Confirmed)
            .ToListAsync(cancellationToken);

        if (confirmed.Count == 0)
        {
            return null;
        }

        foreach (IncomeSourceEntity source in confirmed)
        {
            EntityOwnershipValidator.EnsureOwnedBy(source.OwnerKey, ownerAccessor.OwnerKey);
        }

        decimal monthlyTotal = 0m;
        List<string> labels = [];
        foreach (IncomeSourceEntity source in confirmed)
        {
            decimal amount = source.FixedAmount ?? source.EstimateAmount ?? 0m;
            if (amount <= 0m)
            {
                continue;
            }

            monthlyTotal += NormaliseToMonthly(amount, source.Cadence);
            labels.Add(source.SourceLabel);
        }

        if (monthlyTotal <= 0m)
        {
            return null;
        }

        return new BudgetedIncomeSuggestion(
            Math.Round(monthlyTotal, 2, MidpointRounding.ToEven),
            "Sum of confirmed income patterns normalised to a monthly equivalent. Apply manually if you agree.",
            labels);
    }

    private static decimal NormaliseToMonthly(decimal amount, IncomeCadence cadence) =>
        cadence switch
        {
            IncomeCadence.Weekly => amount * 52m / 12m,
            IncomeCadence.Fortnightly => amount * 26m / 12m,
            IncomeCadence.Monthly => amount,
            _ => amount,
        };
}
