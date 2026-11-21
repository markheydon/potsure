using System.Collections;
using System.Linq.Expressions;

namespace Potsure.Data.Persistence;

/// <summary>
/// Async LINQ helpers for in-memory document queries (mirrors common EF Core patterns).
/// </summary>
public static class DocumentQueryExtensions
{
    public static IQueryable<T> AsNoTracking<T>(this IQueryable<T> source) => source;

    public static IIncludableQueryable<TEntity, TProperty> Include<TEntity, TProperty>(
        this IQueryable<TEntity> source,
        Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        where TEntity : class =>
        new DocumentIncludableQueryable<TEntity, TProperty>(source);

    public static IQueryable<TEntity> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, TPreviousProperty> source,
        Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
        where TEntity : class =>
        source;

    public static IQueryable<TEntity> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, IEnumerable<TPreviousProperty>> source,
        Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
        where TEntity : class =>
        source;

    public static Task<T?> SingleOrDefaultAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(source.SingleOrDefault());

    public static Task<T?> SingleOrDefaultAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(source.Where(predicate).SingleOrDefault());

    public static Task<T> SingleAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(source.Single());

    public static Task<T> SingleAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(source.Where(predicate).Single());

    public static Task<T?> FirstOrDefaultAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(source.FirstOrDefault());

    public static Task<T?> FirstOrDefaultAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(source.Where(predicate).FirstOrDefault());

    public static Task<List<T>> ToListAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(source.ToList());

    public static Task<int> CountAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(source.Count());

    public static Task<bool> AnyAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(source.Any());

    public static Task<bool> AnyAsync<T>(
        this IQueryable<T> source,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(source.Any(predicate));

    public static Task<HashSet<T>> ToHashSetAsync<T>(
        this IQueryable<T> source,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(source.ToHashSet());

    public static Task<Dictionary<TKey, TSource>> ToDictionaryAsync<TSource, TKey>(
        this IQueryable<TSource> source,
        Func<TSource, TKey> keySelector,
        CancellationToken cancellationToken = default)
        where TKey : notnull =>
        Task.FromResult(source.ToDictionary(keySelector));

    public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TSource, TKey, TElement>(
        this IQueryable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        CancellationToken cancellationToken = default)
        where TKey : notnull =>
        Task.FromResult(source.ToDictionary(keySelector, elementSelector));
}

/// <summary>Marker for Include chaining (no-op for document queries).</summary>
public interface IIncludableQueryable<out TEntity, out TProperty> : IQueryable<TEntity>
{
}

/// <summary>Wraps a queryable so <see cref="Include{TEntity,TProperty}"/> can chain.</summary>
internal sealed class DocumentIncludableQueryable<TEntity, TProperty> : IIncludableQueryable<TEntity, TProperty>
    where TEntity : class
{
    public DocumentIncludableQueryable(IQueryable<TEntity> source)
    {
        Expression = source.Expression;
        Provider = source.Provider;
    }

    public Type ElementType => typeof(TEntity);

    public Expression Expression { get; }

    public IQueryProvider Provider { get; }

    public IEnumerator<TEntity> GetEnumerator() => Provider.CreateQuery<TEntity>(Expression).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
