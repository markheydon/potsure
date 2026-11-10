using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Reporting;

namespace Potsure.Infrastructure.Features.Reports;

/// <summary>
/// Loads persisted plan data and builds period reports for the current owner.
/// </summary>
public sealed class PeriodReportService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    public async Task<PeriodSummary> GetPeriodSummaryAsync(
        DateOnly reportStart,
        DateOnly reportEnd,
        CancellationToken cancellationToken = default)
    {
        PeriodSummaryInput input = await BuildPeriodSummaryInputAsync(reportStart, reportEnd, cancellationToken);
        return PeriodSummaryBuilder.Build(input);
    }

    public async Task<BudgetBalanceReport> GetBudgetBalanceReportAsync(
        DateOnly reportStart,
        DateOnly reportEnd,
        CancellationToken cancellationToken = default)
    {
        BudgetBalanceReportInput input = await BuildBudgetBalanceInputAsync(reportStart, reportEnd, cancellationToken);
        return BudgetBalanceReportBuilder.Build(input);
    }

    /// <summary>
    /// Monthly mean of attributed income credits over the FR-M019 rolling window.
    /// </summary>
    public async Task<decimal> GetActualIncomeMonthlyMeanAsync(
        DateOnly windowEnd,
        int windowMonths,
        CancellationToken cancellationToken = default)
    {
        if (windowMonths is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(windowMonths));
        }

        BudgetProfileEntity profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .SingleAsync(cancellationToken);

        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);

        Dictionary<Guid, BudgetCategoryKind> categoryKinds = profile.Categories
            .ToDictionary(c => c.Id, c => c.Kind);

        DateOnly windowStart = ActualIncomeCalculator.WindowStart(windowEnd, windowMonths);
        IReadOnlyList<PeriodTransactionInput> windowTransactions = await LoadPeriodTransactionsAsync(
            windowStart,
            windowEnd,
            categoryKinds,
            cancellationToken);

        HashSet<Guid> attributed = await LoadIncomeAttributedTransactionIdsAsync(cancellationToken);

        return ActualIncomeCalculator.MeanMonthlyReceipts(
            windowTransactions,
            windowEnd,
            windowMonths,
            attributed);
    }

    private async Task<PeriodSummaryInput> BuildPeriodSummaryInputAsync(
        DateOnly reportStart,
        DateOnly reportEnd,
        CancellationToken cancellationToken)
    {
        BudgetProfileEntity profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .Include(p => p.Pots)
            .SingleAsync(cancellationToken);

        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);

        Dictionary<Guid, BudgetCategoryKind> categoryKinds = profile.Categories
            .ToDictionary(c => c.Id, c => c.Kind);

        IReadOnlyList<PeriodTransactionInput> transactions = await LoadPeriodTransactionsAsync(
            reportStart,
            reportEnd,
            categoryKinds,
            cancellationToken);

        decimal actualIncome = await GetActualIncomeMonthlyMeanAsync(
            reportEnd,
            profile.ActualIncomeWindowMonths,
            cancellationToken);

        IReadOnlyList<CategoryFundingInput> funding = BuildFundingInputs(profile);
        IReadOnlyList<CategoryCommitmentInput> commitments = await LoadCommitmentsAsync(
            reportStart,
            reportEnd,
            categoryKinds,
            cancellationToken);

        PlanIncome? planIncome = StandingPlanIncomeService.ToPlanIncome(profile);

        return new PeriodSummaryInput(
            profile.SelectedPreset,
            reportStart,
            reportEnd,
            planIncome,
            actualIncome,
            profile.ForecastHorizonMonths,
            funding,
            transactions,
            commitments);
    }

    private async Task<BudgetBalanceReportInput> BuildBudgetBalanceInputAsync(
        DateOnly reportStart,
        DateOnly reportEnd,
        CancellationToken cancellationToken)
    {
        BudgetProfileEntity profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .SingleAsync(cancellationToken);

        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);

        Dictionary<Guid, BudgetCategoryKind> categoryKinds = profile.Categories
            .ToDictionary(c => c.Id, c => c.Kind);

        decimal actualReceipts = await GetActualIncomeMonthlyMeanAsync(
            reportEnd,
            profile.ActualIncomeWindowMonths,
            cancellationToken);

        IReadOnlyList<CommitmentExpenseInput> expenses = await LoadConfirmedExpenseCommitmentsAsync(
            reportStart,
            categoryKinds,
            cancellationToken);

        IReadOnlyList<IncomeScheduleInput> incomeSchedules = await LoadConfirmedIncomeSchedulesAsync(
            reportStart,
            cancellationToken);

        PlanIncome? planIncome = StandingPlanIncomeService.ToPlanIncome(profile);

        return new BudgetBalanceReportInput(
            profile.SelectedPreset,
            reportStart,
            reportEnd,
            planIncome,
            actualReceipts,
            expenses,
            incomeSchedules);
    }

    private async Task<IReadOnlyList<PeriodTransactionInput>> LoadPeriodTransactionsAsync(
        DateOnly start,
        DateOnly end,
        Dictionary<Guid, BudgetCategoryKind> categoryKinds,
        CancellationToken cancellationToken)
    {
        List<TransactionEntity> rows = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.TransactionDate >= start && t.TransactionDate <= end)
            .ToListAsync(cancellationToken);

        return rows
            .Select(t =>
            {
                BudgetCategoryKind? kind = t.CategoryId is Guid categoryId &&
                                         categoryKinds.TryGetValue(categoryId, out BudgetCategoryKind mapped)
                    ? mapped
                    : null;

                return new PeriodTransactionInput(
                    t.Id,
                    t.TransactionDate,
                    t.Kind,
                    t.Amount,
                    kind);
            })
            .ToList();
    }

    private static List<CategoryFundingInput> BuildFundingInputs(BudgetProfileEntity profile)
    {
        Dictionary<Guid, List<PotFundingInput>> potsByCategory = profile.Pots
            .GroupBy(p => p.CategoryId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => new PotFundingInput(p.AllocationToPotAmount, p.RecordedFundingAmount)).ToList());

        return profile.Categories
            .Select(c => new CategoryFundingInput(
                c.Kind,
                c.RecordedFundingDirectAmount,
                potsByCategory.TryGetValue(c.Id, out List<PotFundingInput>? pots) ? pots : []))
            .ToList();
    }

    private async Task<IReadOnlyList<CategoryCommitmentInput>> LoadCommitmentsAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        Dictionary<Guid, BudgetCategoryKind> categoryKinds,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CommitmentExpenseInput> expenses = await LoadConfirmedExpenseCommitmentsAsync(
            periodStart,
            categoryKinds,
            cancellationToken);

        return Potsure.Reporting.CommitmentProjector.ProjectIntoPeriod(periodStart, periodEnd, expenses);
    }

    private async Task<IReadOnlyList<CommitmentExpenseInput>> LoadConfirmedExpenseCommitmentsAsync(
        DateOnly anchorFallback,
        Dictionary<Guid, BudgetCategoryKind> categoryKinds,
        CancellationToken cancellationToken)
    {
        List<RecurringExpenseEntity> expenses = await dbContext.RecurringExpenses
            .AsNoTracking()
            .Where(e => e.State == PatternConfirmationState.Confirmed)
            .ToListAsync(cancellationToken);

        IReadOnlyDictionary<Guid, DateOnly> anchors = await CommitmentPaymentAnchorResolver.ResolveExpenseAnchorsAsync(
            dbContext,
            expenses,
            cancellationToken);

        return expenses
            .Where(e => categoryKinds.ContainsKey(e.CategoryId))
            .Select(e =>
            {
                anchors.TryGetValue(e.Id, out DateOnly historyAnchor);
                DateOnly effective = CommitmentPaymentAnchorResolver.ResolveEffectiveExpenseAnchor(
                    e,
                    historyAnchor,
                    anchorFallback);
                return new CommitmentExpenseInput(
                    categoryKinds[e.CategoryId],
                    e.Cadence,
                    e.AmountKind,
                    e.FixedAmount,
                    e.StoredMonthlyEstimate,
                    effective);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<IncomeScheduleInput>> LoadConfirmedIncomeSchedulesAsync(
        DateOnly anchorFallback,
        CancellationToken cancellationToken)
    {
        List<IncomeSourceEntity> sources = await dbContext.IncomeSources
            .AsNoTracking()
            .Where(i => i.State == PatternConfirmationState.Confirmed)
            .ToListAsync(cancellationToken);

        IReadOnlyDictionary<Guid, DateOnly> anchors = await CommitmentPaymentAnchorResolver.ResolveIncomeAnchorsAsync(
            dbContext,
            sources,
            cancellationToken);

        return sources
            .Select(i => new IncomeScheduleInput(
                i.Cadence,
                i.FixedAmount ?? i.EstimateAmount,
                anchors.TryGetValue(i.Id, out DateOnly anchor) ? anchor : anchorFallback))
            .ToList();
    }

    private async Task<HashSet<Guid>> LoadIncomeAttributedTransactionIdsAsync(
        CancellationToken cancellationToken)
    {
        List<Guid> ids = await dbContext.Assignments
            .AsNoTracking()
            .Where(a => a.IsIncomeAttribution && a.TransactionId != null)
            .Select(a => a.TransactionId!.Value)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}
