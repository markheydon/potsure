using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Potsure.Budgeting;
using Potsure.Infrastructure.Persistence;
using Potsure.Infrastructure.Persistence.Entities;
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

    private bool _loading = true;
    private string? _error;
    private string? _statusMessage;
    private List<RecurrenceReviewItem> _suggested = [];
    private List<RecurrenceReviewItem> _confirmedOrRejected = [];
    private List<CategoryOption> _categories = [];
    private Dictionary<string, Guid> _expenseCategoryByLabel = [];
    private Dictionary<string, IncomeSplitInput> _incomeSplitsByLabel = [];
    private HashSet<string> _mergeSelection = [];
    private string? _mergeLabel;
    private Guid _mergeCategoryId;
    private string? _splitSourceLabel;
    private string _splitPartOneLabel = string.Empty;
    private string _splitPartTwoLabel = string.Empty;
    private Guid _splitPartOneCategoryId;
    private Guid _splitPartTwoCategoryId;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await ProfileSeeder.EnsureProfileAsync();
            await LoadCategoriesAsync();
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
            }

            if (item.IsIncome && !_incomeSplitsByLabel.ContainsKey(item.Label))
            {
                _incomeSplitsByLabel[item.Label] = new IncomeSplitInput();
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
                await IncomeService.SaveAsync(
                    new SaveIncomeSourceRequest(
                        item.PersistedId,
                        item.SourceLabelFallback(),
                        item.IncomeCadence ?? IncomeCadence.Monthly,
                        item.TypicalAmount,
                        null,
                        splits.Needs,
                        splits.Wants,
                        splits.SavingsDebt,
                        PatternConfirmationState.Confirmed));
            }
            else
            {
                Guid categoryId = _expenseCategoryByLabel[item.Label];
                RecurringExpenseEntity saved = await ExpenseService.SaveAsync(
                    new SaveRecurringExpenseRequest(
                        item.PersistedId,
                        item.Label,
                        categoryId,
                        MappedPotId: null,
                        item.ExpenseCadence ?? ExpenseCadence.Monthly,
                        PaymentType: "Card",
                        AmountKind: "Fixed",
                        item.TypicalAmount,
                        null,
                        null,
                        Tag: null,
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
                    new PatternResolution(_mergeCategoryId, null, null),
                    selected[0].ExpenseCadence ?? ExpenseCadence.Monthly,
                    "Fixed",
                    selected[0].TypicalAmount,
                    null,
                    null));

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
                            new PatternResolution(_splitPartOneCategoryId, null, null),
                            source.ExpenseCadence ?? ExpenseCadence.Monthly,
                            "Fixed",
                            source.TypicalAmount / 2m,
                            null,
                            null),
                        new SplitExpensePart(
                            _splitPartTwoLabel.Trim(),
                            new PatternResolution(_splitPartTwoCategoryId, null, null),
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

    private sealed class IncomeSplitInput
    {
        public decimal Needs { get; set; } = 50m;
        public decimal Wants { get; set; } = 30m;
        public decimal SavingsDebt { get; set; } = 20m;
    }
}

internal static class RecurrenceReviewItemExtensions
{
    internal static string SourceLabelFallback(this RecurrenceReviewItem item) => item.Label;
}
