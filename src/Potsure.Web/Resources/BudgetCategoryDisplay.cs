using Potsure.Budgeting;

namespace Potsure.Web.Resources;

/// <summary>
/// UK English labels and display order for budget categories (matches Needs / Wants / Savings strategy).
/// </summary>
public static class BudgetCategoryDisplay
{
    /// <summary>
    /// Standard order used in presets, reports, and category pickers.
    /// </summary>
    public static readonly BudgetCategoryKind[] StrategyOrder =
    [
        BudgetCategoryKind.Needs,
        BudgetCategoryKind.Wants,
        BudgetCategoryKind.SavingsAndDebt,
    ];

    public static string Format(BudgetCategoryKind kind) =>
        kind switch
        {
            BudgetCategoryKind.Needs => "Needs",
            BudgetCategoryKind.Wants => "Wants",
            BudgetCategoryKind.SavingsAndDebt => "Savings and debt",
            _ => kind.ToString(),
        };

    public static IEnumerable<T> OrderByStrategy<T>(
        IEnumerable<T> source,
        Func<T, BudgetCategoryKind> kindSelector) =>
        source.OrderBy(item => Array.IndexOf(StrategyOrder, kindSelector(item)));
}
