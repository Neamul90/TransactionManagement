using MediatR;

namespace TransactionManagement.Application.Transactions.Commands.DeleteTransaction;

/// <summary>
/// Deletes a transaction and, by cascade, its detail lines.
/// The concurrency token is required so a delete cannot be issued against a stale view.
/// </summary>
public sealed record DeleteTransactionCommand(int TransactionId, byte[] RowVersion) : IRequest;
