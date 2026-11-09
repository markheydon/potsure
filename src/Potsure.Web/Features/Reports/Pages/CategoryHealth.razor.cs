using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Components;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Reporting;
using Potsure.Web.Resources;
using Potsure.Web.Services;

namespace Potsure.Web.Features.Reports.Pages;

public partial class CategoryHealth : IDisposable
{
    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private PeriodReportService ReportService { get; set; } = null!;

    [Inject]
    private CategoryPotBreakdownQuery PotBreakdownQuery { get; set; } = null!;

    [Inject]
    private ReportDateRangeSessionState DateRangeState { get; set; } = null!;

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
            : $"Category measures for {_summary.Value.ReportRange.Start} to {_summary.Value.ReportRange.End}";

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
            PeriodSummary summary = await ReportService.GetPeriodSummaryAsync(start, end);
            _summary = summary;
            _error = null;
            _potBreakdown = await PotBreakdownQuery.ListAsync();
            _categoryTableRows = summary.Categories
                .Select(row => new CategoryPeriodTableRow(row))
                .ToList();
            _categoryChartRows = summary.Categories
                .Select(row => new CategoryHealthChartRow(
                    FormatKind(row.Kind),
                    (double)(row.Allocation?.Amount ?? 0m),
                    (double)row.RecordedFunding.Amount,
                    (double)row.ActualActivity.Amount))
                .ToList();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    private static string FormatKind(BudgetCategoryKind kind) =>
        BudgetCategoryDisplay.Format(kind);

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
