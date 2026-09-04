using TransactionManagement.Domain.Enums;

namespace TransactionManagement.Application.Dtos.Transactions;

/// <summary>
/// Full read model of a single transaction, used by the details and edit screens.
/// <see cref="RowVersion"/> travels to the browser and back so the save can detect a
/// concurrent modification.
/// </summary>
public sealed record TransactionDetailsDto(
    int Id,
    string TransactionNumber,
    DateTime TransactionDate,
    int BusinessPartnerId,
    string BusinessPartnerName,
    PartnerType PartnerType,
    string? Reference,
    string? Remarks,
    decimal TotalQuantity,
    decimal TotalAmount,
    byte[] RowVersion,
    IReadOnlyCollection<TransactionDetailDto> Details);
