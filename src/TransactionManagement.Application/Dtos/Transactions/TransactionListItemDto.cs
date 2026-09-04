using TransactionManagement.Domain.Enums;

namespace TransactionManagement.Application.Dtos.Transactions;

/// <summary>Row of the transaction list grid. Contains only what the grid actually renders.</summary>
public sealed record TransactionListItemDto(
    int Id,
    string TransactionNumber,
    DateTime TransactionDate,
    string BusinessPartnerName,
    PartnerType PartnerType,
    int DetailCount,
    decimal TotalQuantity,
    decimal TotalAmount);
