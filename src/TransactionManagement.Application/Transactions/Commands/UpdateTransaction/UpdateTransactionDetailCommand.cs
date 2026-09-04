namespace TransactionManagement.Application.Transactions.Commands.UpdateTransaction;

/// <summary>
/// One submitted detail line of an existing transaction.
/// <para>
/// <c>Id == 0</c> means the user added the row in the browser. A non-zero identifier is a claim
/// that the row already exists — a claim the aggregate verifies against its own collection before
/// acting on it, so a tampered identifier cannot reach another transaction's data.
/// </para>
/// </summary>
public sealed record UpdateTransactionDetailCommand(
    int Id,
    int ProductId,
    DateTime DetailDate,
    string? Description,
    decimal Quantity,
    decimal Amount,
    bool IsActive);
