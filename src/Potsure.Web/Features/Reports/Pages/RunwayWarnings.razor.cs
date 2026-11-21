using Microsoft.AspNetCore.Components;
using Potsure.Budgeting;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Infrastructure.Features.Recurrence;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Reporting;
using Potsure.Web.Resources;
using Potsure.Web.Services;

namespace Potsure.Web.Features.Reports.Pages;

public partial class RunwayWarnings : IDisposable
{
    [Inject]
    private RunwayReportService RunwayReportService { get; set; } = null!;

    [Inject]
    private ForecastHorizonService HorizonService { get; set; } = null!;

    [Inject]
    private CategoryPotBreakdownQuery PotBreakdownQuery { get; set; } = null!;

    [Inject]
    private RecurringSummaryQuery RecurringSummaryQuery { get; set; } = null!;

    [Inject]
    private ReportDateRangeSessionState DateRangeState { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private int _horizonMonths = ForecastHorizonService.DefaultHorizonMonths;
    private IReadOnlyList<RunwayProjectionLine> _lines = [];
    private List<RunwayTableRow> _runwayTableRows = [];
    private IReadOnlyList<CategoryPotBreakdownLine> _potBreakdown = [];
    private IReadOnlyList<ConfirmedExpenseSummaryRow> _variableExpenses = [];
    private UncertaintyFlags _flags;

    protected override async Task OnInitializedAsync()
    {
        DateRangeState.Changed += OnDateRangeChanged;
        try
        {
            _horizonMonths = await HorizonService.GetHorizonMonthsAsync();
            _potBreakdown = await PotBreakdownQuery.ListAsync();
            RecurringSummaryReadModel summary = await RecurringSummaryQuery.LoadAsync();
            _variableExpenses = summary.ConfirmedExpenses
                .Where(e => e.AmountKind == ExpenseAmountKind.Variable)
                .ToList();
            await LoadRunwayAsync();
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

    public void Dispose()
    {
        DateRangeState.Changed -= OnDateRangeChanged;
        GC.SuppressFinalize(this);
    }

    private async void OnDateRangeChanged() =>
        await InvokeAsync(async () =>
        {
            await LoadRunwayAsync();
            StateHasChanged();
        });

    private async Task SaveHorizonAsync()
    {
        try
        {
            await HorizonService.SetHorizonMonthsAsync(_horizonMonths);
            await LoadRunwayAsync();
            _error = null;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    private async Task LoadRunwayAsync()
    {
        (DateOnly start, DateOnly end) = DateRangeState.GetRange();
        (IReadOnlyList<RunwayProjectionLine> lines, UncertaintyFlags flags) =
            await RunwayReportService.GetRunwayAsync(start, end);
        _lines = lines;
        _runwayTableRows = lines.Select(line => new RunwayTableRow(line)).ToList();
        _flags = flags;
    }

    private static string FormatKind(BudgetCategoryKind kind) =>
        BudgetCategoryDisplay.Format(kind);

    private sealed class RunwayTableRow(RunwayProjectionLine line)
    {
        public RunwayProjectionLine Line { get; } = line;
    }

    private static readonly Dictionary<string, object> HorizonInputAttributes = new()
    {
        ["min"] = 1,
        ["max"] = 12,
    };

    private static int ParseHorizonMonths(string? value, int fallback)
    {
        if (!int.TryParse(value, out int parsed))
        {
            return fallback;
        }

        return Math.Clamp(parsed, 1, 12);
    }
}
