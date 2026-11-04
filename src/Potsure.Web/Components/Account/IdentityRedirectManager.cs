using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Potsure.Data.Identity;

namespace Potsure.Web.Components.Account;

public sealed class IdentityRedirectManager(NavigationManager navigationManager)
{
    public const string StatusCookieName = "Identity.StatusMessage";

    private static readonly CookieBuilder StatusCookieBuilder = new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    public void RedirectTo(string? uri)
    {
        string relativePath = CoerceToLocalRelativePath(uri);
        string normalized = HostedReturnUrlValidator.Normalize(relativePath);
        navigationManager.NavigateTo(normalized);
    }

    public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
    {
        string path = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
        string newUri = navigationManager.GetUriWithQueryParameters(path, queryParameters);
        RedirectTo(newUri);
    }

    public void RedirectToWithStatus(string uri, string message, HttpContext context)
    {
        context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
        RedirectTo(uri);
    }

    public void RedirectToCurrentPage() =>
        RedirectTo(navigationManager.ToBaseRelativePath(navigationManager.Uri));

    public void RedirectToCurrentPageWithStatus(string message, HttpContext context) =>
        RedirectToWithStatus(navigationManager.ToBaseRelativePath(navigationManager.Uri), message, context);

    public void RedirectToInvalidUser(UserManager<ApplicationUser> userManager, HttpContext context)
        => RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);

    private string CoerceToLocalRelativePath(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return "/";
        }

        if (Uri.TryCreate(uri, UriKind.Absolute, out Uri? absolute))
        {
            Uri baseUri = navigationManager.ToAbsoluteUri(navigationManager.BaseUri);
            if (!baseUri.IsBaseOf(absolute))
            {
                return "/";
            }

            string relative = navigationManager.ToBaseRelativePath(absolute.ToString());
            return string.IsNullOrEmpty(relative) ? "/" : relative;
        }

        return uri;
    }
}
