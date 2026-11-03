using Microsoft.JSInterop;

namespace Potsure.Web.Services;

public sealed class PasskeySignInJsInterop(IJSRuntime jsRuntime)
{
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            return await jsRuntime.InvokeAsync<bool>("potsurePasskeySignIn.isAvailable");
        }
        catch (JSException)
        {
            return false;
        }
    }

    public async Task<(bool Ok, string? Message)> SignInAsync(
        string? email,
        string requestVerificationToken)
    {
        try
        {
            PasskeyFlowResult result = await jsRuntime.InvokeAsync<PasskeyFlowResult>(
                "potsurePasskeySignIn.signInWithPasskey",
                email,
                requestVerificationToken);
            return (result.Ok, result.Message);
        }
        catch (JSException)
        {
            return (false, "Passkey sign-in is not available in this browser.");
        }
    }

    public async Task<(bool Ok, string? Message)> RegisterAsync(
        string email,
        string requestVerificationToken)
    {
        try
        {
            PasskeyFlowResult result = await jsRuntime.InvokeAsync<PasskeyFlowResult>(
                "potsurePasskeySignIn.registerPasskey",
                email,
                requestVerificationToken);
            return (result.Ok, result.Message);
        }
        catch (JSException)
        {
            return (false, "Passkey registration is not available in this browser.");
        }
    }

    private sealed record PasskeyFlowResult(bool Ok, string? Message);
}
