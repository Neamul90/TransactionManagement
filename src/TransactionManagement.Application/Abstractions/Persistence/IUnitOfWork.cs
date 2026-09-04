namespace TransactionManagement.Application.Abstractions.Persistence;

/// <summary>
/// One logical unit of work per command. Every repository resolved within a request shares the
/// same change tracker, so a single <see cref="SaveChangesAsync"/> persists the master record,
/// the inserted details, the updated details and the deleted details atomically.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an explicit transaction. Only required when a command must span more than one
    /// <see cref="SaveChangesAsync"/> call; a single save is already atomic.
    /// </summary>
    Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
