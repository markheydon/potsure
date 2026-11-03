namespace Potsure.Web;

using Potsure.Web.Configuration;

public sealed class HostedFinancialDataAuthorizationMiddleware(
    RequestDelegate next,
    IConfiguration configuration)
{
    private static readonly PathString[] ProtectedPrefixes =
    [
        new("/import"),
        new("/plan"),
        new("/reports"),
        new("/recurrence"),
        new("/privacy"),
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        DeploymentModeOptions deployment = configuration
            .GetSection(DeploymentModeOptions.SectionName)
            .Get<DeploymentModeOptions>() ?? new DeploymentModeOptions();

        if (deployment.Mode != DeploymentMode.Hosted)
        {
            await next(context);
            return;
        }

        PathString path = context.Request.Path;
        if (ProtectedPrefixes.Any(prefix => path.StartsWithSegments(prefix))
            && !(context.User.Identity?.IsAuthenticated ?? false))
        {
            string returnUrl = (path + context.Request.QueryString).ToString();
            context.Response.Redirect(
                Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
                    "/account/sign-in",
                    "returnUrl",
                    returnUrl));
            return;
        }

        await next(context);
    }
}
