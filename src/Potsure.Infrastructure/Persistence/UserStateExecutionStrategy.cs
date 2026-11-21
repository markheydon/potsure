namespace Potsure.Infrastructure.Persistence;

public sealed class UserStateExecutionStrategy
{
    public static Task ExecuteAsync(Func<Task> operation) => operation();
}
