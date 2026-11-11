using System.Globalization;
using Microsoft.AspNetCore.Components;
using Potsure.Budgeting;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Web.Resources;

namespace Potsure.Web.Features.Plan.Pages;

public partial class RecordedFunding
{
    [Inject]
    private RecordedFundingService RecordedFundingService { get; set; } = null!;

    [Inject]
    private PlannedFundingService PlannedFundingService { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private List<FundingFormRow> _rows = [];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadAsync();
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

    private async Task LoadAsync()
    {
        IReadOnlyList<RecordedFundingReadModel> recorded = await RecordedFundingService.GetAsync();
        IReadOnlyList<PlannedFundingReadModel> planned = await PlannedFundingService.GetAsync();

        _rows = BudgetCategoryDisplay
            .OrderByStrategy(
                recorded.Select(r =>
                {
                    PlannedFundingReadModel plan = planned.Single(p => p.CategoryId == r.CategoryId);
                    return new FundingFormRow(
                        r.CategoryId,
                        r.Kind,
                        r.RecordedFundingDirect,
                        plan.PlannedFutureFundingDirect,
                        r.Pots
                            .OrderBy(p => p.PotName, StringComparer.OrdinalIgnoreCase)
                            .Select(p => new PotFundingForm(p.PotId, p.PotName, p.RecordedFunding))
                            .ToList());
                }),
                r => r.Kind)
            .ToList();
    }

    private async Task SaveRowAsync(FundingFormRow row)
    {
        try
        {
            await RecordedFundingService.UpdateAsync(new UpdateRecordedFundingRequest(
                row.CategoryId,
                row.RecordedDirect,
                row.Pots.Select(p => (p.PotId, p.Recorded)).ToList()));

            await PlannedFundingService.UpdateAsync(new UpdatePlannedFundingRequest(
                row.CategoryId,
                row.PlannedDirect,
                row.Pots.Select(p => (p.PotId, (decimal?)null)).ToList()));

            _error = null;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    private sealed class FundingFormRow(
        Guid categoryId,
        BudgetCategoryKind kind,
        decimal recordedDirect,
        decimal plannedDirect,
        List<PotFundingForm> pots)
    {
        public Guid CategoryId { get; } = categoryId;

        public BudgetCategoryKind Kind { get; } = kind;

        public decimal RecordedDirect { get; set; } = recordedDirect;

        public decimal PlannedDirect { get; set; } = plannedDirect;

        public List<PotFundingForm> Pots { get; } = pots;
    }

    private sealed class PotFundingForm(Guid potId, string name, decimal recorded)
    {
        public Guid PotId { get; } = potId;

        public string Name { get; } = name;

        public decimal Recorded { get; set; } = recorded;
    }

    private static string FormatDecimal(decimal value) =>
        value.ToString("G29", CultureInfo.InvariantCulture);

    private static decimal ParseDecimal(string? value, decimal fallback) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed)
            ? parsed
            : fallback;
}
