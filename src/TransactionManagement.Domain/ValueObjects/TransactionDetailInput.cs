namespace TransactionManagement.Domain.ValueObjects;

/// <summary>
/// Immutable description of a detail line as submitted by a caller.
/// <para>
/// <see cref="Id"/> is zero for a brand new line and carries the existing identifier for a line
/// that already exists in the database. The aggregate — not the caller — decides what that means.
/// </para>
/// </summary>
public sealed record TransactionDetailInput(
    int Id,
    int ProductId,
    DateTime DetailDate,
    string? Description,
    decimal Quantity,
    decimal Amount,
    bool IsActive);
