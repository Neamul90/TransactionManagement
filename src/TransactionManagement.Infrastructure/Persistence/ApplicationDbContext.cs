using Microsoft.EntityFrameworkCore;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Infrastructure.Persistence;

/// <summary>
/// The single EF Core context of the application.
/// <para>
/// Only aggregate roots are exposed as <see cref="DbSet{TEntity}"/>. <c>TransactionDetail</c> is
/// deliberately absent: detail rows are reachable only through their transaction, which keeps the
/// aggregate boundary enforceable at the persistence level as well as in the domain model.
/// </para>
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<BusinessPartner> BusinessPartners => Set<BusinessPartner>();

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Every IEntityTypeConfiguration in this assembly is applied; OnModelCreating stays empty
        // of per-entity detail no matter how many entities the model grows to.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
