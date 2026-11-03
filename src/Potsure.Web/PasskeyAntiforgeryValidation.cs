using Microsoft.AspNetCore.Antiforgery;

namespace Potsure.Web;

/// <summary>
/// Validates antiforgery tokens on passkey minimal API endpoints.
/// </summary>
public static class PasskeyAntiforgeryValidation
{
    public static async Task<IResult?> ValidateAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return null;
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest("Antiforgery validation failed.");
        }
    }
}
