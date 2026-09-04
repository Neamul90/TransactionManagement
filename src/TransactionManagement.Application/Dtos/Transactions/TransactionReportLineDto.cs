namespace TransactionManagement.Application.Dtos.Transactions;

/// <summary>
/// Flattened detail row bound to the RDLC dataset. The RDLC designer binds by property name,
/// so this contract is treated as part of the report and changed with care.
/// </summary>
public sealed class TransactionReportLineDto
{
    public int LineNumber { get; init; }

    public string ProductCode { get; init; } = string.Empty;

    public string ProductName { get; init; } = string.Empty;

    public string UnitOfMeasure { get; init; } = string.Empty;

    public DateTime DetailDate { get; init; }

    public string Description { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal Amount { get; init; }

    public bool IsActive { get; init; }
}
