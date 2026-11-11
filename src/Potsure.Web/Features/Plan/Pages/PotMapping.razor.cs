using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Web.Resources;

namespace Potsure.Web.Features.Plan.Pages;

public partial class PotMapping
{
    [Inject]
    private BankPotService PotService { get; set; } = null!;

    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private PotsureDbContext DbContext { get; set; } = null!;

    private bool _loading = true;
    private string? _loadError;
    private string? _modeError;
    private string? _potsError;
    private string? _addPotError;
    private string? _editPotError;
    private PotMappingMode _mode = PotMappingMode.CategoryOnly;

    private PotMappingMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value)
            {
                return;
            }

            PotMappingMode previous = _mode;
            _mode = value;
            _ = PersistModeAsync(previous);
        }
    }

    private List<BankPotReadModel> _pots = [];
    private List<CategoryOption> _categories = [];
    private string _newPotName = string.Empty;
    private Guid _newPotCategoryId;
    private Guid? _pendingDeletePotId;
    private decimal _pendingDeleteFunding;
    private bool _showFundingDispositionDialog;
    private Guid? _editingPotId;
    private string _editPotName = string.Empty;
    private Guid _editPotCategoryId;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task LoadAsync()
    {
        var profile = await DbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .SingleAsync();
        _mode = profile.PotMappingMode;
        _categories = BudgetCategoryDisplay
            .OrderByStrategy(profile.Categories, static c => c.Kind)
            .Select(c => new CategoryOption(c.Id, c.Kind))
            .ToList();
        EnsureCategorySelectionValid();

        _pots = (await PotService.ListAsync()).ToList();

        if (_editingPotId is Guid editId && _pots.All(p => p.Id != editId))
        {
            CancelEditPot();
        }
    }

    private void EnsureCategorySelectionValid()
    {
        List<CategoryOption> addOptions = CategoriesAvailableForNewPot();
        if (addOptions.Count == 0)
        {
            _newPotCategoryId = Guid.Empty;
        }
        else if (addOptions.All(c => c.Id != _newPotCategoryId))
        {
            _newPotCategoryId = addOptions[0].Id;
        }

        if (_editingPotId is Guid editId)
        {
            List<CategoryOption> editOptions = CategoriesAvailableForEdit(editId);
            if (editOptions.Count == 0)
            {
                _editPotCategoryId = Guid.Empty;
            }
            else if (editOptions.All(c => c.Id != _editPotCategoryId))
            {
                _editPotCategoryId = editOptions[0].Id;
            }
        }
    }

    private bool ShowAddPotSection =>
        _mode != PotMappingMode.CategoryOnly
        && !(_mode == PotMappingMode.ThreePots && _pots.Count >= 3);

    private List<CategoryOption> CategoriesAvailableForNewPot()
    {
        if (_mode != PotMappingMode.ThreePots)
        {
            return _categories;
        }

        HashSet<Guid> mappedCategoryIds = _pots.Select(p => p.CategoryId).ToHashSet();
        return _categories.Where(c => !mappedCategoryIds.Contains(c.Id)).ToList();
    }

    private List<CategoryOption> CategoriesAvailableForEdit(Guid potId)
    {
        if (_mode != PotMappingMode.ThreePots)
        {
            return _categories;
        }

        HashSet<Guid> mappedByOtherPots = _pots
            .Where(p => p.Id != potId)
            .Select(p => p.CategoryId)
            .ToHashSet();

        return _categories.Where(c => !mappedByOtherPots.Contains(c.Id)).ToList();
    }

    private async Task PersistModeAsync(PotMappingMode revertOnFailure)
    {
        try
        {
            _modeError = null;
            await PotService.SetPotMappingModeAsync(_mode);
        }
        catch (Exception ex)
        {
            _mode = revertOnFailure;
            _modeError = ex.Message;
        }
    }

    private void PromptDeletePot(BankPotReadModel pot)
    {
        if (pot.RecordedFundingAmount > 0m)
        {
            _pendingDeletePotId = pot.Id;
            _pendingDeleteFunding = pot.RecordedFundingAmount;
            _showFundingDispositionDialog = true;
            return;
        }

        _ = DeletePotAsync(pot.Id, BankPotFundingDisposition.NotApplicable);
    }

    private async Task ConfirmFundingDispositionAsync(BankPotFundingDisposition disposition)
    {
        if (_pendingDeletePotId is not Guid potId)
        {
            return;
        }

        _showFundingDispositionDialog = false;
        await DeletePotAsync(potId, disposition);
        _pendingDeletePotId = null;
        _pendingDeleteFunding = 0m;
    }

    private async Task DeletePotAsync(Guid potId, BankPotFundingDisposition disposition)
    {
        try
        {
            _potsError = null;
            await PotService.DeleteAsync(new DeleteBankPotRequest(potId, disposition));
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _potsError = ex.Message;
        }
    }

    private void BeginEditPot(BankPotReadModel pot)
    {
        _editingPotId = pot.Id;
        _editPotName = pot.Name;
        _editPotCategoryId = pot.CategoryId;
        _editPotError = null;
        _potsError = null;
    }

    private async Task SaveEditPotAsync()
    {
        if (_editingPotId is not Guid potId)
        {
            return;
        }

        BankPotReadModel? pot = _pots.FirstOrDefault(p => p.Id == potId);
        if (pot is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_editPotName))
        {
            _editPotError = "Enter a pot name.";
            return;
        }

        if (!TryValidateCategorySelection(_editPotCategoryId, out string? categoryError))
        {
            _editPotError = categoryError;
            return;
        }

        try
        {
            _editPotError = null;
            await PotService.UpdateAsync(
                new UpdateBankPotRequest(
                    potId,
                    _editPotName,
                    _editPotCategoryId,
                    pot.AllocationToPotAmount,
                    pot.PlannedFutureFundingAmount));
            _editingPotId = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _editPotError = ex.Message;
        }
    }

    private void CancelEditPot()
    {
        _editingPotId = null;
        _editPotError = null;
    }

    private async Task CreatePotAsync()
    {
        _addPotError = null;
        if (string.IsNullOrWhiteSpace(_newPotName))
        {
            _addPotError = "Enter a pot name.";
            return;
        }

        if (!TryValidateCategorySelection(_newPotCategoryId, out string? categoryError))
        {
            _addPotError = categoryError;
            return;
        }

        try
        {
            await PotService.CreateAsync(new CreateBankPotRequest(_newPotName, _newPotCategoryId));
            _newPotName = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _addPotError = ex.Message;
        }
    }

    private bool TryValidateCategorySelection(Guid categoryId, out string? error)
    {
        if (categoryId == Guid.Empty)
        {
            error = "Choose a category for this pot.";
            return false;
        }

        if (_categories.All(c => c.Id != categoryId))
        {
            error = "Choose a valid category for this pot.";
            return false;
        }

        if (_mode == PotMappingMode.ThreePots
            && _pots.Any(p => p.CategoryId == categoryId && p.Id != _editingPotId))
        {
            error = "Three pots mode allows at most one pot per category.";
            return false;
        }

        error = null;
        return true;
    }

    private static Dictionary<string, object> InlineAlertAttributes { get; } =
        new() { ["aria-live"] = "polite" };

    private string SelectedNewPotCategoryLabel =>
        CategoriesAvailableForNewPot().FirstOrDefault(c => c.Id == _newPotCategoryId) is { } selected
            ? BudgetCategoryDisplay.Format(selected.Kind)
            : "Select category";

    private sealed record CategoryOption(Guid Id, BudgetCategoryKind Kind);
}
