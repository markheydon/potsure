namespace Potsure.Infrastructure.Persistence;

public sealed class DocumentStoreOptions
{
    public const string SectionName = "DocumentStore";

    public string DataDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Potsure", "data");
}
