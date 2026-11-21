namespace Potsure.Infrastructure.Persistence;

/// <summary>
/// Compatibility shim for call sites that used EF database facades. User state commits through
/// a single conditional blob replace in <see cref="PotsureUserStateContext.SaveChangesAsync"/>.
/// </summary>
public sealed class UserStateDatabaseFacade
{
    public static UserStateExecutionStrategy CreateExecutionStrategy() => new();

    [Obsolete("User state persists atomically via a single SaveChangesAsync; do not start fake transactions.")]
    public static Task<IUserStateTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IUserStateTransaction>(new NoOpUserStateTransaction());
}
