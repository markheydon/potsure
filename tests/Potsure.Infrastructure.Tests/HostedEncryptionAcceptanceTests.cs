namespace Potsure.Infrastructure.Tests;

public sealed class HostedEncryptionAcceptanceTests
{
    [Fact]
    public void EncryptionDoc_ListsTransitAndRestChecklist()
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "docs", "encryption.md"));
        string content = File.ReadAllText(path);
        Assert.Contains("HTTPS", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TDE", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("checklist", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppHost_ConfiguresAzureSqlForHostedDeployment()
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Potsure.AppHost", "AppHost.cs"));
        string content = File.ReadAllText(path);
        Assert.Contains("AddAzureSqlServer", content, StringComparison.Ordinal);
        Assert.Contains("WithExternalHttpEndpoints", content, StringComparison.Ordinal);
    }

    [Fact]
    public void WebHost_EnforcesHttpsRedirection()
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Potsure.Web", "Program.cs"));
        string content = File.ReadAllText(path);
        Assert.Contains("UseHttpsRedirection", content, StringComparison.Ordinal);
    }
}
