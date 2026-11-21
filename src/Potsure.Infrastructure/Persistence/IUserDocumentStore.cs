using Potsure.Data.Documents;

namespace Potsure.Infrastructure.Persistence;

public interface IUserDocumentStore
{
    Task<DocumentLoadResult> LoadAsync(string userId, CancellationToken cancellationToken = default);

    Task SaveAsync(
        string userId,
        UserStateDocument document,
        string? ifMatchEtag,
        CancellationToken cancellationToken = default);

    Task DeleteStateAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> StateExistsAsync(string userId, CancellationToken cancellationToken = default);
}
