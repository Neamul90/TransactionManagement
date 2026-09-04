using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TransactionManagement.Application.Transactions.Commands.CreateTransaction;
using TransactionManagement.Application.Transactions.Commands.DeleteTransaction;
using TransactionManagement.Application.Transactions.Commands.Shared;
using TransactionManagement.Application.Transactions.Commands.UpdateTransaction;
using TransactionManagement.IntegrationTests.Common;
using Xunit;

namespace TransactionManagement.IntegrationTests.Transactions;

/// <summary>
/// End-to-end persistence behaviour of the master–detail workflow against a real relational engine:
/// inserts, updates, orphan deletes and cascade deletes all go through the actual EF Core model.
/// </summary>
public sealed class TransactionPersistenceTests
{
    private static readonly DateTime Today = SqliteTestDatabase.Today;

    private static CreateTransactionCommandHandler CreateHandler(SqliteTestDatabase database) =>
        new(
            database.TransactionRepository,
            database.UnitOfWork,
            new StubTransactionNumberGenerator(),
            SqliteTestDatabase.Clock,
            new TransactionReferenceChecker(database.BusinessPartnerRepository, database.ProductRepository),
            NullLogger<CreateTransactionCommandHandler>.Instance);

    private static UpdateTransactionCommandHandler UpdateHandler(SqliteTestDatabase database) =>
        new(
            database.TransactionRepository,
            database.UnitOfWork,
            SqliteTestDatabase.Clock,
            new TransactionReferenceChecker(database.BusinessPartnerRepository, database.ProductRepository),
            NullLogger<UpdateTransactionCommandHandler>.Instance);

    private static DeleteTransactionCommandHandler DeleteHandler(SqliteTestDatabase database) =>
        new(
            database.TransactionRepository,
            database.UnitOfWork,
            NullLogger<DeleteTransactionCommandHandler>.Instance);

    private static async Task<int> GivenATransactionWithThreeLinesAsync(SqliteTestDatabase database)
    {
        var command = new CreateTransactionCommand(
            Today,
            BusinessPartnerId: 1,
            Reference: "PO-1001",
            Remarks: "Initial",
            Details:
            [
                new CreateTransactionDetailCommand(1, Today, "Line one", 2m, 100m, true),
                new CreateTransactionDetailCommand(2, Today, "Line two", 3m, 150m, true),
                new CreateTransactionDetailCommand(3, Today, "Line three", 4m, 200m, true)
            ]);

        var transactionId = await CreateHandler(database).Handle(command, CancellationToken.None);

        database.Detach();

        return transactionId;
    }

    [Fact]
    public async Task Create_PersistsTheMasterAndEveryDetailLine()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();

        var transactionId = await GivenATransactionWithThreeLinesAsync(database);

        var persisted = await database.Context.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Details)
            .SingleAsync(transaction => transaction.Id == transactionId);

        persisted.TransactionNumber.Should().Be("TRX-202609-00001");
        persisted.Details.Should().HaveCount(3);
        persisted.TotalQuantity.Should().Be(9m);
        persisted.TotalAmount.Should().Be(450m);
    }

    [Fact]
    public async Task Update_AddsModifiesAndDeletesDetailLinesInOneTransaction()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();

        var transactionId = await GivenATransactionWithThreeLinesAsync(database);

        var existingIds = await database.Context.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.Id == transactionId)
            .SelectMany(transaction => transaction.Details.OrderBy(detail => detail.Id).Select(detail => detail.Id))
            .ToListAsync();

        database.Detach();

        var command = new UpdateTransactionCommand(
            transactionId,
            Today,
            BusinessPartnerId: 2,
            Reference: "PO-2002",
            Remarks: "Revised",
            RowVersion: [1, 2, 3, 4, 5, 6, 7, 8],
            Details:
            [
                // First line unchanged, second modified, third omitted (deleted), one new line added.
                new UpdateTransactionDetailCommand(existingIds[0], 1, Today, "Line one", 2m, 100m, true),
                new UpdateTransactionDetailCommand(existingIds[1], 4, Today, "Line two revised", 9m, 900m, true),
                new UpdateTransactionDetailCommand(0, 3, Today, "Brand new", 1m, 25m, true)
            ]);

        await UpdateHandler(database).Handle(command, CancellationToken.None);

        database.Detach();

        var persisted = await database.Context.Transactions
            .AsNoTracking()
            .Include(transaction => transaction.Details)
            .SingleAsync(transaction => transaction.Id == transactionId);

        persisted.BusinessPartnerId.Should().Be(2);
        persisted.Reference.Should().Be("PO-2002");
        persisted.Details.Should().HaveCount(3);
        persisted.Details.Should().NotContain(detail => detail.Id == existingIds[2]);
        persisted.Details.Should().Contain(detail => detail.Description == "Brand new");
        persisted.TotalQuantity.Should().Be(12m);
        persisted.TotalAmount.Should().Be(1025m);
    }

    [Fact]
    public async Task Update_DeactivatingALine_ExcludesItFromTheTotals()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();

        var transactionId = await GivenATransactionWithThreeLinesAsync(database);

        var existingIds = await database.Context.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.Id == transactionId)
            .SelectMany(transaction => transaction.Details.OrderBy(detail => detail.Id).Select(detail => detail.Id))
            .ToListAsync();

        database.Detach();

        await UpdateHandler(database).Handle(
            new UpdateTransactionCommand(
                transactionId, Today, 1, "PO-1001", "Initial", [1, 2, 3, 4, 5, 6, 7, 8],
                [
                    new UpdateTransactionDetailCommand(existingIds[0], 1, Today, "Line one", 2m, 100m, true),
                    new UpdateTransactionDetailCommand(existingIds[1], 2, Today, "Line two", 3m, 150m, false),
                    new UpdateTransactionDetailCommand(existingIds[2], 3, Today, "Line three", 4m, 200m, false)
                ]),
            CancellationToken.None);

        database.Detach();

        var persisted = await database.Context.Transactions
            .AsNoTracking()
            .SingleAsync(transaction => transaction.Id == transactionId);

        persisted.TotalQuantity.Should().Be(2m);
        persisted.TotalAmount.Should().Be(100m);
    }

    [Fact]
    public async Task Delete_RemovesTheMasterAndCascadesToTheDetailLines()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();

        var transactionId = await GivenATransactionWithThreeLinesAsync(database);

        await DeleteHandler(database).Handle(
            new DeleteTransactionCommand(transactionId, [1, 2, 3, 4, 5, 6, 7, 8]),
            CancellationToken.None);

        database.Detach();

        var remainingTransactions = await database.Context.Transactions.CountAsync();
        var remainingDetails = await database.Context.Transactions
            .SelectMany(transaction => transaction.Details)
            .CountAsync();

        remainingTransactions.Should().Be(0);
        remainingDetails.Should().Be(0);
    }
}
