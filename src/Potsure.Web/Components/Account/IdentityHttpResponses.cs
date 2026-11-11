using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Primitives;
using Potsure.Web.Security;

namespace Potsure.Web.Components.Account;

/// <summary>
/// HTTP redirect helpers for Identity minimal API endpoints.
/// </summary>
internal static class IdentityHttpResponses
{
    public static RedirectHttpResult LocalRedirect(
        HttpContext context,
        string relativePath,
        string? statusMessage = null)
    {
        if (statusMessage is not null)
        {
            IdentityStatusCookie.Append(context, statusMessage);
        }

        string path = HostedReturnUrlValidator.Normalize(relativePath);
        return TypedResults.LocalRedirect($"~{path}");
    }

    public static RedirectHttpResult LocalRedirectWithQuery(
        HttpContext context,
        string relativePath,
        IReadOnlyDictionary<string, StringValues> query,
        string? statusMessage = null)
    {
        if (statusMessage is not null)
        {
            IdentityStatusCookie.Append(context, statusMessage);
        }

        string path = UriHelper.BuildRelative(context.Request.PathBase, relativePath, QueryString.Create(query));
        return TypedResults.LocalRedirect(path);
    }
}
