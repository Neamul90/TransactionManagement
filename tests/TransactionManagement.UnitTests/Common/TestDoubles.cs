using TransactionManagement.Application.Abstractions.Services;

namespace TransactionManagement.UnitTests.Common;

/// <summary>Deterministic clock so date-sensitive rules behave identically on every machine.</summary>
internal sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public FixedDateTimeProvider(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; }
}

internal sealed class StubTransactionNumberGenerator : ITransactionNumberGenerator
{
    private readonly string _transactionNumber;

    public StubTransactionNumberGenerator(string transactionNumber = "TRX-202609-00001")
    {
        _transactionNumber = transactionNumber;
    }

    public Task<string> GenerateAsync(DateTime transactionDate, CancellationToken cancellationToken = default) =>
        Task.FromResult(_transactionNumber);
}
