using Microsoft.AspNetCore.Components;
using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Reporting;
using Potsure.Infrastructure.Services;
using Potsure.Reporting;
using Potsure.Web.Resources;

namespace Potsure.Web.Components.Plan;

public partial class PlanSettings
{
    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private BudgetPeriodService PeriodService { get; set; } = null!;

    [Inject]
    private AllocationPresetService PresetService { get; set; } = null!;

    [Inject]
    private PeriodReportService ReportService { get; set; } = null!;

    [Inject]
    private BudgetedIncomeSuggestionService SuggestionService { get; set; } = null!;

    private readonly PlanSettingsForm _form = new();
    private BudgetedIncomeSuggestion? _incomeSuggestion;
    private bool _loading = true;
    private bool _saving;
    private string? _error;
    private decimal? _actualReceiptsInPeriod;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            BudgetPeriodReadModel? period = await PeriodService.GetWorkingPeriodAsync();
            AllocationPreset? preset = await PresetService.GetSelectedPresetAsync();
            if (period is not null && preset is not null)
            {
                _form.StartDate = period.StartDate.ToDateTime(TimeOnly.MinValue);
                _form.EndDate = period.EndDate.ToDateTime(TimeOnly.MinValue);
                _form.BudgetedIncome = period.BudgetedIncomeAmount;
                _form.Preset = preset.Value;
            }

            PeriodSummary summary = await ReportService.GetPeriodSummaryAsync();
            _actualReceiptsInPeriod = summary.ActualIncomeInPeriod.Amount;
            _incomeSuggestion = await SuggestionService.GetSuggestionAsync();
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

    private async Task SaveAsync()
    {
        _saving = true;
        _error = null;
        try
        {
            DateOnly start = DateOnly.FromDateTime(_form.StartDate);
            DateOnly end = DateOnly.FromDateTime(_form.EndDate);
            await PresetService.SwitchPresetAsync(_form.Preset);
            await PeriodService.UpdateWorkingPeriodAsync(
                new UpdateBudgetPeriodRequest(start, end, _form.BudgetedIncome));

            PeriodSummary summary = await ReportService.GetPeriodSummaryAsync();
            _actualReceiptsInPeriod = summary.ActualIncomeInPeriod.Amount;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }

    private static string FormatPreset(AllocationPreset preset) =>
        preset switch
        {
            AllocationPreset.FiftyThirtyTwenty => "50 / 30 / 20",
            AllocationPreset.SeventyFiveFifteenTen => "75 / 15 / 10",
            _ => preset.ToString(),
        };

    private sealed class PlanSettingsForm
    {
        public AllocationPreset Preset { get; set; } = AllocationPreset.FiftyThirtyTwenty;

        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

        public DateTime EndDate { get; set; } = DateTime.UtcNow.Date;

        public decimal BudgetedIncome { get; set; }
    }
}
