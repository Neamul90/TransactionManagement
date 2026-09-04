using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TransactionManagement.Application.Transactions.Commands.CreateTransaction;
using TransactionManagement.Application.Transactions.Commands.Shared;
using TransactionManagement.Application.Transactions.Queries.GetTransactions;
using TransactionManagement.IntegrationTests.Common;
using Xunit;

namespace TransactionManagement.IntegrationTests.Transactions;

/// <summary>
/// Pagination and search are asserted against the database because that is where they execute —
/// the point of the design is that neither ever materialises the whole table.
/// </summary>
public sealed class TransactionPaginationTests
{
    private static readonly DateTime Today = SqliteTestDatabase.Today;

    private static async Task SeedTransactionsAsync(SqliteTestDatabase database, int count)
    {
        var handler = new CreateTransactionCommandHandler(
            database.TransactionRepository,
            database.UnitOfWork,
            new StubTransactionNumberGenerator(),
            SqliteTestDatabase.Clock,
            new TransactionReferenceChecker(database.BusinessPartnerRepository, database.ProductRepository),
            NullLogger<CreateTransactionCommandHandler>.Instance);

        for (var index = 0; index < count; index++)
        {
            await handler.Handle(
                new CreateTransactionCommand(
                    Today.AddDays(-index),
                    BusinessPartnerId: index % 2 == 0 ? 1 : 2,
                    Reference: $"PO-{1000 + index}",
                    Remarks: null,
                    Details: [new CreateTransactionDetailCommand(1, Today.AddDays(-index), "Line", 1m, 10m, true)]),
                CancellationToken.None);

            database.Detach();
        }
    }

    [Fact]
    public async Task GetTransactions_ReturnsTheRequestedPageAndTheTotalCount()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await SeedTransactionsAsync(database, 25);

        var handler = new GetTransactionsQueryHandler(database.TransactionRepository);

        var result = await handler.Handle(
            new GetTransactionsQuery(PageNumber: 2, PageSize: 10),
            CancellationToken.None);

        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.PageNumber.Should().Be(2);
        result.Items.Should().HaveCount(10);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetTransactions_ClampsAnOversizedPageSize()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await SeedTransactionsAsync(database, 12);

        var handler = new GetTransactionsQueryHandler(database.TransactionRepository);

        var result = await handler.Handle(
            new GetTransactionsQuery(PageNumber: 1, PageSize: 5000),
            CancellationToken.None);

        result.PageSize.Should().Be(100);
        result.Items.Should().HaveCount(12);
    }

    [Fact]
    public async Task GetTransactions_FiltersByTheSearchTerm()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await SeedTransactionsAsync(database, 10);

        var handler = new GetTransactionsQueryHandler(database.TransactionRepository);

        var result = await handler.Handle(
            new GetTransactionsQuery(SearchTerm: "PO-1003"),
            CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetTransactions_SortsByTotalAmountWhenAsked()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await SeedTransactionsAsync(database, 5);

        var handler = new GetTransactionsQueryHandler(database.TransactionRepository);

        var result = await handler.Handle(
            new GetTransactionsQuery(
                SortBy: TransactionSortField.TransactionNumber,
                SortDescending: false),
            CancellationToken.None);

        result.Items.Select(item => item.TransactionNumber)
            .Should().BeInAscendingOrder();
    }
}
