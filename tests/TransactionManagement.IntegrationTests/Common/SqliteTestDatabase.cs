using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Abstractions.Services;
using TransactionManagement.Domain.Entities;
using TransactionManagement.Domain.Enums;
using TransactionManagement.Infrastructure.Persistence;
using TransactionManagement.Infrastructure.Persistence.Repositories;

namespace TransactionManagement.IntegrationTests.Common;

/// <summary>
/// A real relational database per test, held in memory. Each instance owns its own connection, so
/// tests are isolated and can run in parallel.
/// </summary>
internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private SqliteTestDatabase(SqliteConnection connection, TestApplicationDbContext context)
    {
        _connection = connection;
        Context = context;
        TransactionRepository = new TransactionRepository(context);
        BusinessPartnerRepository = new BusinessPartnerRepository(context);
        ProductRepository = new ProductRepository(context);
        UnitOfWork = new UnitOfWork(context, NullLogger<UnitOfWork>.Instance);
    }

    internal ApplicationDbContext Context { get; }

    internal ITransactionRepository TransactionRepository { get; }

    internal IBusinessPartnerRepository BusinessPartnerRepository { get; }

    internal IProductRepository ProductRepository { get; }

    internal IUnitOfWork UnitOfWork { get; }

    internal static readonly DateTime Today = new(2026, 9, 1);

    internal static IDateTimeProvider Clock { get; } = new FixedClock(Today);

    internal static async Task<SqliteTestDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var database = new SqliteTestDatabase(connection, context);
        await database.SeedMasterDataAsync();

        return database;
    }

    /// <summary>Four products and two partners, enough for every master–detail scenario.</summary>
    private async Task SeedMasterDataAsync()
    {
        Context.BusinessPartners.AddRange(
            BusinessPartner.Create("CUS-001", "Meridian Retail Group", PartnerType.Customer),
            BusinessPartner.Create("SUP-001", "Orion Industrial Supplies", PartnerType.Supplier));

        Context.Products.AddRange(
            Product.Create("PRD-001", "A4 Copier Paper", "Ream", 4.75m),
            Product.Create("PRD-002", "Ballpoint Pen", "Box", 12.50m),
            Product.Create("PRD-003", "Laser Toner", "Piece", 89.00m),
            Product.Create("PRD-004", "Packing Tape", "Roll", 2.30m));

        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    /// <summary>Detaches everything so the next read genuinely round-trips through the database.</summary>
    internal void Detach() => Context.ChangeTracker.Clear();

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; }
    }
}
