using MediatR;

namespace TransactionManagement.Application.Transactions.Commands.UpdateTransaction;

/// <summary>
/// Applies the submitted master fields and the complete submitted detail collection to an
/// existing transaction. The submitted collection is authoritative: lines that are absent
/// from it are deleted.
/// </summary>
public sealed record UpdateTransactionCommand(
    int TransactionId,
    DateTime TransactionDate,
    int BusinessPartnerId,
    string? Reference,
    string? Remarks,
    byte[] RowVersion,
    IReadOnlyCollection<UpdateTransactionDetailCommand> Details)
    : IRequest;
