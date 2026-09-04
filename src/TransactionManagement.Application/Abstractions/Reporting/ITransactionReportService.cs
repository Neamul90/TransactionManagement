using TransactionManagement.Application.Dtos.Transactions;

namespace TransactionManagement.Application.Abstractions.Reporting;

/// <summary>
/// Supplies the flattened data set consumed by the RDLC report. Kept separate from the
/// screen-oriented query handlers because reports have their own shape, totals and lifetime.
/// </summary>
public interface ITransactionReportService
{
    Task<TransactionReportDto?> GetTransactionReportAsync(
        int transactionId,
        CancellationToken cancellationToken = default);
}
