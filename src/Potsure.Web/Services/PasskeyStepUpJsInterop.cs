using Microsoft.JSInterop;

namespace Potsure.Web.Services;

public sealed class PasskeyStepUpJsInterop(IJSRuntime jsRuntime)
{
    public async Task<(bool Ok, string? Message)> PerformStepUpAsync(string requestVerificationToken)
    {
        try
        {
            StepUpResult result = await jsRuntime.InvokeAsync<StepUpResult>(
                "potsurePasskeyStepUp.performPasskeyStepUp",
                requestVerificationToken);
            return (result.Ok, result.Message);
        }
        catch (JSException)
        {
            return (false, "Passkey verification is not available in this browser.");
        }
    }

    private sealed record StepUpResult(bool Ok, string? Message);
}
