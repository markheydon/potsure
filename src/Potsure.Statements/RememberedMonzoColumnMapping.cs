namespace Potsure.Statements;

/// <summary>
/// Column map persisted on the budget profile when layout matches a prior import (FR-M002).
/// </summary>
public sealed record RememberedMonzoColumnMapping(
    IReadOnlyList<string> FileHeaders,
    IReadOnlyDictionary<string, string> ColumnMap)
{
    public bool MatchesLayout(IReadOnlyList<string> fileHeaders)
    {
        if (FileHeaders.Count != fileHeaders.Count)
        {
            return false;
        }

        for (int index = 0; index < FileHeaders.Count; index++)
        {
            if (!string.Equals(FileHeaders[index], fileHeaders[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
