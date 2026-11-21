using Potsure.Data.Documents;

namespace Potsure.Infrastructure.Identity;

public interface IIdentityDocumentStore
{
    Task<IdentityUserDocument?> LoadUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<IdentityUserLoadResult> LoadUserWithETagAsync(string userId, CancellationToken cancellationToken = default);

    Task SaveUserAsync(IdentityUserDocument user, string? ifMatchEtag, CancellationToken cancellationToken = default);

    Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<EmailIndexDocument?> LoadEmailIndexAsync(string normalizedEmailSha256, CancellationToken cancellationToken = default);

    Task SaveEmailIndexAsync(
        string normalizedEmailSha256,
        EmailIndexDocument index,
        string? ifMatchEtag,
        CancellationToken cancellationToken = default);

    Task DeleteEmailIndexAsync(string normalizedEmailSha256, CancellationToken cancellationToken = default);
}
