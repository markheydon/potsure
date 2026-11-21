namespace Potsure.Statements;

/// <summary>
/// Detected Monzo CSV column layout before import confirmation (FR-M002).
/// </summary>
public sealed record MonzoColumnMappingPreview(
    IReadOnlyList<string> FileHeaders,
    IReadOnlyList<string> RequiredHeaders,
    IReadOnlyList<string> MissingRequiredHeaders,
    bool LayoutSupported);
