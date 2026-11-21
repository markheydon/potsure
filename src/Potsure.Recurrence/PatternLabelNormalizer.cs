namespace Potsure.Recurrence;

/// <summary>
/// Normalises counterparty labels for pattern matching.
/// </summary>
public static class PatternLabelNormalizer
{
    /// <summary>
    /// Normalises a label for recurrence matching.
    /// </summary>
    public static string Normalize(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        string trimmed = label.Trim();
        Span<char> buffer = stackalloc char[trimmed.Length];
        int write = 0;
        bool previousWasSpace = false;
        foreach (char c in trimmed)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!previousWasSpace && write > 0)
                {
                    buffer[write++] = ' ';
                    previousWasSpace = true;
                }

                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                buffer[write++] = char.ToLowerInvariant(c);
                previousWasSpace = false;
            }
        }

        return write == 0 ? string.Empty : new string(buffer[..write]);
    }
}
