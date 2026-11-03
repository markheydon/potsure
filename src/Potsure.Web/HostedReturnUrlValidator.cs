namespace Potsure.Web;

/// <summary>
/// Validates post-authentication return URLs to prevent open redirects.
/// </summary>
public static class HostedReturnUrlValidator
{
    public static string Normalize(string? returnUrl, string fallback = "/")
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return fallback;
        }

        if (!IsLocalUrl(returnUrl))
        {
            return fallback;
        }

        return returnUrl;
    }

    private static bool IsLocalUrl(string url)
    {
        if (url[0] != '/')
        {
            return false;
        }

        if (url.StartsWith("//", StringComparison.Ordinal)
            || url.StartsWith("/\\", StringComparison.Ordinal))
        {
            return false;
        }

        return !url.Contains("://", StringComparison.Ordinal);
    }
}
