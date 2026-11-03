namespace Potsure.Infrastructure.Persistence.Tenancy;

/// <summary>
/// Fixed owner key for local-only deployments without remote Identity accounts.
/// </summary>
public sealed class LocalOnlyOwnerAccessor : ICurrentOwnerAccessor
{
    public const string LocalOwnerKey = "local-profile";

    public string OwnerKey => LocalOwnerKey;

    public bool IsAuthenticated => true;
}
