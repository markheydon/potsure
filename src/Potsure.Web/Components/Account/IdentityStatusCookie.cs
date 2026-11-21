namespace Potsure.Web.Components.Account;

/// <summary>
/// Short-lived status messages shown after HTTP redirects in the Identity UI.
/// </summary>
internal static class IdentityStatusCookie
{
    public const string Name = "Identity.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    public static void Append(HttpContext context, string message) =>
        context.Response.Cookies.Append(Name, message, StatusCookieBuilder.Build(context));
}
