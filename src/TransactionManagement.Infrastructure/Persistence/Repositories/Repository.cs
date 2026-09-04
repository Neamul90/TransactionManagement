using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Persistence;

namespace TransactionManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Common persistence behaviour shared by every aggregate repository.
/// It deliberately does not expose <c>SaveChanges</c>: committing is the unit of work's job.
/// </summary>
public class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    protected Repository(ApplicationDbContext context)
    {
        Context = context;
        Set = context.Set<TEntity>();
    }

    protected ApplicationDbContext Context { get; }

    protected DbSet<TEntity> Set { get; }

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync(new object?[] { id }, cancellationToken);

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public virtual async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default) =>
        await Set.AddRangeAsync(entities, cancellationToken);

    public virtual void Update(TEntity entity) => Set.Update(entity);

    public virtual void Remove(TEntity entity) => Set.Remove(entity);

    public virtual IQueryable<TEntity> Query() => Set.AsQueryable();
}
