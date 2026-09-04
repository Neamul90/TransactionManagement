using TransactionManagement.Application.Abstractions.Services;

namespace TransactionManagement.IntegrationTests.Common;

internal sealed class StubTransactionNumberGenerator : ITransactionNumberGenerator
{
    private int _sequence;

    public Task<string> GenerateAsync(DateTime transactionDate, CancellationToken cancellationToken = default) =>
        Task.FromResult($"TRX-202609-{(++_sequence).ToString().PadLeft(5, '0')}");
}
