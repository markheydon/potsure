namespace Potsure.Data.Tenancy;

/// <summary>
/// Resolves the owning user key for hosted (Identity user id) or local-only mode.
/// </summary>
public interface ICurrentOwnerAccessor
{
    string OwnerKey { get; }

    bool IsAuthenticated { get; }
}
