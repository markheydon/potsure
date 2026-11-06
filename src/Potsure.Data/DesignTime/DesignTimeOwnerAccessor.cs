using Potsure.Data.Tenancy;

namespace Potsure.Data.DesignTime;

/// <summary>
/// Stable owner key for EF design-time model building (query filters).
/// </summary>
internal sealed class DesignTimeOwnerAccessor : ICurrentOwnerAccessor
{
    public string OwnerKey => "design-time";

    public bool IsAuthenticated => true;
}
