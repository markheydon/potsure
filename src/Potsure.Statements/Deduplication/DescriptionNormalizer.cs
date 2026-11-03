namespace Potsure.Statements.Deduplication;

public static class DescriptionNormalizer
{
    public static string Normalize(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        string trimmed = description.Trim();
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
