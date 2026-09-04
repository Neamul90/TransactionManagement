namespace TransactionManagement.Application.Abstractions.Persistence;

/// <summary>
/// Provider-agnostic ambient database transaction. Declared here rather than exposing
/// EF Core's <c>IDbContextTransaction</c> so the Application layer stays free of a
/// persistence technology it does not otherwise need.
/// </summary>
public interface IDatabaseTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
