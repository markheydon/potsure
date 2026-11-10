using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Infrastructure.Features.Plan;
using Potsure.Infrastructure.Features.Recurrence;
using Potsure.Infrastructure.Features.Reports;
using Potsure.Recurrence;
using Potsure.Web.Resources;

namespace Potsure.Web.Features.Recurrence.Pages;

public partial class RecurringSummary
{
    [Inject]
    private RecurringSummaryQuery SummaryQuery { get; set; } = null!;

    [Inject]
    private IncomePatternService IncomeService { get; set; } = null!;

    [Inject]
    private ExpensePatternService ExpenseService { get; set; } = null!;

    [Inject]
    private TransactionAttributionService AttributionService { get; set; } = null!;

    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private BankPotService PotService { get; set; } = null!;

    [Inject]
    private PotsureDbContext DbContext { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private string? _statusMessage;
    private RecurringSummaryReadModel? _model;
    private List<CategoryOption> _categories = [];
    private List<PotOption> _pots = [];
    private PotMappingMode _potMappingMode = PotMappingMode.CategoryOnly;
    private Guid? _editingIncomeId;
    private Guid? _editingExpenseId;
    private IncomeEditDraft _incomeEdit = new();
    private ExpenseEditDraft _expenseEdit = new();
    private VariableExpenseEstimateResult? _pendingVariableSuggestion;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            await LoadCategoriesAsync();
            await LoadPotMappingModeAsync();
            await LoadPotsAsync();
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
            _loading = false;
        }
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
        _pots = pots.Select(p => new PotOption(p.Id, p.Name, p.CategoryId, p.CategoryKind)).ToList();
    }

    private bool ShowPotUi => _potMappingMode != PotMappingMode.CategoryOnly;

    private async Task LoadCategoriesAsync()
    {
        _categories = BudgetCategoryDisplay
            .OrderByStrategy(
                await DbContext.BudgetCategories
                    .AsNoTracking()
                    .Select(c => new CategoryOption(c.Id, c.Kind))
                    .ToListAsync(),
                static c => c.Kind)
            .ToList();
    }

    private async Task DeleteIncomeAsync(Guid incomeId)
    {
        _statusMessage = null;
        try
        {
            await IncomeService.DeleteConfirmedAsync(incomeId);
            _editingIncomeId = null;
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
            _editingExpenseId = null;
            _statusMessage = "Expense pattern deleted.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
        }
    }

    private void BeginEditIncome(ConfirmedIncomeSummaryRow row)
    {
        _editingExpenseId = null;
        _editingIncomeId = row.Id;
        _incomeEdit = new IncomeEditDraft
        {
            SourceLabel = row.SourceLabel,
            Cadence = row.Cadence,
            Amount = row.Cadence is IncomeCadence.Variable or IncomeCadence.AdHoc
                ? row.EstimateAmount ?? 0m
                : row.FixedAmount ?? row.EstimateAmount ?? 0m,
            Needs = row.SplitNeedsPercent,
            Wants = row.SplitWantsPercent,
            SavingsDebt = row.SplitSavingsDebtPercent,
        };
    }

    private async Task BeginEditExpenseAsync(ConfirmedExpenseSummaryRow row)
    {
        _editingIncomeId = null;
        _editingExpenseId = row.Id;
        RecurringExpenseEntity entity = await DbContext.RecurringExpenses.AsNoTracking().SingleAsync(e => e.Id == row.Id);
        _expenseEdit = new ExpenseEditDraft
        {
            VendorLabel = row.VendorLabel,
            CategoryId = entity.CategoryId,
            MappedPotId = entity.MappedPotId,
            Tag = entity.Tag,
            Cadence = entity.Cadence,
            AmountKind = entity.AmountKind,
            Amount = entity.AmountKind == ExpenseAmountKind.Variable
                ? entity.StoredMonthlyEstimate ?? 0m
                : entity.FixedAmount ?? entity.StoredMonthlyEstimate ?? 0m,
            CoverageLabel = entity.EstimateCoverageLabel,
            PaymentAnchor = entity.PaymentAnchorDate,
            PaymentAnchorUserOverride = entity.PaymentAnchorUserOverride,
        };

        _pendingVariableSuggestion = entity.AmountKind == ExpenseAmountKind.Variable
            ? await ExpenseService.EstimateVariableMonthlyAsync(row.VendorLabel)
            : null;
    }

    private void AcceptPendingVariableSuggestion()
    {
        if (_pendingVariableSuggestion is not VariableExpenseEstimateResult suggestion)
        {
            return;
        }

        _expenseEdit.Amount = suggestion.MonthlyEstimate;
        _expenseEdit.CoverageLabel = suggestion.CoverageLabel;
    }

    private void CancelEdit()
    {
        _editingIncomeId = null;
        _editingExpenseId = null;
        _pendingVariableSuggestion = null;
    }

    private async Task SaveIncomeEditAsync()
    {
        if (_editingIncomeId is not Guid id)
        {
            return;
        }

        _statusMessage = null;
        try
        {
            if (_incomeEdit.Amount <= 0m)
            {
                _statusMessage = "Enter a positive amount before saving.";
                return;
            }

            decimal? fixedAmount = _incomeEdit.Cadence is IncomeCadence.Variable or IncomeCadence.AdHoc
                ? null
                : _incomeEdit.Amount;
            decimal? estimateAmount = _incomeEdit.Cadence is IncomeCadence.Variable or IncomeCadence.AdHoc
                ? _incomeEdit.Amount
                : null;

            await IncomeService.SaveAsync(
                new SaveIncomeSourceRequest(
                    id,
                    _incomeEdit.SourceLabel.Trim(),
                    _incomeEdit.Cadence,
                    fixedAmount,
                    estimateAmount,
                    _incomeEdit.Needs,
                    _incomeEdit.Wants,
                    _incomeEdit.SavingsDebt,
                    PatternConfirmationState.Confirmed));

            _editingIncomeId = null;
            _statusMessage = "Income pattern saved.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
        }
    }

    private async Task SaveExpenseEditAsync()
    {
        if (_editingExpenseId is not Guid id)
        {
            return;
        }

        _statusMessage = null;
        try
        {
            bool variable = _expenseEdit.AmountKind == ExpenseAmountKind.Variable;
            if (_expenseEdit.Amount <= 0m)
            {
                _statusMessage = "Enter a positive amount before saving.";
                return;
            }

            decimal? fixedAmount = variable ? null : _expenseEdit.Amount;
            decimal? monthlyEstimate = variable ? _expenseEdit.Amount : null;
            DateOnly? anchor = variable ? null : _expenseEdit.PaymentAnchor;
            bool anchorOverride = !variable && _expenseEdit.PaymentAnchorUserOverride;

            RecurringExpenseEntity saved = await ExpenseService.SaveAsync(
                new SaveRecurringExpenseRequest(
                    id,
                    _expenseEdit.VendorLabel.Trim(),
                    _expenseEdit.CategoryId,
                    _expenseEdit.MappedPotId,
                    _expenseEdit.Cadence,
                    PaymentType: "Card",
                    _expenseEdit.AmountKind,
                    fixedAmount,
                    monthlyEstimate,
                    _expenseEdit.CoverageLabel,
                    NullIfWhiteSpace(_expenseEdit.Tag),
                    anchor,
                    anchorOverride,
                    PatternConfirmationState.Confirmed));

            await AttributionService.AttributeMatchingDebitsAsync(saved.Id);
            _editingExpenseId = null;
            _pendingVariableSuggestion = null;
            _statusMessage = "Expense pattern saved.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
        }
    }

    private void SetExpenseCategory(Guid categoryId)
    {
        _expenseEdit.CategoryId = categoryId;
        if (!ShowPotUi)
        {
            _expenseEdit.MappedPotId = null;
            return;
        }

        List<PotOption> potsForCategory = PotsForCategory(categoryId).ToList();
        _expenseEdit.MappedPotId = _potMappingMode switch
        {
            PotMappingMode.ThreePots => potsForCategory.Count > 0 ? potsForCategory[0].Id : null,
            PotMappingMode.SeveralPots => potsForCategory.Count == 1 ? potsForCategory[0].Id : null,
            _ => null,
        };
    }

    private void SetExpensePaymentAnchor(DateTime? value)
    {
        if (value is not DateTime picked)
        {
            return;
        }

        _expenseEdit.PaymentAnchor = DateOnly.FromDateTime(picked);
        _expenseEdit.PaymentAnchorUserOverride = true;
    }

    private Guid ExpensePotSelection
    {
        get => _expenseEdit.MappedPotId ?? Guid.Empty;
        set => _expenseEdit.MappedPotId = value == Guid.Empty ? null : value;
    }

    private IEnumerable<PotOption> PotsForCategory(Guid categoryId) =>
        _pots.Where(p => p.CategoryId == categoryId);

    private string CategoryLabel(Guid categoryId) =>
        _categories.FirstOrDefault(c => c.Id == categoryId) is { } category
            ? BudgetCategoryDisplay.Format(category.Kind)
            : "Category";

    private string PotSelectionLabel(Guid selection) =>
        selection == Guid.Empty
            ? "None"
            : _pots.FirstOrDefault(p => p.Id == selection)?.Name ?? "None";

    private static string IncomeCadenceLabel(IncomeCadence cadence) =>
        cadence switch
        {
            IncomeCadence.AdHoc => "Ad hoc",
            _ => cadence.ToString(),
        };

    private static string ExpenseAmountKindLabel(ExpenseAmountKind kind) =>
        kind == ExpenseAmountKind.Variable ? "Variable (monthly estimate)" : "Fixed per payment";

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatDecimal(decimal value) =>
        value.ToString("G29", CultureInfo.InvariantCulture);

    private static decimal ParseDecimal(string? value, decimal fallback) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed)
            ? parsed
            : fallback;

    private sealed record CategoryOption(Guid Id, BudgetCategoryKind Kind);

    private sealed record PotOption(Guid Id, string Name, Guid CategoryId, BudgetCategoryKind CategoryKind);

    private sealed class IncomeEditDraft
    {
        public string SourceLabel { get; set; } = string.Empty;
        public IncomeCadence Cadence { get; set; }
        public decimal Amount { get; set; }
        public decimal Needs { get; set; } = 50m;
        public decimal Wants { get; set; } = 30m;
        public decimal SavingsDebt { get; set; } = 20m;
    }

    private sealed class ExpenseEditDraft
    {
        public string VendorLabel { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public Guid? MappedPotId { get; set; }
        public string? Tag { get; set; }
        public ExpenseCadence Cadence { get; set; } = ExpenseCadence.Monthly;
        public ExpenseAmountKind AmountKind { get; set; } = ExpenseAmountKind.Fixed;
        public decimal Amount { get; set; }
        public string? CoverageLabel { get; set; }
        public DateOnly? PaymentAnchor { get; set; }
        public bool PaymentAnchorUserOverride { get; set; }
    }
}
