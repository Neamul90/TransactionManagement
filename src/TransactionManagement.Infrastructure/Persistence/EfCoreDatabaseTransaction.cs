using Microsoft.EntityFrameworkCore.Storage;
using TransactionManagement.Application.Abstractions.Persistence;

namespace TransactionManagement.Infrastructure.Persistence;

/// <summary>
/// Adapts EF Core's <see cref="IDbContextTransaction"/> to the Application layer's
/// provider-agnostic <see cref="IDatabaseTransaction"/> abstraction.
/// </summary>
internal sealed class EfCoreDatabaseTransaction : IDatabaseTransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfCoreDatabaseTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        _transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
