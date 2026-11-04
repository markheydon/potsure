using System.Globalization;
using Potsure.Statements.Deduplication;

namespace Potsure.Statements;

/// <summary>
/// De-duplication key for imported Monzo transactions (FR-M004).
/// </summary>
public readonly record struct TransactionDedupeKey(string Value, bool IsTransactionId)
{
    public static TransactionDedupeKey FromMonzoTransactionId(string monzoTransactionId) =>
        new(monzoTransactionId.Trim(), IsTransactionId: true);

    public static TransactionDedupeKey FromDateAmountDescription(DateOnly date, decimal amount, string normalizedDescription) =>
        new(
            $"{date:yyyy-MM-dd}|{amount.ToString("0.00", CultureInfo.InvariantCulture)}|{normalizedDescription}",
            IsTransactionId: false);
}

/// <summary>
/// De-duplicates Monzo rows by transaction ID, else date, amount, and normalised description (FR-M004).
/// </summary>
public static class TransactionDeduplicator
{
    /// <summary>
    /// Returns unique rows in order and the number of duplicates removed within the batch and against existing keys.
    /// </summary>
    public static TransactionDeduplicationResult Deduplicate(
        IReadOnlyList<MonzoCsvRow> rows,
        IReadOnlyCollection<TransactionDedupeKey>? existingKeys = null)
    {
        HashSet<TransactionDedupeKey> seen = existingKeys is null
            ? []
            : new HashSet<TransactionDedupeKey>(existingKeys);
        List<MonzoCsvRow> unique = new(rows.Count);
        int duplicateCount = 0;

        foreach (MonzoCsvRow row in rows)
        {
            TransactionDedupeKey key = CreateKey(row);
            if (!seen.Add(key))
            {
                duplicateCount++;
                continue;
            }

            unique.Add(row);
        }

        return new TransactionDeduplicationResult(unique, duplicateCount);
    }

    /// <summary>
    /// Builds the de-duplication key for a parsed row.
    /// </summary>
    public static TransactionDedupeKey CreateKey(MonzoCsvRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.MonzoTransactionId))
        {
            return TransactionDedupeKey.FromMonzoTransactionId(row.MonzoTransactionId);
        }

        string normalizedDescription = DescriptionNormalizer.Normalize(
            string.IsNullOrWhiteSpace(row.Description) ? row.Name : row.Description);

        return TransactionDedupeKey.FromDateAmountDescription(
            row.TransactionDate,
            row.Amount,
            normalizedDescription);
    }
}

/// <summary>
/// Result of de-duplicating a batch of Monzo rows.
/// </summary>
public sealed record TransactionDeduplicationResult(
    IReadOnlyList<MonzoCsvRow> UniqueRows,
    int DuplicateCount);
