using Potsure.Data.Tenancy;

namespace Potsure.Infrastructure.Tenancy;

/// <summary>
/// Local-only mode owner key (single implicit user on this device).
/// </summary>
public sealed class LocalOnlyOwnerAccessor : ICurrentOwnerAccessor
{
    public const string LocalOwnerKey = "local";

    public string OwnerKey => LocalOwnerKey;

    public bool IsAuthenticated => true;
}
