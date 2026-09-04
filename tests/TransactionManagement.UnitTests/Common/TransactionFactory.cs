using TransactionManagement.Domain.Entities;

namespace TransactionManagement.UnitTests.Common;

/// <summary>
/// Builds transaction aggregates for tests, including the "already persisted" shape where every
/// detail line carries a database identifier.
/// </summary>
internal static class TransactionFactory
{
    internal static readonly DateTime Today = new(2026, 9, 1);

    internal static Transaction CreateNew(DateTime? transactionDate = null) =>
        Transaction.Create(
            "TRX-202609-00001",
            transactionDate ?? Today,
            businessPartnerId: 1,
            reference: "PO-1001",
            remarks: null,
            referenceDateUtc: Today);

    /// <summary>A transaction with three saved detail lines whose identifiers are 11, 12 and 13.</summary>
    internal static Transaction CreatePersistedWithThreeDetails()
    {
        var transaction = CreateNew().WithId(7);

        transaction.AddDetail(1, Today, "Line one", 2m, 100m, isActive: true);
        transaction.AddDetail(2, Today, "Line two", 3m, 150m, isActive: true);
        transaction.AddDetail(3, Today, "Line three", 4m, 200m, isActive: true);

        var identifier = 11;

        foreach (var detail in transaction.Details)
        {
            detail.WithId(identifier++);
        }

        return transaction;
    }
}
