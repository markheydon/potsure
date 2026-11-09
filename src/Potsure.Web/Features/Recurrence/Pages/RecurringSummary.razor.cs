using Microsoft.AspNetCore.Components;
using Potsure.Infrastructure.Features.Recurrence;

namespace Potsure.Web.Features.Recurrence.Pages;

public partial class RecurringSummary
{
    [Inject]
    private RecurringSummaryQuery SummaryQuery { get; set; } = null!;

    [Inject]
    private IncomePatternService IncomeService { get; set; } = null!;

    [Inject]
    private ExpensePatternService ExpenseService { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private string? _statusMessage;
    private RecurringSummaryReadModel? _model;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        _loading = true;
        _error = null;
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

    private async Task DeleteIncomeAsync(Guid incomeId)
    {
        _statusMessage = null;
        try
        {
            await IncomeService.DeleteConfirmedAsync(incomeId);
            _statusMessage = "Income pattern deleted.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
        }
    }

    private async Task DeleteExpenseAsync(Guid expenseId)
    {
        _statusMessage = null;
        try
        {
            await ExpenseService.DeleteConfirmedAsync(expenseId);
            _statusMessage = "Expense pattern deleted.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
        }
    }
}
