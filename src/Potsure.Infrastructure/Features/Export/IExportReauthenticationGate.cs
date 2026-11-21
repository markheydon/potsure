namespace Potsure.Infrastructure.Features.Export;

/// <summary>
/// Confirms the user has reauthenticated before export or deletion (FR-M022).
/// </summary>
public interface IExportReauthenticationGate
{
    Task<bool> ConfirmAsync(CancellationToken cancellationToken = default);
}
