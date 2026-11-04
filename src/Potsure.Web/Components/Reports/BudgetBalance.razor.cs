using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Components;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Infrastructure.Queries;
using Potsure.Infrastructure.Reporting;
using Potsure.Reporting;
using Potsure.Web.Resources;

namespace Potsure.Web.Components.Reports;

public partial class BudgetBalance
{
    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private PeriodReportService ReportService { get; set; } = null!;

    [Inject]
    private CategoryPotBreakdownQuery PotBreakdownQuery { get; set; } = null!;

    [Inject]
    private RecurringSummaryQuery RecurringSummaryQuery { get; set; } = null!;

    private BudgetBalanceReport? _report;
    private IReadOnlyList<CategoryPotBreakdownLine> _potBreakdown = [];
    private IReadOnlyList<ConfirmedExpenseSummaryRow> _variableExpenses = [];
    private IReadOnlyList<BudgetBalanceChartRow> _budgetCompareRows = [];
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            _report = await ReportService.GetBudgetBalanceReportAsync();
            _potBreakdown = await PotBreakdownQuery.ListAsync();
            RecurringSummaryReadModel summary = await RecurringSummaryQuery.LoadAsync();
            _variableExpenses = summary.ConfirmedExpenses
                .Where(e => string.Equals(e.AmountKind, "Variable", StringComparison.OrdinalIgnoreCase))
                .ToList();
            _budgetCompareRows =
            [
                new BudgetBalanceChartRow(
                    ReportCopy.BudgetedIncomeHeading,
                    (double)_report.Value.BudgetedIncome.Amount,
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
        kind switch
        {
            BudgetCategoryKind.Needs => "Needs",
            BudgetCategoryKind.Wants => "Wants",
            BudgetCategoryKind.SavingsAndDebt => "Savings and debt",
            _ => kind.ToString(),
        };

    private sealed record BudgetBalanceChartRow(string Label, double Amount, string Fill);
}
