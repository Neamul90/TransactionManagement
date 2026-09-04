using MediatR;
using TransactionManagement.Application.Dtos.Transactions;

namespace TransactionManagement.Application.Transactions.Queries.GetTransactionReport;

public sealed record GetTransactionReportQuery(int TransactionId) : IRequest<TransactionReportDto?>;
