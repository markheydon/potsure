using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Web.Resources;
using Potsure.Web.Services;
using BankPotReadModel = Potsure.Infrastructure.Features.Plan.BankPotReadModel;

namespace Potsure.Web.Features.Reports.Pages;

public partial class OneOffCategorise : IDisposable
{
    [Inject]
    private UncategorisedTransactionQuery TransactionQuery { get; set; } = null!;

    [Inject]
    private OneOffCategorisationService CategorisationService { get; set; } = null!;

    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private BankPotService PotService { get; set; } = null!;

    [Inject]
    private PotsureDbContext DbContext { get; set; } = null!;

    [Inject]
    private ReportDateRangeSessionState DateRangeState { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private string? _statusMessage;
    private List<UncategorisedTransactionRow> _transactions = [];
    private List<CategoryOption> _categories = [];
    private List<PotOption> _pots = [];
    private PotMappingMode _potMappingMode = PotMappingMode.CategoryOnly;
    private Dictionary<Guid, Guid> _selectedCategoryByTransaction = [];
    private Dictionary<Guid, Guid?> _selectedPotByTransaction = [];

    protected override async Task OnInitializedAsync()
    {
        DateRangeState.Changed += OnDateRangeChanged;
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            await LoadPotMappingModeAsync();
            await LoadCategoriesAsync();
            await LoadPotsAsync();
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

    public void Dispose()
    {
        DateRangeState.Changed -= OnDateRangeChanged;
        GC.SuppressFinalize(this);
    }

    private async void OnDateRangeChanged() =>
        await InvokeAsync(async () =>
        {
            await ReloadTransactionsAsync();
            StateHasChanged();
        });

    private async Task LoadPotMappingModeAsync()
    {
        _potMappingMode = await DbContext.BudgetProfiles
            .AsNoTracking()
            .Select(p => p.PotMappingMode)
            .SingleAsync();
    }

    private async Task LoadPotsAsync()
    {
        IReadOnlyList<BankPotReadModel> pots = await PotService.ListAsync();
        _pots = pots.Select(p => new PotOption(p.Id, p.Name, p.CategoryId)).ToList();
    }

    private bool ShowPotUi => _potMappingMode != PotMappingMode.CategoryOnly;

    private string CategoryLabel(Guid categoryId) =>
        _categories.FirstOrDefault(c => c.Id == categoryId) is { } category
            ? BudgetCategoryDisplay.Format(category.Kind)
            : "Category";

    private string PotSelectionLabel(Guid transactionId)
    {
        if (!_selectedPotByTransaction.TryGetValue(transactionId, out Guid? pot) || pot is not Guid id || id == Guid.Empty)
        {
            return "None";
        }

        return _pots.FirstOrDefault(p => p.Id == id)?.Name ?? "None";
    }

    private Guid GetPotSelection(Guid transactionId) =>
        _selectedPotByTransaction.TryGetValue(transactionId, out Guid? pot) && pot is Guid id ? id : Guid.Empty;

    private void SetPotSelection(Guid transactionId, Guid value) =>
        _selectedPotByTransaction[transactionId] = value == Guid.Empty ? null : value;

    private async Task ReloadTransactionsAsync()
    {
        (DateOnly start, DateOnly end) = DateRangeState.GetRange();
        _transactions = (await TransactionQuery.ListForReportRangeAsync(start, end)).ToList();
        foreach (UncategorisedTransactionRow row in _transactions)
        {
            if (!_selectedCategoryByTransaction.ContainsKey(row.TransactionId) && _categories.Count > 0)
            {
                Guid defaultCategory = _categories[0].Id;
                _selectedCategoryByTransaction[row.TransactionId] = defaultCategory;
                ApplyDefaultPotSelection(row.TransactionId, defaultCategory);
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

    private void SetCategory(Guid transactionId, Guid categoryId)
    {
        _selectedCategoryByTransaction[transactionId] = categoryId;
        ApplyDefaultPotSelection(transactionId, categoryId);
    }

    private void ApplyDefaultPotSelection(Guid transactionId, Guid categoryId)
    {
        if (!ShowPotUi)
        {
            _selectedPotByTransaction[transactionId] = null;
            return;
        }

        List<PotOption> potsForCategory = PotsForCategory(categoryId).ToList();
        _selectedPotByTransaction[transactionId] = _potMappingMode switch
        {
            PotMappingMode.ThreePots => potsForCategory.Count > 0 ? potsForCategory[0].Id : null,
            PotMappingMode.SeveralPots => potsForCategory.Count == 1 ? potsForCategory[0].Id : null,
            _ => null,
        };
    }

    private IEnumerable<PotOption> PotsForCategory(Guid categoryId) =>
        _pots.Where(p => p.CategoryId == categoryId);

    private async Task AssignAsync(Guid transactionId)
    {
        _statusMessage = null;
        if (!_selectedCategoryByTransaction.TryGetValue(transactionId, out Guid categoryId))
        {
            return;
        }

        _selectedPotByTransaction.TryGetValue(transactionId, out Guid? potId);

        try
        {
            await CategorisationService.CategoriseAsync(
                new OneOffCategorisationRequest(transactionId, categoryId, potId));
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
            TransactionKind.ExpenseDebit => "Expense debit",
            TransactionKind.IncomeCredit => "Income credit",
            TransactionKind.RefundCredit => "Refund",
            _ => kind.ToString(),
        };

    private sealed record CategoryOption(Guid Id, BudgetCategoryKind Kind);

    private sealed record PotOption(Guid Id, string Name, Guid CategoryId);
}
