using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Components;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Infrastructure.Features.Recurrence;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Reporting;
using Potsure.Web.Resources;
using Potsure.Web.Services;

namespace Potsure.Web.Features.Reports.Pages;

public partial class BudgetBalance : IDisposable
{
    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private PeriodReportService ReportService { get; set; } = null!;

    [Inject]
    private CategoryPotBreakdownQuery PotBreakdownQuery { get; set; } = null!;

    [Inject]
    private RecurringSummaryQuery RecurringSummaryQuery { get; set; } = null!;

    [Inject]
    private ReportDateRangeSessionState DateRangeState { get; set; } = null!;

    private BudgetBalanceReport? _report;
    private IReadOnlyList<CategoryPotBreakdownLine> _potBreakdown = [];
    private IReadOnlyList<ConfirmedExpenseSummaryRow> _variableExpenses = [];
    private IReadOnlyList<BudgetBalanceChartRow> _budgetCompareRows = [];
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        DateRangeState.Changed += OnDateRangeChanged;
        await LoadReportAsync();
    }

    public void Dispose()
    {
        DateRangeState.Changed -= OnDateRangeChanged;
        GC.SuppressFinalize(this);
    }

    private async void OnDateRangeChanged() =>
        await InvokeAsync(async () =>
        {
            _loading = true;
            await LoadReportAsync();
            _loading = false;
            StateHasChanged();
        });

    private async Task LoadReportAsync()
    {
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            (DateOnly start, DateOnly end) = DateRangeState.GetRange();
            _report = await ReportService.GetBudgetBalanceReportAsync(start, end);
            _error = null;
            _potBreakdown = await PotBreakdownQuery.ListAsync();
            RecurringSummaryReadModel summary = await RecurringSummaryQuery.LoadAsync();
            _variableExpenses = summary.ConfirmedExpenses
                .Where(e => e.AmountKind == ExpenseAmountKind.Variable)
                .ToList();
            double estimatedInRange = (double)(_report.Value.EstimatedIncomeInRange?.Amount ?? 0m);
            _budgetCompareRows =
            [
                new BudgetBalanceChartRow(
                    ReportCopy.BudgetedIncomeHeading,
                    estimatedInRange,
                    "var(--chart-1)"),
                new BudgetBalanceChartRow(
                    "Required total",
                    (double)_report.Value.RequiredIncome.RequiredTotal.Amount,
                    "var(--chart-2)"),
            ];
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private static string FormatKind(BudgetCategoryKind kind) =>
        BudgetCategoryDisplay.Format(kind);

    private sealed record BudgetBalanceChartRow(string Label, double Amount, string Fill);
}
