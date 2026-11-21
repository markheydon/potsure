namespace Potsure.Infrastructure.Persistence;

public sealed class DocumentStoreConcurrencyException : Exception
{
    public DocumentStoreConcurrencyException()
        : base("The document was modified by another request. Reload and retry.")
    {
    }
}
