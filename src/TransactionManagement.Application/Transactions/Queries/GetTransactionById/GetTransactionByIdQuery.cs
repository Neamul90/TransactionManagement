using MediatR;
using TransactionManagement.Application.Dtos.Transactions;

namespace TransactionManagement.Application.Transactions.Queries.GetTransactionById;

public sealed record GetTransactionByIdQuery(int TransactionId) : IRequest<TransactionDetailsDto?>;
