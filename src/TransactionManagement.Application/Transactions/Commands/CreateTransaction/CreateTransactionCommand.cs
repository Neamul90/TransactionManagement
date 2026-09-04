using MediatR;

namespace TransactionManagement.Application.Transactions.Commands.CreateTransaction;

/// <summary>
/// Creates a transaction together with all of its detail lines in one unit of work.
/// The transaction number is issued server side and is therefore not part of the command.
/// </summary>
public sealed record CreateTransactionCommand(
    DateTime TransactionDate,
    int BusinessPartnerId,
    string? Reference,
    string? Remarks,
    IReadOnlyCollection<CreateTransactionDetailCommand> Details)
    : IRequest<int>;
