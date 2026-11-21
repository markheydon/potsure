using System.Linq.Expressions;
using Potsure.Data.Persistence;

namespace Potsure.Infrastructure.Persistence;

public static class DocumentSetQueryExtensions
{
    public static IQueryable<T> Where<T>(
        this DocumentSet<T> source,
        Expression<Func<T, bool>> predicate)
        where T : class =>
        source.AsQueryable().Where(predicate);

    public static IIncludableQueryable<TEntity, TProperty> Include<TEntity, TProperty>(
        this DocumentSet<TEntity> source,
        Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        where TEntity : class =>
        source.AsQueryable().Include(navigationPropertyPath);

    public static IQueryable<TEntity> ThenInclude<TEntity, TPreviousProperty, TProperty>(
        this IIncludableQueryable<TEntity, TPreviousProperty> source,
        Expression<Func<TPreviousProperty, TProperty>> navigationPropertyPath)
        where TEntity : class =>
        source.ThenInclude(navigationPropertyPath);

    public static IQueryable<T> AsNoTracking<T>(this DocumentSet<T> source)
        where T : class =>
        source.AsQueryable().AsNoTracking();

    public static async Task<T?> SingleOrDefaultAsync<T>(
        this DocumentSet<T> source,
        CancellationToken cancellationToken = default)
        where T : class
    {
        await source.PrepareQueryAsync(cancellationToken);
        return await source.AsQueryable().SingleOrDefaultAsync(cancellationToken);
    }

    public static async Task<T?> SingleOrDefaultAsync<T>(
        this DocumentSet<T> source,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : class
    {
        await source.PrepareQueryAsync(cancellationToken);
        return await source.AsQueryable().SingleOrDefaultAsync(predicate, cancellationToken);
    }

    public static async Task<T> SingleAsync<T>(
        this DocumentSet<T> source,
        CancellationToken cancellationToken = default)
        where T : class
    {
        await source.PrepareQueryAsync(cancellationToken);
        return await source.AsQueryable().SingleAsync(cancellationToken);
    }

    public static async Task<T> SingleAsync<T>(
        this DocumentSet<T> source,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : class
    {
        await source.PrepareQueryAsync(cancellationToken);
        return await source.AsQueryable().SingleAsync(predicate, cancellationToken);
    }

    public static async Task<List<T>> ToListAsync<T>(
        this DocumentSet<T> source,
        CancellationToken cancellationToken = default)
        where T : class
    {
        await source.PrepareQueryAsync(cancellationToken);
        return await source.AsQueryable().ToListAsync(cancellationToken);
    }

    public static async Task<int> CountAsync<T>(
        this DocumentSet<T> source,
        CancellationToken cancellationToken = default)
        where T : class
    {
        await source.PrepareQueryAsync(cancellationToken);
        return await source.AsQueryable().CountAsync(cancellationToken);
    }

    public static async Task<bool> AnyAsync<T>(
        this DocumentSet<T> source,
        CancellationToken cancellationToken = default)
        where T : class
    {
        await source.PrepareQueryAsync(cancellationToken);
        return await source.AsQueryable().AnyAsync(cancellationToken);
    }

    public static async Task<bool> AnyAsync<T>(
        this DocumentSet<T> source,
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
        where T : class
    {
        await source.PrepareQueryAsync(cancellationToken);
        return await source.AsQueryable().AnyAsync(predicate, cancellationToken);
    }
}
