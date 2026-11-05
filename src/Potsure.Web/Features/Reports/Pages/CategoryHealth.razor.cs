using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Components;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Infrastructure.Queries;
using Potsure.Infrastructure.Reporting;
using Potsure.Reporting;

namespace Potsure.Web.Features.Reports.Pages;

public partial class CategoryHealth
{
    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private PeriodReportService ReportService { get; set; } = null!;

    [Inject]
    private CategoryPotBreakdownQuery PotBreakdownQuery { get; set; } = null!;

    private PeriodSummary? _summary;
    private IReadOnlyList<CategoryPotBreakdownLine> _potBreakdown = [];
    private List<CategoryPeriodTableRow> _categoryTableRows = [];
    private IReadOnlyList<CategoryHealthChartRow> _categoryChartRows = [];
    private ChartConfig _categoryChartConfig = ChartConfig.Create(
        ("Allocation", new ChartSeriesConfig { Label = "Allocation", Color = "var(--chart-1)" }),
        ("RecordedFunding", new ChartSeriesConfig { Label = "Recorded funding", Color = "var(--chart-2)" }),
        ("Actual", new ChartSeriesConfig { Label = "Actual activity", Color = "var(--chart-3)" }));
    private bool _loading = true;
    private string? _error;

    private string CategoryTableAriaLabel =>
        _summary is null
            ? "Category measures"
            : $"Category measures for {_summary.Value.Period.Start} to {_summary.Value.Period.End}";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            PeriodSummary summary = await ReportService.GetPeriodSummaryAsync();
            _summary = summary;
            _potBreakdown = await PotBreakdownQuery.ListAsync();
            _categoryTableRows = summary.Categories
                .Select(row => new CategoryPeriodTableRow(row))
                .ToList();
            _categoryChartRows = summary.Categories
                .Select(row => new CategoryHealthChartRow(
                    FormatKind(row.Kind),
                    (double)row.Allocation.Amount,
                    (double)row.RecordedFunding.Amount,
                    (double)row.ActualActivity.Amount))
                .ToList();
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

    private sealed record CategoryHealthChartRow(
        string Category,
        double Allocation,
        double RecordedFunding,
        double Actual);

    private sealed class CategoryPeriodTableRow(CategoryPeriodSummary summary)
    {
        public CategoryPeriodSummary Summary { get; } = summary;
    }
}
