using Potsure.Budgeting;

namespace Potsure.Web.Resources;

/// <summary>
/// User-facing labels for Monzo pot mapping modes (FR-M014).
/// </summary>
public static class PotMappingModeDisplay
{
    public static readonly PotMappingMode[] OptionsInDisplayOrder =
    [
        PotMappingMode.None,
        PotMappingMode.CategoryNamedPots,
        PotMappingMode.ExpenseSpecificPots,
    ];

    public static string Format(PotMappingMode mode) =>
        mode switch
        {
            PotMappingMode.None => "Budget by category only (no Monzo pots)",
            PotMappingMode.CategoryNamedPots => "Three pots named after categories",
            PotMappingMode.ExpenseSpecificPots => "Separate pot per expense",
            _ => mode.ToString(),
        };
}
