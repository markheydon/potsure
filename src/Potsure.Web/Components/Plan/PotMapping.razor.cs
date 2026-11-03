using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Infrastructure.Services;

namespace Potsure.Web.Components.Plan;

public partial class PotMapping
{
    [Inject]
    private BankPotService PotService { get; set; } = null!;

    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private PotsureDbContext DbContext { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private PotMappingMode _mode = PotMappingMode.None;

    private PotMappingMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            _ = PersistModeAsync();
        }
    }
    private List<BankPotReadModel> _pots = [];
    private List<CategoryOption> _categories = [];
    private string _newPotName = string.Empty;
    private Guid _newPotCategoryId;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
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
        var profile = await DbContext.BudgetProfiles
            .AsNoTracking()
            .Include(p => p.Categories)
            .SingleAsync();
        _mode = profile.PotMappingMode;
        _categories = profile.Categories
            .Select(c => new CategoryOption(c.Id, c.Kind))
            .ToList();
        if (_categories.Count > 0 && _newPotCategoryId == Guid.Empty)
        {
            _newPotCategoryId = _categories[0].Id;
        }

        _pots = (await PotService.ListAsync()).ToList();
    }

    private async Task PersistModeAsync()
    {
        try
        {
            await PotService.SetPotMappingModeAsync(_mode);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    private async Task CreatePotAsync()
    {
        if (string.IsNullOrWhiteSpace(_newPotName))
        {
            _error = "Enter a pot name.";
            return;
        }

        try
        {
            await PotService.CreateAsync(new CreateBankPotRequest(_newPotName, _newPotCategoryId));
            _newPotName = string.Empty;
            _error = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    private static string FormatMode(PotMappingMode mode) =>
        mode switch
        {
            PotMappingMode.None => "No pots (category-only)",
            PotMappingMode.CategoryNamedPots => "Category-named pots",
            PotMappingMode.ExpenseSpecificPots => "Expense-specific pots",
            _ => mode.ToString(),
        };

    private sealed record CategoryOption(Guid Id, BudgetCategoryKind Kind);
}
