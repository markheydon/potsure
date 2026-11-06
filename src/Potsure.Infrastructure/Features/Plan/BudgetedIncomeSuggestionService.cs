using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Reporting;

namespace Potsure.Infrastructure.Features.Plan;

/// <summary>
/// Optional budgeted income suggestion from confirmed income patterns (FR-M031); never auto-applied.
/// </summary>
public sealed record BudgetedIncomeSuggestion(
    decimal SuggestedAmount,
    string Rationale,
    IReadOnlyList<string> SourceLabels,
    IReadOnlyList<string> ExcludedSources);

/// <summary>
/// Derives a display-only income suggestion from confirmed recurring income sources.
/// </summary>
public sealed class BudgetedIncomeSuggestionService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    /// <summary>
    /// Returns a projected total for the working budget period from schedulable income patterns; otherwise null.
    /// </summary>
    public async Task<BudgetedIncomeSuggestion?> GetSuggestionAsync(
        CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity? profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.BudgetPeriod)
            .SingleOrDefaultAsync(cancellationToken);

        if (profile?.BudgetPeriod is null)
        {
            return null;
        }

        DateOnly periodStart = profile.BudgetPeriod.StartDate;
        DateOnly periodEnd = profile.BudgetPeriod.EndDate;
        DateOnly anchorFallback = periodStart;

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

        IReadOnlyDictionary<Guid, DateOnly> anchors = await CommitmentPaymentAnchorResolver.ResolveIncomeAnchorsAsync(
            dbContext,
            confirmed,
            cancellationToken);

        List<IncomeScheduleInput> schedulable = [];
        List<string> excludedLabels = [];
        foreach (IncomeSourceEntity source in confirmed)
        {
            if (source.Cadence is IncomeCadence.Variable or IncomeCadence.AdHoc)
            {
                excludedLabels.Add(source.SourceLabel);
                continue;
            }

            decimal amount = source.FixedAmount ?? source.EstimateAmount ?? 0m;
            if (amount <= 0m)
            {
                excludedLabels.Add(source.SourceLabel);
                continue;
            }

            DateOnly anchor = anchors.TryGetValue(source.Id, out DateOnly a) ? a : anchorFallback;
            schedulable.Add(new IncomeScheduleInput(source.Cadence, amount, anchor));
        }

        if (schedulable.Count == 0)
        {
            return null;
        }

        IReadOnlyList<ExpectedIncomePayment> payments = IncomePaymentSchedule.ListExpectedPayments(
            periodStart,
            periodEnd,
            schedulable);

        decimal periodTotal = payments.Sum(p => p.Amount.Amount);
        HashSet<string> includedLabels = new(StringComparer.OrdinalIgnoreCase);
        foreach (IncomeSourceEntity source in confirmed)
        {
            if (source.Cadence is IncomeCadence.Variable or IncomeCadence.AdHoc)
            {
                continue;
            }

            decimal amount = source.FixedAmount ?? source.EstimateAmount ?? 0m;
            if (amount <= 0m)
            {
                continue;
            }

            includedLabels.Add(source.SourceLabel);
        }

        if (periodTotal <= 0m)
        {
            return null;
        }

        CultureInfo uk = CultureInfo.GetCultureInfo("en-GB");
        string rationale =
            $"Sum of confirmed income payments expected between {periodStart.ToString("dd/MM/yyyy", uk)} and {periodEnd.ToString("dd/MM/yyyy", uk)} (inclusive), from weekly, fortnightly, and monthly patterns only. Apply manually if you agree.";

        return new BudgetedIncomeSuggestion(
            Math.Round(periodTotal, 2, MidpointRounding.ToEven),
            rationale,
            includedLabels.Count > 0 ? includedLabels.ToList() : new List<string> { "(income)" },
            excludedLabels);
    }
}
