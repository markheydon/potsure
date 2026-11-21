namespace Potsure.Infrastructure.Persistence;

public interface IUserStateTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}

public sealed class NoOpUserStateTransaction : IUserStateTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
