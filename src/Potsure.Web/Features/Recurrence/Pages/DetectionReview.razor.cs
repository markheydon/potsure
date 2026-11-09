using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
    private Dictionary<string, decimal> _incomeAmountByLabel = [];
    private Dictionary<string, decimal> _expenseAmountByLabel = [];
    private HashSet<string> _mergeSelection = [];
    private string? _mergeLabel;
    private Guid _mergeCategoryId;
    private Guid? _mergePotId;
    private string? _mergeTag;
    private bool _mergeRequiresExplicitResolution;
    private string? _splitSourceLabel;

    /// <summary>
    /// Confirmed expense row selected in Split expense pattern (if any).
    /// </summary>
    private RecurrenceReviewItem? SplitSourceReviewItem =>
        string.IsNullOrEmpty(_splitSourceLabel)
            ? null
            : _confirmedOrRejected.FirstOrDefault(i =>
                !i.IsIncome
                && i.State == PatternConfirmationState.Confirmed
                && i.Label == _splitSourceLabel);
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

            if (item.IsIncome)
            {
                _incomeAmountByLabel[item.Label] = item.TypicalAmount ?? 0m;
            }
            else
            {
                _expenseAmountByLabel[item.Label] = item.TypicalAmount ?? 0m;
            }
        }
    }

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
                decimal amount = _incomeAmountByLabel.GetValueOrDefault(item.Label);
                if (amount <= 0m)
                {
                    _statusMessage = "Enter a positive amount for this income line before confirming.";
                    return;
                }

                decimal? fixedAmount = cadence is IncomeCadence.Variable or IncomeCadence.AdHoc ? null : amount;
                decimal? estimateAmount = cadence is IncomeCadence.Variable or IncomeCadence.AdHoc ? amount : null;

                await IncomeService.SaveAsync(
                    new SaveIncomeSourceRequest(
                        item.PersistedId,
                        item.SourceOrVendorLabel(),
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
                bool treatAsVariable = item.ExpenseIsVariableAmount();
                VariableExpenseEstimateResult estimate = await BuildVariableEstimateAsync(item.Label);
                decimal entered = _expenseAmountByLabel.GetValueOrDefault(item.Label);
                decimal? monthlyEstimate = null;
                string? coverageLabel = estimate.CoverageLabel;
                decimal? fixedAmount = null;

                if (treatAsVariable)
                {
                    monthlyEstimate = entered > 0m ? entered : estimate.MonthlyEstimate;
                    if (monthlyEstimate is null or <= 0m)
                    {
                        _statusMessage = "Enter a positive monthly estimate for this variable expense before confirming.";
                        return;
                    }
                }
                else
                {
                    fixedAmount = entered > 0m ? entered : item.TypicalAmount;
                    if (fixedAmount is null or <= 0m)
                    {
                        _statusMessage = "Enter a positive fixed amount per payment before confirming.";
                        return;
                    }
                }

                RecurringExpenseEntity saved = await ExpenseService.SaveAsync(
                    new SaveRecurringExpenseRequest(
                        item.PersistedId,
                        item.SourceOrVendorLabel(),
                        categoryId,
                        potId,
                        cadence,
                        PaymentType: "Card",
                        treatAsVariable ? ExpenseAmountKind.Variable : ExpenseAmountKind.Fixed,
                        fixedAmount,
                        monthlyEstimate,
                        coverageLabel,
                        tag,
                        PaymentAnchorDate: null,
                        PaymentAnchorUserOverride: false,
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
                IncomeCadence cadence = _incomeCadenceByLabel.GetValueOrDefault(item.Label, item.IncomeCadence ?? IncomeCadence.Monthly);
                decimal amount = _incomeAmountByLabel.GetValueOrDefault(item.Label);
                decimal? fixedAmount = cadence is IncomeCadence.Variable or IncomeCadence.AdHoc ? null : amount;
                decimal? estimateAmount = cadence is IncomeCadence.Variable or IncomeCadence.AdHoc ? amount : null;
                await IncomeService.SaveAsync(
                    new SaveIncomeSourceRequest(
                        item.PersistedId,
                        item.SourceOrVendorLabel(),
                        cadence,
                        fixedAmount,
                        estimateAmount,
                        50m,
                        30m,
                        20m,
                        PatternConfirmationState.Rejected));
            }
            else
            {
                Guid categoryId = _expenseCategoryByLabel[item.Label];
                bool variable = item.ExpenseIsVariableAmount();
                decimal amount = _expenseAmountByLabel.GetValueOrDefault(item.Label);
                decimal? fixedPart = variable ? null : (item.TypicalAmount ?? amount);
                decimal? monthlyPart = variable ? (item.TypicalAmount ?? amount) : null;
                string? coverage = null;
                if (variable && (monthlyPart is null or <= 0m))
                {
                    VariableExpenseEstimateResult estimate = await BuildVariableEstimateAsync(item.Label);
                    monthlyPart = estimate.MonthlyEstimate;
                    coverage = estimate.CoverageLabel;
                }

                await ExpenseService.SaveAsync(
                    new SaveRecurringExpenseRequest(
                        item.PersistedId,
                        item.SourceOrVendorLabel(),
                        categoryId,
                        null,
                        item.ExpenseCadence ?? ExpenseCadence.Monthly,
                        "Card",
                        variable ? ExpenseAmountKind.Variable : ExpenseAmountKind.Fixed,
                        fixedPart,
                        monthlyPart,
                        coverage,
                        null,
                        PaymentAnchorDate: null,
                        PaymentAnchorUserOverride: false,
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
            List<Guid> ids = selected.Select(s => s.PersistedId!.Value).ToList();
            List<RecurringExpenseEntity> entities = await DbContext.RecurringExpenses
                .AsNoTracking()
                .Where(e => ids.Contains(e.Id))
                .ToListAsync();

            bool anyVariable = entities.Any(e => e.AmountKind == ExpenseAmountKind.Variable);
            decimal? mergedFixed = anyVariable
                ? null
                : entities.Sum(e => e.FixedAmount ?? 0m);
            decimal? mergedMonthly = anyVariable
                ? entities.Sum(e => e.StoredMonthlyEstimate ?? e.FixedAmount ?? 0m)
                : null;

            await MergeSplitService.MergeAsync(
                new MergeRecurringExpensesRequest(
                    ids,
                    mergedLabel,
                    new PatternResolution(_mergeCategoryId, _mergePotId, NullIfWhiteSpace(_mergeTag)),
                    selected[0].ExpenseCadence ?? ExpenseCadence.Monthly,
                    anyVariable ? "Variable" : "Fixed",
                    mergedFixed,
                    mergedMonthly,
                    anyVariable ? "Merged from selected patterns." : null,
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
            RecurringExpenseEntity? sourceEntity = await DbContext.RecurringExpenses
                .AsNoTracking()
                .SingleOrDefaultAsync(e => e.Id == source.PersistedId!.Value);

            bool variable = sourceEntity is not null
                            && sourceEntity.AmountKind == ExpenseAmountKind.Variable;
            decimal? halfFixed = variable
                ? null
                : (sourceEntity?.FixedAmount ?? source.TypicalAmount) is decimal f
                    ? f / 2m
                    : null;
            decimal? halfMonthly = variable
                ? (sourceEntity?.StoredMonthlyEstimate ?? source.TypicalAmount) is decimal m
                    ? m / 2m
                    : null
                : null;

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
                            variable ? "Variable" : "Fixed",
                            halfFixed,
                            halfMonthly,
                            variable ? "Split from variable pattern." : null),
                        new SplitExpensePart(
                            _splitPartTwoLabel.Trim(),
                            new PatternResolution(
                                _splitPartTwoCategoryId,
                                _splitPartTwoPotId,
                                NullIfWhiteSpace(_splitPartTwoTag)),
                            source.ExpenseCadence ?? ExpenseCadence.Monthly,
                            variable ? "Variable" : "Fixed",
                            halfFixed,
                            halfMonthly,
                            variable ? "Split from variable pattern." : null),
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

    private sealed record CategoryOption(Guid Id, BudgetCategoryKind Kind);

    private sealed record PotOption(Guid Id, string Name, BudgetCategoryKind CategoryKind);

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private IEnumerable<PotOption> PotsForCategory(Guid categoryId) =>
        _pots.Where(p => _categories.FirstOrDefault(c => c.Id == categoryId)?.Kind == p.CategoryKind);

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

    /// <summary>
    /// Stable, HTML-safe id for the merge checkbox (label text can include punctuation; avoid raw labels in ids).
    /// </summary>
    private static string MergeCheckboxDomId(RecurrenceReviewItem item)
    {
        if (item.PersistedId is Guid id)
        {
            return $"merge-confirmed-expense-{id:N}";
        }

        string key = $"{item.Label}\u001f{item.IsIncome}\u001f{item.State}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return $"merge-confirmed-expense-{Convert.ToHexString(hash, 0, 8).ToLowerInvariant()}";
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
