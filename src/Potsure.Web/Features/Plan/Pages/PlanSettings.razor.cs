using System.Globalization;
using Microsoft.AspNetCore.Components;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Reporting;
using Potsure.Web.Resources;

namespace Potsure.Web.Features.Plan.Pages;

public partial class PlanSettings
{
    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private StandingPlanIncomeService StandingPlanIncomeService { get; set; } = null!;

    [Inject]
    private AllocationPresetService PresetService { get; set; } = null!;

    [Inject]
    private PeriodReportService ReportService { get; set; } = null!;

    [Inject]
    private BudgetedIncomeSuggestionService SuggestionService { get; set; } = null!;

    private readonly PlanSettingsForm _form = new();
    private EstimatedIncomeSuggestion? _incomeSuggestion;
    private bool _planIncomeComplete;
    private bool _loading = true;
    private bool _saving;
    private string? _error;
    private string? _saveMessage;
    private decimal? _actualReceiptsInWindow;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            StandingPlanIncomeReadModel? standingPlan = await StandingPlanIncomeService.GetAsync();
            AllocationPreset? preset = await PresetService.GetSelectedPresetAsync();
            if (preset is not null)
            {
                _form.Preset = preset.Value;
            }

            if (standingPlan is not null)
            {
                _planIncomeComplete = standingPlan.IsComplete;
                _form.EstimatedIncomeAmount = standingPlan.EstimatedIncomeAmount ?? 0m;
                _form.EstimatedIncomeCadence = standingPlan.EstimatedIncomeCadence;
                _form.ActualIncomeWindowMonths = standingPlan.ActualIncomeWindowMonths;
            }

            await RefreshActualReceiptsAsync();
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
        _saveMessage = null;
        try
        {
            if (_form.EstimatedIncomeCadence is null)
            {
                _error = "Choose an income cadence before saving estimated income.";
                return;
            }

            await PresetService.SwitchPresetAsync(_form.Preset);
            await StandingPlanIncomeService.UpdateAsync(
                new UpdateStandingPlanIncomeRequest(
                    _form.EstimatedIncomeAmount,
                    _form.EstimatedIncomeCadence.Value,
                    _form.ActualIncomeWindowMonths));

            _planIncomeComplete = true;
            await RefreshActualReceiptsAsync();
            _saveMessage = "Your preset, estimated income, and actual receipts window were updated.";
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

    private async Task RefreshActualReceiptsAsync()
    {
        DateOnly end = DateOnly.FromDateTime(DateTime.UtcNow);
        _actualReceiptsInWindow = await ReportService.GetActualIncomeMonthlyMeanAsync(
            end,
            _form.ActualIncomeWindowMonths);
    }

    private static (DateOnly Start, DateOnly End) ActualReceiptsWindow(int windowMonths)
    {
        int clamped = Math.Clamp(windowMonths, 1, 12);
        DateOnly end = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly start = end.AddMonths(-(clamped - 1));
        start = new DateOnly(start.Year, start.Month, 1);
        return (start, end);
    }

    private static string FormatPreset(AllocationPreset preset) =>
        preset switch
        {
            AllocationPreset.FiftyThirtyTwenty => "50 / 30 / 20",
            AllocationPreset.SeventyFiveFifteenTen => "75 / 15 / 10",
            _ => preset.ToString(),
        };

    private static string FormatCadence(PlanIncomeCadence cadence) =>
        cadence switch
        {
            PlanIncomeCadence.Weekly => "Weekly",
            PlanIncomeCadence.Fortnightly => "Fortnightly",
            PlanIncomeCadence.Monthly => "Monthly",
            PlanIncomeCadence.Quarterly => "Quarterly",
            PlanIncomeCadence.Yearly => "Yearly",
            _ => cadence.ToString(),
        };

    private static string FormatDecimal(decimal value) =>
        value.ToString("G29", CultureInfo.InvariantCulture);

    private static decimal ParseDecimal(string? value, decimal fallback) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed)
            ? parsed
            : fallback;

    private void OnActualIncomeWindowMonthsChanged(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            _form.ActualIncomeWindowMonths = Math.Clamp(parsed, 1, 12);
        }
    }

    private sealed class PlanSettingsForm
    {
        public AllocationPreset Preset { get; set; } = AllocationPreset.FiftyThirtyTwenty;

        public decimal EstimatedIncomeAmount { get; set; }

        public PlanIncomeCadence? EstimatedIncomeCadence { get; set; }

        public int ActualIncomeWindowMonths { get; set; } = 3;
    }
}
