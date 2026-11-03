using Microsoft.AspNetCore.Components;
using Potsure.Budgeting;
using Potsure.Infrastructure.Services;

namespace Potsure.Web.Components.Recurrence;

public partial class DetectionReview
{
    [Inject]
    private RecurrenceReviewService ReviewService { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private List<RecurrenceReviewItem> _suggested = [];
    private List<RecurrenceReviewItem> _confirmedOrRejected = [];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            IReadOnlyList<RecurrenceReviewItem> items = await ReviewService.GetReviewItemsAsync();
            _suggested = items.Where(i => i.State == PatternConfirmationState.Suggested).ToList();
            _confirmedOrRejected = items.Where(i => i.State != PatternConfirmationState.Suggested).ToList();
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

    private static string FormatCadence(RecurrenceReviewItem item) =>
        item.IsIncome
            ? item.IncomeCadence?.ToString() ?? "Unknown"
            : item.ExpenseCadence?.ToString() ?? "Unknown";
}
