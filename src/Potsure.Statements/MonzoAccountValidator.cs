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
    /// Compares a file-derived account identifier to the stored association; requires explicit user confirmation when identity cannot be verified from the file (FR-M006).
    /// </summary>
    /// <param name="fileDerivedAccountFingerprint">Parsed from statement metadata when available; null when the export provides no account identifier.</param>
    /// <param name="storedAccountFingerprint">Previously persisted identifier from a prior confirmed import.</param>
    /// <param name="userConfirmedAccount">True when the user explicitly confirmed this export belongs to their Monzo account.</param>
    public static MonzoAccountValidationResult ValidateAssociation(
        string? fileDerivedAccountFingerprint,
        string? storedAccountFingerprint,
        bool userConfirmedAccount)
    {
        string? fileId = string.IsNullOrWhiteSpace(fileDerivedAccountFingerprint)
            ? null
            : fileDerivedAccountFingerprint.Trim();
        string? storedId = string.IsNullOrWhiteSpace(storedAccountFingerprint)
            ? null
            : storedAccountFingerprint.Trim();

        if (fileId is not null
            && storedId is not null
            && !string.Equals(fileId, storedId, StringComparison.Ordinal))
        {
            return new MonzoAccountValidationResult(
                MonzoAccountValidationOutcome.DifferentAccountRejected,
                "This file appears to be from a different Monzo account.");
        }

        if (fileId is not null && storedId is not null)
        {
            return new MonzoAccountValidationResult(MonzoAccountValidationOutcome.Accepted, null);
        }

        if (userConfirmedAccount)
        {
            return new MonzoAccountValidationResult(MonzoAccountValidationOutcome.Accepted, null);
        }

        return new MonzoAccountValidationResult(
            MonzoAccountValidationOutcome.ConfirmationRequired,
            "Account identity could not be verified from the file; confirm this export is from your linked account.");
    }
}
