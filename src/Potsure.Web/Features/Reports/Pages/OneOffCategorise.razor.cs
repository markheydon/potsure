using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Web.Resources;

namespace Potsure.Web.Features.Reports.Pages;

public partial class OneOffCategorise
{
    [Inject]
    private UncategorisedTransactionQuery TransactionQuery { get; set; } = null!;

    [Inject]
    private OneOffCategorisationService CategorisationService { get; set; } = null!;

    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private PotsureDbContext DbContext { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private string? _statusMessage;
    private List<UncategorisedTransactionRow> _transactions = [];
    private List<CategoryOption> _categories = [];
    private Dictionary<Guid, Guid> _selectedCategoryByTransaction = [];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            await LoadCategoriesAsync();
            await ReloadTransactionsAsync();
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

    private string CategoryLabel(Guid categoryId) =>
        _categories.FirstOrDefault(c => c.Id == categoryId) is { } category
            ? BudgetCategoryDisplay.Format(category.Kind)
            : "Category";

    private async Task ReloadTransactionsAsync()
    {
        _transactions = (await TransactionQuery.ListForWorkingPeriodAsync()).ToList();
        foreach (UncategorisedTransactionRow row in _transactions)
        {
            if (!_selectedCategoryByTransaction.ContainsKey(row.TransactionId) && _categories.Count > 0)
            {
                _selectedCategoryByTransaction[row.TransactionId] = _categories[0].Id;
            }
        }
    }

    private async Task LoadCategoriesAsync()
    {
        _categories = await DbContext.BudgetCategories
            .AsNoTracking()
            .OrderBy(c => c.Kind)
            .Select(c => new CategoryOption(c.Id, c.Kind))
            .ToListAsync();
    }

    private async Task AssignAsync(Guid transactionId)
    {
        _statusMessage = null;
        if (!_selectedCategoryByTransaction.TryGetValue(transactionId, out Guid categoryId))
        {
            return;
        }

        try
        {
            await CategorisationService.CategoriseAsync(
                new OneOffCategorisationRequest(transactionId, categoryId, MappedPotId: null));
            _statusMessage = "Assignment saved.";
            await ReloadTransactionsAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
        }
    }

    private async Task ClearAsync(Guid transactionId)
    {
        _statusMessage = null;
        try
        {
            await CategorisationService.ClearAsync(transactionId);
            _statusMessage = "Assignment cleared.";
            await ReloadTransactionsAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
        }
    }

    private static string FormatKind(TransactionKind kind) =>
        kind switch
        {
            TransactionKind.InternalTransfer => "Internal transfer",
            TransactionKind.PotMovement => "Pot movement",
            TransactionKind.RefundCredit => "Refund",
            TransactionKind.IncomeCredit => "Income credit",
            _ => "Expense",
        };

    private sealed record CategoryOption(Guid Id, BudgetCategoryKind Kind);

}
