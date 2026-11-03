namespace Potsure.Web.Services;

public sealed class ReauthenticationService
{
    public Task<bool> RequireReauthenticationAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
