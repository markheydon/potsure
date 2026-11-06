namespace Potsure.Infrastructure.Tenancy;

public static class EntityOwnershipValidator
{
    public static void EnsureOwnedBy(string entityOwnerKey, string currentOwnerKey)
    {
        if (!string.Equals(entityOwnerKey, currentOwnerKey, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Entity is not owned by the current user.");
        }
    }
}
