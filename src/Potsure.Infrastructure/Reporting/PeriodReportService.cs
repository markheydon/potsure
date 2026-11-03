using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Data.Tenancy;
using Potsure.Reporting;

namespace Potsure.Infrastructure.Reporting;

/// <summary>
/// Loads persisted plan data and builds period reports for the current owner.
/// </summary>
public sealed class PeriodReportService(
    PotsureDbContext dbContext,
    ICurrentOwnerAccessor ownerAccessor)
{
    /// <summary>
    /// Builds the category health period summary for the working period.
    /// </summary>
    public async Task<PeriodSummary> GetPeriodSummaryAsync(CancellationToken cancellationToken = default)
    {
        PeriodSummaryInput input = await BuildPeriodSummaryInputAsync(cancellationToken);
        return PeriodSummaryBuilder.Build(input);
    }

    /// <summary>
    /// Builds the budget balance report for the working period.
    /// </summary>
    public async Task<BudgetBalanceReport> GetBudgetBalanceReportAsync(CancellationToken cancellationToken = default)
    {
        BudgetBalanceReportInput input = await BuildBudgetBalanceInputAsync(cancellationToken);
        return BudgetBalanceReportBuilder.Build(input);
    }

    private async Task<PeriodSummaryInput> BuildPeriodSummaryInputAsync(CancellationToken cancellationToken)
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

        IReadOnlyList<PeriodTransactionInput> transactions = await LoadPeriodTransactionsAsync(
            period.StartDate,
            period.EndDate,
            categoryKinds,
            cancellationToken);

        decimal actualIncome = transactions
            .Where(t => t.Kind == TransactionKind.IncomeCredit)
            .Sum(t => Math.Abs(t.SignedAmount));

        IReadOnlyList<CategoryFundingInput> funding = BuildFundingInputs(profile);
        IReadOnlyList<CategoryCommitmentInput> commitments = await LoadCommitmentsAsync(
            period.StartDate,
            period.EndDate,
            categoryKinds,
            cancellationToken);

        return new PeriodSummaryInput(
            profile.SelectedPreset,
            period.StartDate,
            period.EndDate,
            period.BudgetedIncomeAmount,
            actualIncome,
            profile.ForecastHorizonMonths,
            funding,
            transactions,
            commitments);
    }

    private async Task<BudgetBalanceReportInput> BuildBudgetBalanceInputAsync(CancellationToken cancellationToken)
    {
        BudgetProfileEntity profile = await dbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.BudgetPeriod)
            .Include(p => p.Categories)
            .SingleAsync(cancellationToken);

        EntityOwnershipValidator.EnsureOwnedBy(profile.OwnerKey, ownerAccessor.OwnerKey);
        BudgetPeriodEntity period = profile.BudgetPeriod;

        Dictionary<Guid, BudgetCategoryKind> categoryKinds = profile.Categories
            .ToDictionary(c => c.Id, c => c.Kind);

        IReadOnlyList<PeriodTransactionInput> transactions = await LoadPeriodTransactionsAsync(
            period.StartDate,
            period.EndDate,
            categoryKinds,
            cancellationToken);

        decimal actualIncome = transactions
            .Where(t => t.Kind == TransactionKind.IncomeCredit)
            .Sum(t => Math.Abs(t.SignedAmount));

        IReadOnlyList<CommitmentExpenseInput> expenses = await LoadConfirmedExpenseCommitmentsAsync(
            period.StartDate,
            categoryKinds,
            cancellationToken);

        IReadOnlyList<IncomeScheduleInput> incomeSchedules = await LoadConfirmedIncomeSchedulesAsync(
            period.StartDate,
            cancellationToken);

        return new BudgetBalanceReportInput(
            profile.SelectedPreset,
            period.StartDate,
            period.EndDate,
            period.BudgetedIncomeAmount,
            actualIncome,
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

        return CommitmentProjector.ProjectIntoPeriod(periodStart, periodEnd, expenses);
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

        return expenses
            .Where(e => categoryKinds.ContainsKey(e.CategoryId))
            .Select(e => new CommitmentExpenseInput(
                categoryKinds[e.CategoryId],
                e.Cadence,
                string.Equals(e.AmountKind, "Variable", StringComparison.OrdinalIgnoreCase),
                e.FixedAmount,
                e.MonthlyEstimate,
                anchorFallback))
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

        return sources
            .Select(i => new IncomeScheduleInput(i.Cadence, i.FixedAmount ?? i.EstimateAmount, anchorFallback))
            .ToList();
    }
}
