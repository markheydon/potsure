using Microsoft.AspNetCore.Components;
using Potsure.Infrastructure.Queries;

namespace Potsure.Web.Features.Recurrence.Pages;

public partial class RecurringSummary
{
    [Inject]
    private RecurringSummaryQuery SummaryQuery { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private RecurringSummaryReadModel? _model;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _model = await SummaryQuery.LoadAsync();
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
}
