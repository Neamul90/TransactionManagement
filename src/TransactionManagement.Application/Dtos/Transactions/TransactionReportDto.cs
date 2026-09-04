namespace TransactionManagement.Application.Dtos.Transactions;

/// <summary>
/// Report model: header fields become RDLC report parameters, <see cref="Lines"/> becomes
/// the report dataset.
/// </summary>
public sealed class TransactionReportDto
{
    public int TransactionId { get; init; }

    public string TransactionNumber { get; init; } = string.Empty;

    public DateTime TransactionDate { get; init; }

    public string BusinessPartnerCode { get; init; } = string.Empty;

    public string BusinessPartnerName { get; init; } = string.Empty;

    /// <summary>"Customer" or "Supplier", resolved server side for the report header.</summary>
    public string BusinessPartnerType { get; init; } = string.Empty;

    public string Reference { get; init; } = string.Empty;

    public string Remarks { get; init; } = string.Empty;

    public decimal TotalQuantity { get; init; }

    public decimal TotalAmount { get; init; }

    public IReadOnlyCollection<TransactionReportLineDto> Lines { get; init; } = [];
}
