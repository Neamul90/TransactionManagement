namespace TransactionManagement.Application.Dtos.Transactions;

public sealed record TransactionDetailDto(
    int Id,
    int ProductId,
    string ProductCode,
    string ProductName,
    string UnitOfMeasure,
    DateTime DetailDate,
    string? Description,
    decimal Quantity,
    decimal Amount,
    bool IsActive);
