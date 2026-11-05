using Potsure.Infrastructure.Features.Export;

namespace Potsure.Web.Services;

/// <summary>
/// Bridges web reauthentication to export and deletion gates.
/// </summary>
public sealed class ExportReauthenticationGateAdapter(ReauthenticationService reauthenticationService)
    : IExportReauthenticationGate
{
    public Task<bool> ConfirmAsync(CancellationToken cancellationToken = default) =>
        reauthenticationService.RequireReauthenticationAsync(cancellationToken);
}
