using Potsure.Data.Tenancy;
using Potsure.Infrastructure.Features.Export;
using Potsure.Infrastructure.Identity;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Infrastructure.Deletion;

/// <summary>
/// Permanently deletes all financial data for the current owner (FR-M023).
/// </summary>
public sealed class UserDataDeletionService(
    PotsureUserStateContext userState,
    IUserDocumentStore documentStore,
    IIdentityDocumentStore identityStore,
    ICurrentOwnerAccessor ownerAccessor,
    IExportReauthenticationGate reauthenticationGate)
{
    public async Task<bool> DeleteAllFinancialDataAsync(CancellationToken cancellationToken = default)
    {
        if (!await reauthenticationGate.ConfirmAsync(cancellationToken))
        {
            return false;
        }

        string ownerKey = ownerAccessor.OwnerKey;
        await UserStateExecutionStrategy.ExecuteAsync(async () =>
        {
            await documentStore.DeleteStateAsync(ownerKey, cancellationToken);
            userState.ClearAllFinancialData();
        });

        return true;
    }

    public Task DeleteHostedUserBlobsAsync(
        ApplicationUserIdentityDeletionRequest request,
        CancellationToken cancellationToken = default) =>
        HostedUserBlobDeletion.DeleteAsync(request, documentStore, identityStore, cancellationToken);
}

public sealed record ApplicationUserIdentityDeletionRequest(string UserId, string? Email);
