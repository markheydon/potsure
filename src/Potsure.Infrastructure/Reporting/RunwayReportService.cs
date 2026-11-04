using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Reporting;

namespace Potsure.Infrastructure.Reporting;

/// <summary>
/// Loads persisted data and builds runway projection output for the UI.
/// </summary>
public sealed class RunwayReportService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    /// <summary>
    /// Builds runway lines and uncertainty flags for the working period.
    /// </summary>
    public async Task<(IReadOnlyList<RunwayProjectionLine> Lines, UncertaintyFlags Flags)> GetRunwayAsync(
        CancellationToken cancellationToken = default)
    {
        BudgetProfileEntity profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.BudgetPeriod)
            .Include(p => p.Categories)
            .Include(p => p.Pots)
            .SingleAsync(cancellationToken);
        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);

        BudgetPeriodEntity period = profile.BudgetPeriod;
        Dictionary<Guid, BudgetCategoryKind> categoryKinds = profile.Categories
            .ToDictionary(c => c.Id, c => c.Kind);

        IReadOnlyList<CommitmentExpenseInput> expenses = await LoadConfirmedExpenseCommitmentsAsync(
            period.StartDate,
            categoryKinds,
            cancellationToken);

        IReadOnlyList<CategoryFundingInput> funding = BuildFundingInputs(profile);

        IReadOnlyList<PeriodTransactionInput> transactions = await LoadPeriodTransactionsAsync(
            period.StartDate,
            period.EndDate,
            categoryKinds,
            cancellationToken);

        IReadOnlyList<CategoryActualInput> actuals = ActualActivityCalculator.Calculate(
            period.StartDate,
            period.EndDate,
            transactions);
        Money uncategorised = UncategorisedSpendCalculator.Calculate(
            period.StartDate,
            period.EndDate,
            transactions);

        RunwayProjectionInput input = new(
            profile.SelectedPreset,
            period.StartDate,
            period.EndDate,
            profile.ForecastHorizonMonths,
            period.BudgetedIncomeAmount,
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
            period.StartDate,
            cancellationToken);

        UncertaintyFlags flags = UncertaintyFlagsBuilder.Build(
            period.StartDate,
            period.EndDate,
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
            .Select(e => new CommitmentExpenseInput(
                categoryKinds[e.CategoryId],
                e.Cadence,
                string.Equals(e.AmountKind, "Variable", StringComparison.OrdinalIgnoreCase),
                e.FixedAmount,
                e.MonthlyEstimate,
                anchors.TryGetValue(e.Id, out DateOnly anchor) ? anchor : anchorFallback))
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
