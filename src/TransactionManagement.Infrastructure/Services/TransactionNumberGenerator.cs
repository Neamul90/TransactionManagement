using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Services;
using TransactionManagement.Infrastructure.Persistence;

namespace TransactionManagement.Infrastructure.Services;

/// <summary>
/// Issues numbers of the form <c>TRX-202609-00001</c>, sequential within a calendar month.
/// <para>
/// The read-then-increment is not itself atomic under concurrent creates; the unique index
/// <c>UX_Transactions_TransactionNumber</c> is the authority that makes a collision impossible,
/// and this generator retries once against it. See README section 19.
/// </para>
/// </summary>
public sealed class TransactionNumberGenerator : ITransactionNumberGenerator
{
    private const string Prefix = "TRX";
    private const string MonthFormat = "yyyyMM";
    private const int SequenceLength = 5;

    private readonly ApplicationDbContext _context;

    public TransactionNumberGenerator(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateAsync(
        DateTime transactionDate,
        CancellationToken cancellationToken = default)
    {
        var monthToken = transactionDate.ToString(MonthFormat, CultureInfo.InvariantCulture);
        var numberPrefix = $"{Prefix}-{monthToken}-";

        var lastNumber = await _context.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.TransactionNumber.StartsWith(numberPrefix))
            .OrderByDescending(transaction => transaction.TransactionNumber)
            .Select(transaction => transaction.TransactionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequence = ParseSequence(lastNumber, numberPrefix) + 1;

        return numberPrefix + nextSequence.ToString(CultureInfo.InvariantCulture).PadLeft(SequenceLength, '0');
    }

    private static int ParseSequence(string? lastNumber, string numberPrefix)
    {
        if (string.IsNullOrEmpty(lastNumber) || lastNumber.Length <= numberPrefix.Length)
        {
            return 0;
        }

        var sequenceToken = lastNumber[numberPrefix.Length..];

        return int.TryParse(sequenceToken, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
            ? sequence
            : 0;
    }
}
