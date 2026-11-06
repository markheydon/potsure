using Potsure.Infrastructure.Features.Export;
using Potsure.Web.Services;

namespace Potsure.Web.Features.Privacy;

/// <summary>
/// Bridges web reauthentication to export and deletion gates.
/// </summary>
public sealed class ExportReauthenticationGateAdapter(ReauthenticationService reauthenticationService)
    : IExportReauthenticationGate
{
    public Task<bool> ConfirmAsync(CancellationToken cancellationToken = default) =>
        reauthenticationService.RequireReauthenticationAsync(cancellationToken);
}
