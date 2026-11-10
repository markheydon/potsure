using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Reporting;

namespace Potsure.Infrastructure.Features.Reports;

/// <summary>
/// Loads persisted data and builds runway projection output for the UI.
/// </summary>
public sealed class RunwayReportService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    public async Task<(IReadOnlyList<RunwayProjectionLine> Lines, UncertaintyFlags Flags)> GetRunwayAsync(
        DateOnly reportStart,
        DateOnly reportEnd,
        CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .Include(p => p.Pots)
            .SingleAsync(cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);

        Dictionary<Guid, BudgetCategoryKind> categoryKinds = profile.Categories
            .ToDictionary(c => c.Id, c => c.Kind);

        IReadOnlyList<CommitmentExpenseInput> expenses = await LoadConfirmedExpenseCommitmentsAsync(
            reportStart,
            categoryKinds,
            cancellationToken);

        IReadOnlyList<CategoryFundingInput> funding = BuildFundingInputs(profile);

        IReadOnlyList<PeriodTransactionInput> transactions = await LoadPeriodTransactionsAsync(
            reportStart,
            reportEnd,
            categoryKinds,
            cancellationToken);

        IReadOnlyList<CategoryActualInput> actuals = ActualActivityCalculator.Calculate(
            reportStart,
            reportEnd,
            transactions);
        Money uncategorised = UncategorisedSpendCalculator.Calculate(
            reportStart,
            reportEnd,
            transactions);

        PlanIncome? planIncome = StandingPlanIncomeService.ToPlanIncome(profile);

        RunwayProjectionInput input = new(
            profile.SelectedPreset,
            reportStart,
            reportEnd,
            profile.ForecastHorizonMonths,
            planIncome,
            funding,
            actuals,
            uncategorised,
            expenses);

        IReadOnlyList<RunwayProjectionLine> lines = RunwayProjector.Project(input);

        List<DateOnly> coverageDays = await dbContext.Transactions
            .AsNoTracking()
            .Select(t => t.TransactionDate)
            .Distinct()
            .ToListAsync(cancellationToken);

        IReadOnlyList<IncomeScheduleInput> incomeSchedules = await LoadConfirmedIncomeSchedulesAsync(
            reportStart,
            cancellationToken);

        UncertaintyFlags flags = UncertaintyFlagsBuilder.Build(
            reportStart,
            reportEnd,
            coverageDays,
            incomeSchedules);

        return (lines, flags);
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
}
