using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Potsure.Infrastructure.Persistence.Identity;

namespace Potsure.Web.Configuration;

/// <summary>
/// Passkey assertion endpoints for sensitive-action step-up (FR-M022, constitution IV).
/// </summary>
public static class HostedPasskeyStepUpEndpoints
{
    public static WebApplication MapHostedPasskeyStepUpEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/identity/reauth").RequireAuthorization();

        group.MapPost("/passkey/options", async Task<IResult> (
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            HttpContext httpContext) =>
        {
            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            string optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);
            return Results.Content(optionsJson, "application/json");
        });

        group.MapPost("/passkey/verify", async Task<IResult> (
            [FromBody] PasskeyStepUpVerifyRequest body,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            HttpContext httpContext) =>
        {
            if (string.IsNullOrWhiteSpace(body.CredentialJson))
            {
                return Results.BadRequest();
            }

            ApplicationUser? user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            PasskeyAssertionResult<ApplicationUser> assertion =
                await signInManager.PerformPasskeyAssertionAsync(body.CredentialJson);
            if (!assertion.Succeeded)
            {
                return Results.Unauthorized();
            }

            if (assertion.User is null
                || !string.Equals(assertion.User.Id, user.Id, StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            }

            IdentityResult update = await userManager.AddOrUpdatePasskeyAsync(user, assertion.Passkey);
            if (!update.Succeeded)
            {
                return Results.Problem("Passkey verification could not be stored.");
            }

            return Results.Ok();
        });

        return app;
    }

    public sealed record PasskeyStepUpVerifyRequest(string CredentialJson);
}
