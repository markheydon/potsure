using Potsure.Budgeting;

namespace Potsure.Web.Resources;

/// <summary>
/// User-facing labels for Monzo pot mapping modes (FR-M014).
/// </summary>
public static class PotMappingModeDisplay
{
    public static readonly PotMappingMode[] OptionsInDisplayOrder =
    [
        PotMappingMode.CategoryOnly,
        PotMappingMode.ThreePots,
        PotMappingMode.SeveralPots,
    ];

    public static string Format(PotMappingMode mode) =>
        mode switch
        {
            PotMappingMode.CategoryOnly => "Budget by category only (no Monzo pots)",
            PotMappingMode.ThreePots => "Three pots named after categories",
            PotMappingMode.SeveralPots => "Separate pot per expense",
            _ => mode.ToString(),
        };
}
