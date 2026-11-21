namespace Potsure.Web.Services;

/// <summary>
/// Tracks recent reauthentication for export and deletion (FR-M022, T026).
/// </summary>
public sealed class ReauthenticationService
{
    private static readonly TimeSpan ValidWindow = TimeSpan.FromMinutes(5);
    private DateTimeOffset? _grantedUntil;

    /// <summary>
    /// Whether a recent successful reauthentication is still valid for this session.
    /// </summary>
    public bool IsReauthenticationValid =>
        _grantedUntil is DateTimeOffset until && until > DateTimeOffset.UtcNow;

    /// <summary>
    /// Returns true when export or deletion may proceed.
    /// </summary>
    public Task<bool> RequireReauthenticationAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(IsReauthenticationValid);

    /// <summary>
    /// Records successful reauthentication after the user completes the challenge UI.
    /// </summary>
    public void GrantReauthentication() =>
        _grantedUntil = DateTimeOffset.UtcNow.Add(ValidWindow);

    /// <summary>
    /// Clears any prior reauthentication grant.
    /// </summary>
    public void RevokeReauthentication() => _grantedUntil = null;
}
