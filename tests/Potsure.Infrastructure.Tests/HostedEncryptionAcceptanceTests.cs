namespace Potsure.Infrastructure.Tests;

/// <summary>
/// FR-M027: asserts hosted encryption documentation and web host configuration hooks referenced in <c>docs/encryption.md</c>.
/// </summary>
public sealed class HostedEncryptionAcceptanceTests
{
    private static string RepoRoot => FindRepoRoot();

    [Fact]
    public void WebHost_ProgramIncludesHttpsAndHstsHooks()
    {
        string program = File.ReadAllText(Path.Combine(RepoRoot, "src", "Potsure.Web", "Program.cs"));
        Assert.Contains("UseHsts", program, StringComparison.Ordinal);
        Assert.Contains("UseHttpsRedirection", program, StringComparison.Ordinal);
        Assert.Contains("UseForwardedHeaders", program, StringComparison.Ordinal);
    }

    [Fact]
    public void WebHost_ProgramIncludesBlobStoragePersistenceHooks()
    {
        string program = File.ReadAllText(Path.Combine(RepoRoot, "src", "Potsure.Web", "Program.cs"));
        Assert.Contains("AddAzureBlobContainerClient", program, StringComparison.Ordinal);
        Assert.Contains("PersistKeysToAzureBlobStorage", program, StringComparison.Ordinal);
        Assert.Contains("BlobUserDocumentStore", program, StringComparison.Ordinal);
    }

    [Fact]
    public void EncryptionDocumentation_ReferencesAcceptanceTestsAndStorageEncryption()
    {
        string encryption = File.ReadAllText(Path.Combine(RepoRoot, "docs", "encryption.md"));
        Assert.Contains("HostedEncryptionAcceptanceTests", encryption, StringComparison.Ordinal);
        Assert.Contains("Azure Blob Storage", encryption, StringComparison.Ordinal);
        Assert.Contains("service encryption", encryption, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeploymentStorageDocumentation_DescribesPrivateUserdataContainer()
    {
        string storage = File.ReadAllText(Path.Combine(RepoRoot, "docs", "deployment-storage.md"));
        Assert.Contains("userdata", storage, StringComparison.Ordinal);
        Assert.Contains("private", storage, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Potsure.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root (Potsure.slnx).");
    }
}