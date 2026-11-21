using Potsure.Data.Documents;

namespace Potsure.Infrastructure.Identity;

public sealed class IdentityUserLoadResult
{
    public IdentityUserDocument? User { get; init; }

    public string? ETag { get; init; }

    public bool IsNew => User is null;
}