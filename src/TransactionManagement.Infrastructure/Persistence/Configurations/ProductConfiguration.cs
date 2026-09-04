using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionManagement.Domain.Common;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Code)
            .IsRequired()
            .HasMaxLength(DomainConstants.Lengths.ProductCode);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(DomainConstants.Lengths.ProductName);

        builder.Property(product => product.UnitOfMeasure)
            .IsRequired()
            .HasMaxLength(DomainConstants.Lengths.UnitOfMeasure);

        builder.Property(product => product.DefaultUnitPrice)
            .IsRequired()
            .HasPrecision(DomainConstants.Precision.AmountPrecision, DomainConstants.Precision.AmountScale);

        builder.Property(product => product.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(product => product.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(product => product.Code)
            .IsUnique()
            .HasDatabaseName("UX_Products_Code");

        builder.HasIndex(product => product.Name)
            .HasDatabaseName("IX_Products_Name");
    }
}
