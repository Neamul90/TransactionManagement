using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Application.Abstractions.Persistence;

public interface ITransactionRepository : IRepository<Transaction>
{
    /// <summary>
    /// Loads the complete aggregate — master plus every detail line — tracked, so that the
    /// update workflow can add, modify and orphan-delete lines within a single unit of work.
    /// </summary>
    Task<Transaction?> GetWithDetailsAsync(int transactionId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds the concurrency token that the aggregate carried when it was shown to the user, so
    /// that a save issued against a stale copy fails instead of overwriting someone else's work.
    /// </summary>
    void SetOriginalRowVersion(Transaction transaction, byte[] rowVersion);

    Task<bool> TransactionNumberExistsAsync(
        string transactionNumber,
        int? excludingTransactionId = null,
        CancellationToken cancellationToken = default);
}
