using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Loads the aggregate tracked and complete. Tracking is required here — the update workflow
    /// relies on the change tracker to turn removed detail objects into DELETE statements.
    /// </summary>
    public async Task<Transaction?> GetWithDetailsAsync(
        int transactionId,
        CancellationToken cancellationToken = default) =>
        await Set
            .Include(transaction => transaction.Details)
            .SingleOrDefaultAsync(transaction => transaction.Id == transactionId, cancellationToken);

    public async Task<bool> ExistsAsync(int transactionId, CancellationToken cancellationToken = default) =>
        await Set
            .AsNoTracking()
            .AnyAsync(transaction => transaction.Id == transactionId, cancellationToken);

    public void SetOriginalRowVersion(Transaction transaction, byte[] rowVersion) =>
        Context.Entry(transaction)
            .Property(entity => entity.RowVersion)
            .OriginalValue = rowVersion;

    public async Task<bool> TransactionNumberExistsAsync(
        string transactionNumber,
        int? excludingTransactionId = null,
        CancellationToken cancellationToken = default) =>
        await Set
            .AsNoTracking()
            .AnyAsync(
                transaction => transaction.TransactionNumber == transactionNumber
                    && (excludingTransactionId == null || transaction.Id != excludingTransactionId),
                cancellationToken);
}
