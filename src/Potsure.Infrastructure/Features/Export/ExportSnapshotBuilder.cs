using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Features.Export;

/// <summary>
/// Loads the current owner's financial data into an export snapshot.
/// </summary>
public sealed class ExportSnapshotBuilder(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    public async Task<UserFinancialExportSnapshot> BuildAsync(CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .Include(p => p.Pots)
            .Include(p => p.MonzoAccount)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No budget profile exists to export.");

        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);

        Dictionary<Guid, BudgetCategoryKind> categoryKinds = profile.Categories.ToDictionary(c => c.Id, c => c.Kind);
        Dictionary<Guid, string> potNames = profile.Pots.ToDictionary(p => p.Id, p => p.Name);

        List<TransactionEntity> transactions = await dbContext.Transactions.AsNoTracking().ToListAsync(cancellationToken);
        List<IncomeSourceEntity> incomeSources = await dbContext.IncomeSources.AsNoTracking().ToListAsync(cancellationToken);
        List<RecurringExpenseEntity> recurringExpenses = await dbContext.RecurringExpenses.AsNoTracking().ToListAsync(cancellationToken);
        List<AssignmentEntity> assignments = await dbContext.Assignments.AsNoTracking().ToListAsync(cancellationToken);

        return new UserFinancialExportSnapshot(
            DateTimeOffset.UtcNow,
            profile.SelectedPreset,
            profile.ForecastHorizonMonths,
            profile.ActualIncomeWindowMonths,
            profile.EstimatedIncomeAmount,
            profile.EstimatedIncomeCadence,
            profile.PotMappingMode,
            profile.Categories.Select(c => new CategoryExportRow(
                c.Kind,
                c.AllocationAmount,
                c.RecordedFundingDirectAmount,
                c.PlannedFutureFundingDirectAmount)).ToList(),
            profile.Pots.Select(p => new BankPotExportRow(
                p.Id,
                p.Name,
                categoryKinds[p.CategoryId],
                p.AllocationToPotAmount,
                p.RecordedFundingAmount)).ToList(),
            profile.MonzoAccount is null
                ? null
                : new MonzoAccountExportRow(
                    profile.MonzoAccount.DisplayName,
                    profile.MonzoAccount.AccountFingerprint),
            incomeSources.Select(i => new IncomeSourceExportRow(
                i.Id,
                i.SourceLabel,
                i.Cadence,
                i.FixedAmount,
                i.SplitNeedsPercent,
                i.SplitWantsPercent,
                i.SplitSavingsDebtPercent,
                i.State.ToString())).ToList(),
            recurringExpenses.Select(e => new RecurringExpenseExportRow(
                e.Id,
                e.VendorLabel,
                categoryKinds[e.CategoryId],
                e.Cadence,
                e.AmountKind,
                e.FixedAmount,
                e.StoredMonthlyEstimate,
                e.PaymentAnchorDate,
                e.Tag,
                e.State.ToString())).ToList(),
            assignments.Select(a => new AssignmentExportRow(
                a.Id,
                a.TransactionId,
                a.RecurringExpenseId,
                a.IncomeSourceId,
                a.IsIncomeAttribution,
                a.CategoryId is Guid categoryId && categoryKinds.TryGetValue(categoryId, out BudgetCategoryKind kind)
                    ? kind
                    : null,
                a.Tag,
                a.ConfirmedAt)).ToList(),
            transactions.Select(t => new TransactionExportRow(
                t.Id,
                t.MonzoTransactionId,
                t.TransactionDate,
                t.TransactionTime,
                t.Type,
                t.Name,
                t.Amount,
                t.Currency,
                t.Description,
                t.Kind,
                t.CategoryId is Guid categoryId && categoryKinds.TryGetValue(categoryId, out BudgetCategoryKind kind)
                    ? kind
                    : null,
                t.MappedPotId is Guid potId && potNames.TryGetValue(potId, out string? potName) ? potName : null,
                null)).ToList());
    }
}
