namespace Potsure.Web.Security;

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

        if (returnUrl.Contains("://", StringComparison.Ordinal)
            || returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.StartsWith("/\\", StringComparison.Ordinal))
        {
            return fallback;
        }

        string path = returnUrl.StartsWith('/') ? returnUrl : $"/{returnUrl}";

        if (!IsLocalUrl(path))
        {
            return fallback;
        }

        return path;
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
