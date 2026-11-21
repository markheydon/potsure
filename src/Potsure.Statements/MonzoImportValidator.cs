namespace Potsure.Statements;

/// <summary>
/// Validates parsed Monzo rows before import (FR-M026).
/// </summary>
public static class MonzoImportValidator
{
    /// <summary>
    /// Rejects non-GBP rows with clear errors; no user override path in v1.
    /// </summary>
    public static IReadOnlyList<MonzoImportValidationError> ValidateRows(IReadOnlyList<MonzoCsvRow> rows)
    {
        List<MonzoImportValidationError> errors = new();
        for (int index = 0; index < rows.Count; index++)
        {
            MonzoCsvRow row = rows[index];
            if (!string.Equals(row.Currency, "GBP", StringComparison.Ordinal))
            {
                errors.Add(new MonzoImportValidationError(index, "Non-GBP currency"));
            }
        }

        return errors;
    }
}

/// <summary>
/// A row-level import validation failure (zero-based index into the parsed row list).
/// </summary>
public sealed record MonzoImportValidationError(int RowIndex, string Reason);
