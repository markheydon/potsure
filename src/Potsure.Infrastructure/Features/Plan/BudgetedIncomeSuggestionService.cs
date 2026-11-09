using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Reporting;

namespace Potsure.Infrastructure.Features.Plan;

public sealed record EstimatedIncomeSuggestion(
    decimal SuggestedAmount,
    PlanIncomeCadence SuggestedCadence,
    string Rationale,
    IReadOnlyList<string> SourceLabels,
    IReadOnlyList<string> ExcludedSources);

/// <summary>
/// Optional estimated income suggestion from confirmed income patterns (FR-M031); never auto-applied.
/// </summary>
public sealed class BudgetedIncomeSuggestionService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    public async Task<EstimatedIncomeSuggestion?> GetSuggestionAsync(
        CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity? profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return null;
        }

        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);

        List<IncomeSourceEntity> confirmed = await dbContext.IncomeSources
            .AsNoTracking()
            .Where(i => i.State == PatternConfirmationState.Confirmed)
            .ToListAsync(cancellationToken);

        if (confirmed.Count == 0)
        {
            return null;
        }

        DateOnly anchorFallback = DateOnly.FromDateTime(DateTime.UtcNow);
        IReadOnlyDictionary<Guid, DateOnly> anchors = await CommitmentPaymentAnchorResolver.ResolveIncomeAnchorsAsync(
            dbContext,
            confirmed,
            cancellationToken);

        decimal monthlyTotal = 0m;
        List<string> includedLabels = [];
        List<string> excludedLabels = [];
        foreach (IncomeSourceEntity source in confirmed)
        {
            EntityOwnershipValidator.EnsureOwnedBy(source.OwnerKey, ownerAccessor.OwnerKey);

            if (source.Cadence is IncomeCadence.Variable or IncomeCadence.AdHoc)
            {
                excludedLabels.Add(source.SourceLabel);
                decimal estimate = source.EstimateAmount ?? source.FixedAmount ?? 0m;
                if (estimate > 0m)
                {
                    monthlyTotal += estimate;
                    includedLabels.Add(source.SourceLabel);
                }

                continue;
            }

            decimal amount = source.FixedAmount ?? source.EstimateAmount ?? 0m;
            if (amount <= 0m)
            {
                excludedLabels.Add(source.SourceLabel);
                continue;
            }

            decimal monthly = source.Cadence switch
            {
                IncomeCadence.Weekly => amount * (52m / 12m),
                IncomeCadence.Fortnightly => amount * (26m / 12m),
                IncomeCadence.Monthly => amount,
                _ => amount,
            };
            monthlyTotal += monthly;
            includedLabels.Add(source.SourceLabel);
        }

        if (monthlyTotal <= 0m)
        {
            return null;
        }

        CultureInfo uk = CultureInfo.GetCultureInfo("en-GB");
        string rationale =
            $"Monthly-equivalent total from {includedLabels.Count} confirmed income pattern(s) as at {DateOnly.FromDateTime(DateTime.UtcNow).ToString("dd/MM/yyyy", uk)}. Confirm manually to save as estimated income.";

        return new EstimatedIncomeSuggestion(
            Math.Round(monthlyTotal, 2, MidpointRounding.ToEven),
            PlanIncomeCadence.Monthly,
            rationale,
            includedLabels,
            excludedLabels);
    }
}
