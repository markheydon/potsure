using Microsoft.AspNetCore.Identity;
using Potsure.Data.Identity;

namespace Potsure.Web.Services;

/// <summary>
/// Validates the current user's password for sensitive actions in hosted mode.
/// </summary>
public sealed class PasswordReauthenticationService(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<bool> ValidatePasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User is null)
        {
            return false;
        }

        ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
        if (user is null)
        {
            return false;
        }

        return await userManager.CheckPasswordAsync(user, password);
    }
}
