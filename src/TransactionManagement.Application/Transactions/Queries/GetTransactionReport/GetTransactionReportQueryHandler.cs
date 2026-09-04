using MediatR;
using TransactionManagement.Application.Abstractions.Reporting;
using TransactionManagement.Application.Dtos.Transactions;

namespace TransactionManagement.Application.Transactions.Queries.GetTransactionReport;

/// <summary>
/// Thin orchestration over the reporting abstraction. The report's data shape is owned by
/// <see cref="ITransactionReportService"/> so that report changes never ripple into screen queries.
/// </summary>
public sealed class GetTransactionReportQueryHandler
    : IRequestHandler<GetTransactionReportQuery, TransactionReportDto?>
{
    private readonly ITransactionReportService _transactionReportService;

    public GetTransactionReportQueryHandler(ITransactionReportService transactionReportService)
    {
        _transactionReportService = transactionReportService;
    }

    public Task<TransactionReportDto?> Handle(
        GetTransactionReportQuery request,
        CancellationToken cancellationToken) =>
        _transactionReportService.GetTransactionReportAsync(request.TransactionId, cancellationToken);
}
