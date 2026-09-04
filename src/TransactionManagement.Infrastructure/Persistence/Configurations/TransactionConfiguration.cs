using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionManagement.Domain.Common;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.TransactionNumber)
            .IsRequired()
            .HasMaxLength(DomainConstants.Lengths.TransactionNumber);

        builder.Property(transaction => transaction.TransactionDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(transaction => transaction.Reference)
            .HasMaxLength(DomainConstants.Lengths.Reference);

        builder.Property(transaction => transaction.Remarks)
            .HasMaxLength(DomainConstants.Lengths.Remarks);

        builder.Property(transaction => transaction.TotalQuantity)
            .IsRequired()
            .HasPrecision(DomainConstants.Precision.QuantityPrecision, DomainConstants.Precision.QuantityScale);

        builder.Property(transaction => transaction.TotalAmount)
            .IsRequired()
            .HasPrecision(DomainConstants.Precision.AmountPrecision, DomainConstants.Precision.AmountScale);

        builder.Property(transaction => transaction.CreatedAtUtc)
            .IsRequired();

        // Optimistic concurrency for the whole aggregate: SQL Server maintains the token, and any
        // save issued from a stale copy of the transaction fails instead of overwriting.
        builder.Property(transaction => transaction.RowVersion)
            .IsRowVersion();

        builder.HasOne(transaction => transaction.BusinessPartner)
            .WithMany()
            .HasForeignKey(transaction => transaction.BusinessPartnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasMany(transaction => transaction.Details)
            .WithOne()
            .HasForeignKey(detail => detail.TransactionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // The detail collection is encapsulated behind a private list; EF Core writes to the
        // backing field so the aggregate never has to expose a mutable collection.
        // Configured after HasMany so the navigation is guaranteed to exist on the model.
        builder.Metadata
            .FindNavigation(nameof(Transaction.Details))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(transaction => transaction.TransactionNumber)
            .IsUnique()
            .HasDatabaseName("UX_Transactions_TransactionNumber");

        builder.HasIndex(transaction => transaction.TransactionDate)
            .HasDatabaseName("IX_Transactions_TransactionDate");

        builder.HasIndex(transaction => transaction.BusinessPartnerId)
            .HasDatabaseName("IX_Transactions_BusinessPartnerId");
    }
}
