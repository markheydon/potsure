using Potsure.Web.Configuration;

namespace Potsure.Web.Tests;

public sealed class DeploymentModeStartupValidationTests
{
    [Fact]
    public void EnsureValid_ThrowsWhenLocalOnlyWithPublicBinding()
    {
        DeploymentModeOptions options = new()
        {
            Mode = DeploymentMode.LocalOnly,
            BindPublicEndpoints = true,
        };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            DeploymentModeStartupValidation.EnsureValid(options));

        Assert.Contains("LocalOnly", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureValid_AllowsLocalOnlyOnLoopback()
    {
        DeploymentModeOptions options = new()
        {
            Mode = DeploymentMode.LocalOnly,
            BindPublicEndpoints = false,
        };

        DeploymentModeStartupValidation.EnsureValid(options);
    }
}
