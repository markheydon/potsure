namespace Potsure.Data.Documents;

/// <summary>
/// Email lookup index at <c>identity/email-index/{sha256}.json</c>.
/// </summary>
public sealed class EmailIndexDocument
{
    public string UserId { get; set; } = string.Empty;
}
