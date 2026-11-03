using Microsoft.Extensions.Configuration;

namespace Potsure.Data.DesignTime;

/// <summary>
/// Resolves the <c>potsure</c> connection for EF design-time tools only.
/// Runtime and AppHost migration jobs use Aspire-injected configuration, not this helper.
/// </summary>
public static class DesignTimeAspireConnection
{
    public const string DatabaseReferenceName = "potsure";

    public static string Resolve(IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString(DatabaseReferenceName);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException(
            $"Connection string '{DatabaseReferenceName}' is not configured. " +
            "Design-time EF commands need the same Aspire client configuration as the running app " +
            $"(for example set ConnectionStrings__{DatabaseReferenceName} from the AppHost / dashboard, or run " +
            "`dotnet ef` while the AppHost has injected environment for the migration resource). " +
            "Do not hard-code SQL connection strings in source.");
    }

    public static IConfigurationRoot LoadConfiguration()
    {
        return new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
    }
}
