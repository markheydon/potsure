namespace Potsure.Statements;

/// <summary>
/// Outcome of comparing a CSV account fingerprint with the user's association (FR-M005, FR-M006).
/// </summary>
public enum MonzoAccountValidationOutcome
{
    Accepted,
    DifferentAccountRejected,
    ConfirmationRequired,
}

/// <summary>
/// Result of Monzo account validation for an upload.
/// </summary>
public sealed record MonzoAccountValidationResult(
    MonzoAccountValidationOutcome Outcome,
    string? Detail);

/// <summary>
/// Validates Monzo account identity for statement imports.
/// </summary>
public static class MonzoAccountValidator
{
    private static readonly string[] MonzoRequiredHeaders =
    [
        "Date",
        "Type",
        "Name",
        "Amount",
        "Currency",
    ];

    /// <summary>
    /// Returns false when headers do not match a known Monzo export layout (FR-M002, FR-M005).
    /// </summary>
    public static bool IsMonzoLayout(IReadOnlyList<string> headers)
    {
        foreach (string required in MonzoRequiredHeaders)
        {
            if (!headers.Contains(required, StringComparer.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Compares file fingerprint to the associated account; requires explicit confirmation when identity is absent (FR-M006).
    /// </summary>
    public static MonzoAccountValidationResult ValidateAssociation(
        string? fileAccountFingerprint,
        string? associatedAccountFingerprint,
        bool explicitConfirmationProvided)
    {
        if (!string.IsNullOrWhiteSpace(fileAccountFingerprint)
            && !string.IsNullOrWhiteSpace(associatedAccountFingerprint)
            && !string.Equals(
                fileAccountFingerprint.Trim(),
                associatedAccountFingerprint.Trim(),
                StringComparison.Ordinal))
        {
            return new MonzoAccountValidationResult(
                MonzoAccountValidationOutcome.DifferentAccountRejected,
                "This file appears to be from a different Monzo account.");
        }

        if (string.IsNullOrWhiteSpace(fileAccountFingerprint)
            || string.IsNullOrWhiteSpace(associatedAccountFingerprint))
        {
            if (explicitConfirmationProvided)
            {
                return new MonzoAccountValidationResult(MonzoAccountValidationOutcome.Accepted, null);
            }

            return new MonzoAccountValidationResult(
                MonzoAccountValidationOutcome.ConfirmationRequired,
                "Account identity could not be verified from the file; confirm this export is from your linked account.");
        }

        return new MonzoAccountValidationResult(MonzoAccountValidationOutcome.Accepted, null);
    }
}
