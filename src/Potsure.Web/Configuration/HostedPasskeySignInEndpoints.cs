using System.Security.Cryptography;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Potsure.Infrastructure.Persistence.Identity;
using Potsure.Web;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Potsure.Web.Configuration;

/// <summary>
/// Passkey registration and sign-in endpoints for hosted Blazor (MapIdentityApi has no passkey routes in .NET 10).
/// </summary>
public static class HostedPasskeySignInEndpoints
{
    public static WebApplication MapHostedPasskeySignInEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/identity/passkeys");

        group.MapPost("/requestOptions", async Task<IResult> (
            [FromBody] PasskeySignInOptionsRequest? body,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            HttpContext httpContext,
            IAntiforgery antiforgery) =>
        {
            IResult? antiforgeryFailure = await PasskeyAntiforgeryValidation.ValidateAsync(httpContext, antiforgery);
            if (antiforgeryFailure is not null)
            {
                return antiforgeryFailure;
            }

            ApplicationUser? user = null;
            if (!string.IsNullOrWhiteSpace(body?.Email))
            {
                user = await userManager.FindByEmailAsync(body.Email.Trim());
            }

            string optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);
            return Results.Content(optionsJson, "application/json");
        });

        group.MapPost("/login", async Task<IResult> (
            [FromBody] PasskeyCredentialRequest body,
            SignInManager<ApplicationUser> signInManager,
            HttpContext httpContext,
            IAntiforgery antiforgery) =>
        {
            IResult? antiforgeryFailure = await PasskeyAntiforgeryValidation.ValidateAsync(httpContext, antiforgery);
            if (antiforgeryFailure is not null)
            {
                return antiforgeryFailure;
            }

            if (string.IsNullOrWhiteSpace(body.CredentialJson))
            {
                return Results.BadRequest();
            }

            IdentitySignInResult result = await signInManager.PasskeySignInAsync(body.CredentialJson);
            return result.Succeeded ? Results.Ok() : Results.Unauthorized();
        });

        group.MapPost("/register/options", async Task<IResult> (
            [FromBody] PasskeyRegisterOptionsRequest body,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            HttpContext httpContext,
            IAntiforgery antiforgery) =>
        {
            IResult? antiforgeryFailure = await PasskeyAntiforgeryValidation.ValidateAsync(httpContext, antiforgery);
            if (antiforgeryFailure is not null)
            {
                return antiforgeryFailure;
            }

            if (string.IsNullOrWhiteSpace(body.Email))
            {
                return Results.BadRequest("Email is required to register a passkey.");
            }

            string email = body.Email.Trim();
            ApplicationUser? existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                return Results.BadRequest(
                    "An account already exists for this email. Sign in with your passkey instead of registering again.");
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
            };

            string unusablePassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(96));
            IdentityResult create = await userManager.CreateAsync(user, unusablePassword);
            if (!create.Succeeded)
            {
                return Results.BadRequest(string.Join(' ', create.Errors.Select(e => e.Description)));
            }

            string userId = await userManager.GetUserIdAsync(user) ?? user.Id;
            string userName = await userManager.GetUserNameAsync(user) ?? email;
            string optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
            {
                Id = userId,
                Name = userName,
                DisplayName = userName,
            });

            return Results.Content(optionsJson, "application/json");
        });

        group.MapPost("/register", async Task<IResult> (
            [FromBody] PasskeyCredentialRequest body,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            HttpContext httpContext,
            IAntiforgery antiforgery) =>
        {
            IResult? antiforgeryFailure = await PasskeyAntiforgeryValidation.ValidateAsync(httpContext, antiforgery);
            if (antiforgeryFailure is not null)
            {
                return antiforgeryFailure;
            }

            if (string.IsNullOrWhiteSpace(body.CredentialJson))
            {
                return Results.BadRequest();
            }

            PasskeyAttestationResult attestation =
                await signInManager.PerformPasskeyAttestationAsync(body.CredentialJson);
            if (!attestation.Succeeded
                || attestation.Passkey is null
                || attestation.UserEntity is null)
            {
                return Results.Unauthorized();
            }

            ApplicationUser? user = await userManager.FindByIdAsync(attestation.UserEntity.Id);
            if (user is null)
            {
                return Results.Unauthorized();
            }
            IdentityResult store = await userManager.AddOrUpdatePasskeyAsync(user, attestation.Passkey);
            if (!store.Succeeded)
            {
                return Results.Problem("Passkey registration could not be stored.");
            }

            await signInManager.SignInAsync(user, isPersistent: true);
            return Results.Ok();
        });

        return app;
    }

    public sealed record PasskeySignInOptionsRequest(string? Email);

    public sealed record PasskeyRegisterOptionsRequest(string Email);

    public sealed record PasskeyCredentialRequest(string CredentialJson);
}
