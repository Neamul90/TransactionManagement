namespace TransactionManagement.Application.Abstractions.Persistence;

/// <summary>
/// Persistence behaviour that is genuinely common to every aggregate.
/// Aggregate specific access belongs on a specialised repository interface instead.
/// </summary>
public interface IRepository<TEntity>
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    /// <summary>
    /// Composable, non-tracking-agnostic query root used by read-side handlers to project
    /// straight into DTOs. Callers are responsible for applying <c>AsNoTracking()</c>.
    /// </summary>
    IQueryable<TEntity> Query();
}
