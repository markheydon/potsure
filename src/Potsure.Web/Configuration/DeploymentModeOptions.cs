namespace Potsure.Web.Configuration;

public sealed class DeploymentModeOptions
{
    public const string SectionName = "Deployment";

    public DeploymentMode Mode { get; init; } = DeploymentMode.Hosted;

    public bool BindPublicEndpoints { get; init; } = true;
}

public enum DeploymentMode
{
    LocalOnly,
    Hosted,
}
