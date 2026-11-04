using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Data;
using Potsure.Data.Entities;
using Potsure.Infrastructure.Services;
using Potsure.Recurrence;

namespace Potsure.Web.Components.Recurrence;

public partial class DetectionReview
{
    [Inject]
    private RecurrenceReviewService ReviewService { get; set; } = null!;

    [Inject]
    private IncomePatternService IncomeService { get; set; } = null!;

    [Inject]
    private ExpensePatternService ExpenseService { get; set; } = null!;

    [Inject]
    private PatternMergeSplitService MergeSplitService { get; set; } = null!;

    [Inject]
    private TransactionAttributionService AttributionService { get; set; } = null!;

    [Inject]
    private BudgetProfileSeeder ProfileSeeder { get; set; } = null!;

    [Inject]
    private PotsureDbContext DbContext { get; set; } = null!;

    [Inject]
    private BankPotService PotService { get; set; } = null!;

    private bool _loading = true;
    private string? _error;
    private string? _statusMessage;
    private List<RecurrenceReviewItem> _suggested = [];
    private List<RecurrenceReviewItem> _confirmedOrRejected = [];
    private List<CategoryOption> _categories = [];
    private List<PotOption> _pots = [];
    private Dictionary<string, Guid> _expenseCategoryByLabel = [];
    private Dictionary<string, Guid?> _expensePotByLabel = [];
    private Dictionary<string, string?> _expenseTagByLabel = [];
    private Dictionary<string, IncomeSplitInput> _incomeSplitsByLabel = [];
    private Dictionary<string, IncomeCadence> _incomeCadenceByLabel = [];
    private HashSet<string> _mergeSelection = [];
    private string? _mergeLabel;
    private Guid _mergeCategoryId;
    private Guid? _mergePotId;
    private string? _mergeTag;
    private bool _mergeRequiresExplicitResolution;
    private string? _splitSourceLabel;
    private string _splitPartOneLabel = string.Empty;
    private string _splitPartTwoLabel = string.Empty;
    private Guid _splitPartOneCategoryId;
    private Guid? _splitPartOnePotId;
    private string? _splitPartOneTag;
    private Guid _splitPartTwoCategoryId;
    private Guid? _splitPartTwoPotId;
    private string? _splitPartTwoTag;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            await LoadCategoriesAsync();
            await LoadPotsAsync();
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

    private async Task LoadPotsAsync()
    {
        IReadOnlyList<BankPotReadModel> pots = await PotService.ListAsync();
        _pots = pots.Select(p => new PotOption(p.Id, p.Name, p.CategoryKind)).ToList();
    }

    private async Task ReloadAsync()
    {
        IReadOnlyList<RecurrenceReviewItem> items = await ReviewService.GetReviewItemsAsync();
        _suggested = items.Where(i => i.State == PatternConfirmationState.Suggested).ToList();
        _confirmedOrRejected = items.Where(i => i.State != PatternConfirmationState.Suggested).ToList();

        foreach (RecurrenceReviewItem item in _suggested.Concat(_confirmedOrRejected))
        {
            if (!item.IsIncome && !_expenseCategoryByLabel.ContainsKey(item.Label) && _categories.Count > 0)
            {
                _expenseCategoryByLabel[item.Label] = _categories[0].Id;
                _expensePotByLabel[item.Label] = null;
                _expenseTagByLabel[item.Label] = null;
            }

            if (item.IsIncome && !_incomeSplitsByLabel.ContainsKey(item.Label))
            {
                _incomeSplitsByLabel[item.Label] = new IncomeSplitInput();
            }

            if (item.IsIncome && !_incomeCadenceByLabel.ContainsKey(item.Label))
            {
                _incomeCadenceByLabel[item.Label] = item.IncomeCadence ?? IncomeCadence.Monthly;
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

        if (_categories.Count > 0)
        {
            _mergeCategoryId = _categories[0].Id;
            _splitPartOneCategoryId = _categories[0].Id;
            _splitPartTwoCategoryId = _categories[0].Id;
        }
    }

    private async Task ConfirmAsync(RecurrenceReviewItem item)
    {
        _statusMessage = null;
        try
        {
            if (item.IsIncome)
            {
                IncomeSplitInput splits = _incomeSplitsByLabel[item.Label];
                IncomeCadence cadence = _incomeCadenceByLabel[item.Label];
                decimal? fixedAmount = cadence is IncomeCadence.Variable or IncomeCadence.AdHoc ? null : item.TypicalAmount;
                decimal? estimateAmount = cadence is IncomeCadence.Variable or IncomeCadence.AdHoc ? item.TypicalAmount : null;
                await IncomeService.SaveAsync(
                    new SaveIncomeSourceRequest(
                        item.PersistedId,
                        item.SourceLabelFallback(),
                        cadence,
                        fixedAmount,
                        estimateAmount,
                        splits.Needs,
                        splits.Wants,
                        splits.SavingsDebt,
                        PatternConfirmationState.Confirmed));
            }
            else
            {
                Guid categoryId = _expenseCategoryByLabel[item.Label];
                Guid? potId = _expensePotByLabel[item.Label];
                string? tag = NullIfWhiteSpace(_expenseTagByLabel[item.Label]);
                ExpenseCadence cadence = item.ExpenseCadence ?? ExpenseCadence.Monthly;
                bool isVariable = cadence == ExpenseCadence.Variable;
                decimal? monthlyEstimate = null;
                string? coverageLabel = null;
                if (isVariable)
                {
                    VariableExpenseEstimateResult estimate = await BuildVariableEstimateAsync(item.Label);
                    monthlyEstimate = estimate.MonthlyEstimate;
                    coverageLabel = estimate.CoverageLabel;
                }

                RecurringExpenseEntity saved = await ExpenseService.SaveAsync(
                    new SaveRecurringExpenseRequest(
                        item.PersistedId,
                        item.Label,
                        categoryId,
                        potId,
                        cadence,
                        PaymentType: "Card",
                        AmountKind: isVariable ? "Variable" : "Fixed",
                        isVariable ? null : item.TypicalAmount,
                        monthlyEstimate,
                        coverageLabel,
                        tag,
                        PatternConfirmationState.Confirmed));

                await AttributionService.AttributeMatchingDebitsAsync(saved.Id);
            }

            _statusMessage = $"Confirmed {item.Label}.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
        }
    }

    private async Task RejectAsync(RecurrenceReviewItem item)
    {
        _statusMessage = null;
        try
        {
            if (item.IsIncome)
            {
                await IncomeService.SaveAsync(
                    new SaveIncomeSourceRequest(
                        item.PersistedId,
                        item.SourceLabelFallback(),
                        item.IncomeCadence ?? IncomeCadence.Monthly,
                        item.TypicalAmount,
                        null,
                        50m,
                        30m,
                        20m,
                        PatternConfirmationState.Rejected));
            }
            else
            {
                Guid categoryId = _expenseCategoryByLabel[item.Label];
                await ExpenseService.SaveAsync(
                    new SaveRecurringExpenseRequest(
                        item.PersistedId,
                        item.Label,
                        categoryId,
                        null,
                        item.ExpenseCadence ?? ExpenseCadence.Monthly,
                        "Card",
                        "Fixed",
                        item.TypicalAmount,
                        null,
                        null,
                        null,
                        PatternConfirmationState.Rejected));
            }

            _statusMessage = $"Rejected {item.Label}.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
        }
    }

    private void ToggleMergeSelection(string label)
    {
        if (!_mergeSelection.Remove(label))
        {
            _mergeSelection.Add(label);
        }

        UpdateMergeConflictState();
    }

    private void UpdateMergeConflictState()
    {
        List<RecurringExpenseEntity> selected = GetSelectedMergeSources();
        _mergeRequiresExplicitResolution = PatternMergeSplitService.SourcesHaveAssignmentConflict(selected);
    }

    private List<RecurringExpenseEntity> GetSelectedMergeSources()
    {
        List<Guid> ids = _confirmedOrRejected
            .Where(i => !i.IsIncome && _mergeSelection.Contains(i.Label) && i.PersistedId is not null)
            .Select(i => i.PersistedId!.Value)
            .ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        return DbContext.RecurringExpenses.AsNoTracking().Where(e => ids.Contains(e.Id)).ToList();
    }

    private async Task MergeSelectedAsync()
    {
        _statusMessage = null;
        if (_mergeSelection.Count < 2)
        {
            _statusMessage = "Select at least two expense patterns to merge.";
            return;
        }

        List<RecurrenceReviewItem> selected = _confirmedOrRejected
            .Where(i => !i.IsIncome && _mergeSelection.Contains(i.Label) && i.PersistedId is not null)
            .ToList();

        if (selected.Count < 2)
        {
            _statusMessage = "Merge requires two or more confirmed expense patterns with saved IDs.";
            return;
        }

        string mergedLabel = string.IsNullOrWhiteSpace(_mergeLabel)
            ? selected[0].Label
            : _mergeLabel.Trim();

        try
        {
            await MergeSplitService.MergeAsync(
                new MergeRecurringExpensesRequest(
                    selected.Select(s => s.PersistedId!.Value).ToList(),
                    mergedLabel,
                    new PatternResolution(_mergeCategoryId, _mergePotId, NullIfWhiteSpace(_mergeTag)),
                    selected[0].ExpenseCadence ?? ExpenseCadence.Monthly,
                    "Fixed",
                    selected[0].TypicalAmount,
                    null,
                    null,
                    UserResolvedAssignments: true));

            _mergeSelection.Clear();
            _statusMessage = $"Merged patterns into {mergedLabel}.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
        }
    }

    private async Task SplitPatternAsync()
    {
        _statusMessage = null;
        if (string.IsNullOrWhiteSpace(_splitSourceLabel))
        {
            _statusMessage = "Choose a confirmed expense pattern to split.";
            return;
        }

        RecurrenceReviewItem? source = _confirmedOrRejected
            .FirstOrDefault(i => !i.IsIncome && i.Label == _splitSourceLabel && i.PersistedId is not null);

        if (source is null)
        {
            _statusMessage = "Selected pattern could not be found.";
            return;
        }

        try
        {
            await MergeSplitService.SplitAsync(
                new SplitRecurringExpenseRequest(
                    source.PersistedId!.Value,
                    [
                        new SplitExpensePart(
                            _splitPartOneLabel.Trim(),
                            new PatternResolution(
                                _splitPartOneCategoryId,
                                _splitPartOnePotId,
                                NullIfWhiteSpace(_splitPartOneTag)),
                            source.ExpenseCadence ?? ExpenseCadence.Monthly,
                            "Fixed",
                            source.TypicalAmount / 2m,
                            null,
                            null),
                        new SplitExpensePart(
                            _splitPartTwoLabel.Trim(),
                            new PatternResolution(
                                _splitPartTwoCategoryId,
                                _splitPartTwoPotId,
                                NullIfWhiteSpace(_splitPartTwoTag)),
                            source.ExpenseCadence ?? ExpenseCadence.Monthly,
                            "Fixed",
                            source.TypicalAmount / 2m,
                            null,
                            null),
                    ]));

            _statusMessage = $"Split {source.Label} into two patterns.";
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            _statusMessage = ex.Message;
        }
    }

    private static string FormatCadence(RecurrenceReviewItem item) =>
        item.IsIncome
            ? item.IncomeCadence?.ToString() ?? "Unknown"
            : item.ExpenseCadence?.ToString() ?? "Unknown";

    private static string FormatCategoryKind(BudgetCategoryKind kind) =>
        kind switch
        {
            BudgetCategoryKind.Needs => "Needs",
            BudgetCategoryKind.Wants => "Wants",
            BudgetCategoryKind.SavingsAndDebt => "Savings and debt",
            _ => kind.ToString(),
        };

    private sealed record CategoryOption(Guid Id, BudgetCategoryKind Kind);

    private sealed record PotOption(Guid Id, string Name, BudgetCategoryKind CategoryKind);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private IEnumerable<PotOption> PotsForCategory(Guid categoryId) =>
        _pots.Where(p => _categories.FirstOrDefault(c => c.Id == categoryId)?.Kind == p.CategoryKind);

    private void SetMergeIncluded(string label, bool included)
    {
        if (included)
        {
            _mergeSelection.Add(label);
        }
        else
        {
            _mergeSelection.Remove(label);
        }
    }

    private Guid GetExpensePotSelection(string label) =>
        _expensePotByLabel.TryGetValue(label, out Guid? pot) && pot is Guid id ? id : Guid.Empty;

    private void SetExpensePotSelection(string label, Guid value) =>
        _expensePotByLabel[label] = value == Guid.Empty ? null : value;

    private Guid MergePotSelection
    {
        get => _mergePotId ?? Guid.Empty;
        set => _mergePotId = value == Guid.Empty ? null : value;
    }

    private string SplitSourceSelection
    {
        get => _splitSourceLabel ?? string.Empty;
        set => _splitSourceLabel = string.IsNullOrEmpty(value) ? null : value;
    }

    private Guid SplitPartOnePotSelection
    {
        get => _splitPartOnePotId ?? Guid.Empty;
        set => _splitPartOnePotId = value == Guid.Empty ? null : value;
    }

    private Guid SplitPartTwoPotSelection
    {
        get => _splitPartTwoPotId ?? Guid.Empty;
        set => _splitPartTwoPotId = value == Guid.Empty ? null : value;
    }

    private static string FormatDecimal(decimal value) =>
        value.ToString("G29", CultureInfo.InvariantCulture);

    private static decimal ParseDecimal(string? value, decimal fallback) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed)
            ? parsed
            : fallback;

    private sealed class IncomeSplitInput
    {
        public decimal Needs { get; set; } = 50m;
        public decimal Wants { get; set; } = 30m;
        public decimal SavingsDebt { get; set; } = 20m;
    }

    private async Task<VariableExpenseEstimateResult> BuildVariableEstimateAsync(string vendorLabel)
    {
        string normalised = PatternLabelNormalizer.Normalize(vendorLabel);
        List<TransactionEntity> transactions = await DbContext.Transactions.AsNoTracking().ToListAsync();
        List<(DateOnly Date, decimal Amount)> debits = transactions
            .Where(t => t.Kind == TransactionKind.ExpenseDebit)
            .Where(t => string.Equals(PatternLabelNormalizer.Normalize(t.Name), normalised, StringComparison.Ordinal))
            .Select(t => (t.TransactionDate, t.Amount))
            .ToList();

        List<DateOnly> coverageDays = transactions.Select(t => t.TransactionDate).Distinct().OrderBy(d => d).ToList();
        DateOnly asOf = coverageDays.Count > 0 ? coverageDays[^1] : DateOnly.FromDateTime(DateTime.UtcNow);
        return VariableExpenseEstimator.Estimate(debits, coverageDays, asOf);
    }
}

internal static class RecurrenceReviewItemExtensions
{
    internal static string SourceLabelFallback(this RecurrenceReviewItem item) => item.Label;
}
