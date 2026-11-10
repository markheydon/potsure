using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Web.Resources;
using Potsure.Web.Services;

namespace Potsure.Web.Features.Reports.Pages;

public partial class TransactionAttributionReview : IDisposable
{
    [Inject]
    private AttributedTransactionQuery AttributionQuery { get; set; } = null!;

    [Inject]
    private TransactionAttributionService AttributionService { get; set; } = null!;

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
    private List<AttributedTransactionRow> _transactions = [];
    private List<CategoryOption> _categories = [];
    private List<PotOption> _pots = [];
    private List<PatternOption> _patterns = [];
    private PotMappingMode _potMappingMode = PotMappingMode.CategoryOnly;
    private Dictionary<Guid, Guid> _categoryByTransaction = [];
    private Dictionary<Guid, Guid?> _potByTransaction = [];
    private Dictionary<Guid, Guid?> _patternByTransaction = [];
    private Dictionary<Guid, string?> _tagByTransaction = [];

    protected override async Task OnInitializedAsync()
    {
        DateRangeState.Changed += OnDateRangeChanged;
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            await LoadPotMappingModeAsync();
            await LoadLookupsAsync();
            await ReloadAsync();
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
            await ReloadAsync();
            StateHasChanged();
        });

    private async Task LoadPotMappingModeAsync()
    {
        _potMappingMode = await DbContext.BudgetProfiles
            .AsNoTracking()
            .Select(p => p.PotMappingMode)
            .SingleAsync();
    }

    private bool ShowPotUi => _potMappingMode != PotMappingMode.CategoryOnly;

    private async Task LoadLookupsAsync()
    {
        _categories = await DbContext.BudgetCategories
            .AsNoTracking()
            .OrderBy(c => c.Kind)
            .Select(c => new CategoryOption(c.Id, c.Kind))
            .ToListAsync();

        IReadOnlyList<BankPotReadModel> pots = await PotService.ListAsync();
        _pots = pots.Select(p => new PotOption(p.Id, p.Name, p.CategoryKind)).ToList();

        _patterns = await DbContext.RecurringExpenses
            .AsNoTracking()
            .Where(e => e.State == PatternConfirmationState.Confirmed)
            .OrderBy(e => e.VendorLabel)
            .Select(e => new PatternOption(e.Id, e.VendorLabel))
            .ToListAsync();
    }

    private async Task ReloadAsync()
    {
        (DateOnly start, DateOnly end) = DateRangeState.GetRange();
        _transactions = (await AttributionQuery.ListForReportRangeAsync(start, end)).ToList();
        foreach (AttributedTransactionRow row in _transactions)
        {
            if (!_categoryByTransaction.ContainsKey(row.TransactionId))
            {
                _categoryByTransaction[row.TransactionId] = row.CategoryId;
                _potByTransaction[row.TransactionId] = row.MappedPotId;
                _patternByTransaction[row.TransactionId] = row.RecurringExpenseId;
                _tagByTransaction[row.TransactionId] = row.Tag;
            }
        }
    }

    private async Task SaveOverrideAsync(Guid transactionId)
    {
        _statusMessage = null;
        if (!_categoryByTransaction.TryGetValue(transactionId, out Guid categoryId))
        {
            return;
        }

        try
        {
            await AttributionService.OverrideAttributionAsync(
                transactionId,
                categoryId,
                _potByTransaction[transactionId],
                _patternByTransaction[transactionId],
                NullIfWhiteSpace(_tagByTransaction[transactionId]));
            _statusMessage = "Override saved.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
        }
    }

    private void SetCategory(Guid transactionId, Guid categoryId)
    {
        _categoryByTransaction[transactionId] = categoryId;
        ApplyDefaultPotSelection(transactionId, categoryId);
    }

    private void ApplyDefaultPotSelection(Guid transactionId, Guid categoryId)
    {
        if (!ShowPotUi)
        {
            _potByTransaction[transactionId] = null;
            return;
        }

        List<PotOption> potsForCategory = PotsForCategory(categoryId).ToList();
        _potByTransaction[transactionId] = _potMappingMode switch
        {
            PotMappingMode.ThreePots => potsForCategory.Count > 0 ? potsForCategory[0].Id : null,
            PotMappingMode.SeveralPots => potsForCategory.Count == 1 ? potsForCategory[0].Id : null,
            _ => null,
        };
    }

    private IEnumerable<PotOption> PotsForCategory(Guid categoryId) =>
        _pots.Where(p => _categories.FirstOrDefault(c => c.Id == categoryId)?.Kind == p.CategoryKind);

    private string CategoryLabel(Guid categoryId) =>
        _categories.FirstOrDefault(c => c.Id == categoryId) is { } category
            ? BudgetCategoryDisplay.Format(category.Kind)
            : "Category";

    private string PotSelectionLabel(Guid transactionId)
    {
        if (!_potByTransaction.TryGetValue(transactionId, out Guid? pot) || pot is not Guid id || id == Guid.Empty)
        {
            return "None";
        }

        return _pots.FirstOrDefault(p => p.Id == id)?.Name ?? "None";
    }

    private string PatternSelectionLabel(Guid transactionId)
    {
        if (!_patternByTransaction.TryGetValue(transactionId, out Guid? pattern)
            || pattern is not Guid id
            || id == Guid.Empty)
        {
            return "None";
        }

        return _patterns.FirstOrDefault(p => p.Id == id)?.Label ?? "None";
    }

    private Guid GetPotSelection(Guid transactionId) =>
        _potByTransaction.TryGetValue(transactionId, out Guid? pot) && pot is Guid id ? id : Guid.Empty;

    private void SetPotSelection(Guid transactionId, Guid value) =>
        _potByTransaction[transactionId] = value == Guid.Empty ? null : value;

    private Guid GetPatternSelection(Guid transactionId) =>
        _patternByTransaction.TryGetValue(transactionId, out Guid? pattern) && pattern is Guid id ? id : Guid.Empty;

    private void SetPatternSelection(Guid transactionId, Guid value) =>
        _patternByTransaction[transactionId] = value == Guid.Empty ? null : value;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record CategoryOption(Guid Id, BudgetCategoryKind Kind);

    private sealed record PotOption(Guid Id, string Name, BudgetCategoryKind CategoryKind);

    private sealed record PatternOption(Guid Id, string Label);

}
