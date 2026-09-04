namespace TransactionManagement.Application.Transactions.Commands.CreateTransaction;

/// <summary>
/// One submitted detail line of a brand new transaction. It deliberately carries no identifier:
/// every line of a new transaction is new.
/// </summary>
public sealed record CreateTransactionDetailCommand(
    int ProductId,
    DateTime DetailDate,
    string? Description,
    decimal Quantity,
    decimal Amount,
    bool IsActive);
