using Potsure.Data.Documents;

namespace Potsure.Infrastructure.Persistence;

public sealed class DocumentLoadResult
{
    public required UserStateDocument Document { get; init; }

    public string? ETag { get; init; }

    public bool IsNew { get; init; }
}
