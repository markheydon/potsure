using Potsure.Infrastructure.Identity;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Infrastructure.Deletion;

/// <summary>
/// Ordered removal of hosted user blobs (financial state, email index, identity user).
/// </summary>
public static class HostedUserBlobDeletion
{
    public static async Task DeleteAsync(
        ApplicationUserIdentityDeletionRequest request,
        IUserDocumentStore userDocuments,
        IIdentityDocumentStore identityDocuments,
        CancellationToken cancellationToken = default)
    {
        await userDocuments.DeleteStateAsync(request.UserId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            string emailHash = IdentityEmailHasher.HashEmailForIndex(request.Email);
            await identityDocuments.DeleteEmailIndexAsync(emailHash, cancellationToken);
        }

        await identityDocuments.DeleteUserAsync(request.UserId, cancellationToken);
    }
}
