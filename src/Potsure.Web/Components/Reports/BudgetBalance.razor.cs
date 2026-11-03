using Microsoft.AspNetCore.Components;
using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Queries;
using Potsure.Infrastructure.Reporting;
using Potsure.Reporting;

namespace Potsure.Web.Components.Reports;

public partial class BudgetBalance
{
    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private PeriodReportService ReportService { get; set; } = null!;

    [Inject]
    private CategoryPotBreakdownQuery PotBreakdownQuery { get; set; } = null!;

    private BudgetBalanceReport? _report;
    private IReadOnlyList<CategoryPotBreakdownLine> _potBreakdown = [];
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            _report = await ReportService.GetBudgetBalanceReportAsync();
            _potBreakdown = await PotBreakdownQuery.ListAsync();
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
}
