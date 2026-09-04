namespace TransactionManagement.Application.Abstractions.Services;

/// <summary>
/// Issues the next human-readable transaction number. Server-side only: the number is never
/// accepted from the browser, which removes an entire class of tampering and collision problems.
/// </summary>
public interface ITransactionNumberGenerator
{
    Task<string> GenerateAsync(DateTime transactionDate, CancellationToken cancellationToken = default);
}
