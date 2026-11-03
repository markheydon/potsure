using Microsoft.AspNetCore.Components;
using Potsure.Infrastructure.Reporting;
using Potsure.Infrastructure.Services;
using Potsure.Reporting;

namespace Potsure.Web.Components.Reports;

public partial class RunwayWarnings
{
    [Inject]
    private RunwayReportService RunwayReportService { get; set; } = null!;

    [Inject]
    private ForecastHorizonService HorizonService { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private int _horizonMonths = ForecastHorizonService.DefaultHorizonMonths;
    private IReadOnlyList<RunwayProjectionLine> _lines = [];
    private UncertaintyFlags _flags;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _horizonMonths = await HorizonService.GetHorizonMonthsAsync();
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
        (IReadOnlyList<RunwayProjectionLine> lines, UncertaintyFlags flags) = await RunwayReportService.GetRunwayAsync();
        _lines = lines;
        _flags = flags;
    }
}
