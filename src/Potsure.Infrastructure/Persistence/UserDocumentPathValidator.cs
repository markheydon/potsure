namespace Potsure.Infrastructure.Persistence;

public static class UserDocumentPathValidator
{
    public static void ValidateUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (userId.Contains('/', StringComparison.Ordinal)
            || userId.Contains('\\', StringComparison.Ordinal)
            || userId.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("User id contains invalid path characters.", nameof(userId));
        }
    }

    public static string StateBlobName(string userId)
    {
        ValidateUserId(userId);
        return $"tenants/{userId}/state.json";
    }

    public static string IdentityUserBlobName(string userId)
    {
        ValidateUserId(userId);
        return $"identity/users/{userId}.json";
    }

    public static string EmailIndexBlobName(string sha256Hex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256Hex);
        return $"identity/email-index/{sha256Hex}.json";
    }

    public static string DataProtectionBlobName() => "system/data-protection-keys.xml";
}
