namespace TransactionManagement.Application.Abstractions.Services;

/// <summary>
/// Abstracts the system clock so date-sensitive business rules stay deterministic under test.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
