using System.Reflection;

namespace Potsure.Infrastructure.Persistence;

public sealed class DocumentSet<T>
    where T : class
{
    private readonly Func<List<T>> listAccessor;
    private readonly Action? ensureLoadedSync;
    private readonly Func<CancellationToken, Task>? ensureLoadedAsync;

    public DocumentSet(
        Func<List<T>> listAccessor,
        Action? ensureLoadedSync = null,
        Func<CancellationToken, Task>? ensureLoadedAsync = null)
    {
        this.listAccessor = listAccessor;
        this.ensureLoadedSync = ensureLoadedSync;
        this.ensureLoadedAsync = ensureLoadedAsync;
    }

    internal async Task PrepareQueryAsync(CancellationToken cancellationToken)
    {
        if (ensureLoadedAsync is not null)
        {
            await ensureLoadedAsync(cancellationToken);
        }
    }

    private void OnMutation() => ensureLoadedSync?.Invoke();

    private List<T> List => listAccessor();

    public void Add(T entity)
    {
        OnMutation();
        List.Add(entity);
    }

    public void AddRange(IEnumerable<T> entities)
    {
        OnMutation();
        List.AddRange(entities);
    }

    public void Remove(T entity)
    {
        OnMutation();
        List<T> list = List;
        Guid? id = TryReadGuidId(entity);
        if (id is Guid entityId)
        {
            list.RemoveAll(item => TryReadGuidId(item) == entityId);
            return;
        }

        list.Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities)
    {
        OnMutation();
        foreach (T entity in entities)
        {
            Remove(entity);
        }
    }

    public void Clear()
    {
        OnMutation();
        List.Clear();
    }

    public IQueryable<T> AsQueryable() => List.AsQueryable();

    private static Guid? TryReadGuidId(T entity)
    {
        PropertyInfo? idProperty = entity.GetType().GetProperty("Id");
        if (idProperty?.PropertyType == typeof(Guid))
        {
            return (Guid?)idProperty.GetValue(entity);
        }

        return null;
    }
}
