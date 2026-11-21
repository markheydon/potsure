namespace Potsure.Web.Configuration;

/// <summary>
/// Validates deployment configuration before the host listens (FR-M024).
/// </summary>
public static class DeploymentModeStartupValidation
{
    /// <summary>
    /// Throws when local-only mode is combined with public endpoint binding.
    /// </summary>
    public static void EnsureValid(DeploymentModeOptions deployment)
    {
        if (deployment.Mode == DeploymentMode.LocalOnly && deployment.BindPublicEndpoints)
        {
            throw new InvalidOperationException(
                "Deployment:Mode LocalOnly cannot be used with BindPublicEndpoints true. " +
                "Use hosted mode with Identity for network-exposed deployments.");
        }
    }
}
