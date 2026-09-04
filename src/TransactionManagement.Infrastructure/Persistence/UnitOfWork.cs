using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Common.Exceptions;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Infrastructure.Persistence;

/// <summary>
/// Scoped unit of work over the shared <see cref="ApplicationDbContext"/>.
/// <para>
/// A single <see cref="SaveChangesAsync"/> already runs inside a database transaction, so the
/// inserted, updated and deleted detail rows of one command commit or roll back together.
/// <see cref="BeginTransactionAsync"/> exists for the rarer case of a command that must span
/// more than one save.
/// </para>
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                "Optimistic concurrency conflict detected while saving changes");

            throw new ConcurrencyConflictException(nameof(Transaction), exception);
        }
    }

    public async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        return new EfCoreDatabaseTransaction(transaction);
    }
}
