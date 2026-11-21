using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Potsure.Infrastructure.Persistence;

namespace Potsure.Infrastructure.Tests;

public sealed class DocumentSetBlazorLoadTests
{
    [Fact]
    public async Task EnsureProfile_CompletesUnderBlazorSynchronizationContext()
    {
        await using ServiceProvider provider = DocumentStoreTestHarness.BuildProvider("blazor-sync-context-user");

        using var syncContext = new SingleThreadSynchronizationContext();
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        syncContext.Post(
            _ =>
            {
                async Task RunOnBlazorLikeContextAsync()
                {
                    try
                    {
                        SynchronizationContext.SetSynchronizationContext(syncContext);
                        using IServiceScope scope = provider.CreateScope();
                        BudgetProfileSeeder seeder = scope.ServiceProvider.GetRequiredService<BudgetProfileSeeder>();
                        await seeder.EnsureProfileAsync(cancellationToken: TestContext.Current.CancellationToken);
                        completion.SetResult();
                    }
                    catch (Exception ex)
                    {
                        completion.SetException(ex);
                    }
                    finally
                    {
                        SynchronizationContext.SetSynchronizationContext(null);
                    }
                }

                _ = RunOnBlazorLikeContextAsync();
            },
            null);

        await completion.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    /// <summary>Minimal single-thread SynchronizationContext similar to Blazor Server dispatches.</summary>
    private sealed class SingleThreadSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> work =
            new();

        private readonly Thread thread;
        private bool disposed;

        public SingleThreadSynchronizationContext()
        {
            thread = new Thread(RunOnThread)
            {
                IsBackground = true,
                Name = "BlazorLoadTestSyncCtx",
            };
            thread.Start();
        }

        public override void Post(SendOrPostCallback d, object? state) => work.Add((d, state));

        public override void Send(SendOrPostCallback d, object? state)
        {
            using ManualResetEventSlim done = new(false);
            work.Add((arg =>
            {
                try
                {
                    d(arg);
                }
                finally
                {
                    done.Set();
                }
            }, state));
            done.Wait();
        }

        private void RunOnThread()
        {
            foreach ((SendOrPostCallback callback, object? state) in work.GetConsumingEnumerable())
            {
                if (disposed)
                {
                    return;
                }

                callback(state);
            }
        }

        public void Dispose()
        {
            disposed = true;
            work.CompleteAdding();
            thread.Join(TimeSpan.FromSeconds(5));
            work.Dispose();
        }
    }
}
