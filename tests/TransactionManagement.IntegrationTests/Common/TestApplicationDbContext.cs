using Microsoft.EntityFrameworkCore;
using TransactionManagement.Domain.Entities;
using TransactionManagement.Infrastructure.Persistence;

namespace TransactionManagement.IntegrationTests.Common;

/// <summary>
/// The production model maps the aggregate's concurrency token to SQL Server's <c>rowversion</c>,
/// which SQLite has no equivalent for. This subclass relaxes just that one mapping so the rest of
/// the model — relationships, cascade behaviour, precision, indexes and constraints — is exercised
/// exactly as configured for production.
/// </summary>
internal sealed class TestApplicationDbContext : ApplicationDbContext
{
    public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>()
            .Property(transaction => transaction.RowVersion)
            .HasColumnType("BLOB")
            .ValueGeneratedNever()
            .IsConcurrencyToken(false);
    }
}
