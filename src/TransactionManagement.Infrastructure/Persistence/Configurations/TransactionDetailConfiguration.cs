using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionManagement.Domain.Common;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Infrastructure.Persistence.Configurations;

public sealed class TransactionDetailConfiguration : IEntityTypeConfiguration<TransactionDetail>
{
    public void Configure(EntityTypeBuilder<TransactionDetail> builder)
    {
        builder.ToTable(
            "TransactionDetails",
            table =>
            {
                table.HasCheckConstraint("CK_TransactionDetails_Quantity", "[Quantity] > 0");
                table.HasCheckConstraint("CK_TransactionDetails_Amount", "[Amount] >= 0");
            });

        builder.HasKey(detail => detail.Id);

        builder.Property(detail => detail.DetailDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(detail => detail.Description)
            .HasMaxLength(DomainConstants.Lengths.Description);

        builder.Property(detail => detail.Quantity)
            .IsRequired()
            .HasPrecision(DomainConstants.Precision.QuantityPrecision, DomainConstants.Precision.QuantityScale);

        builder.Property(detail => detail.Amount)
            .IsRequired()
            .HasPrecision(DomainConstants.Precision.AmountPrecision, DomainConstants.Precision.AmountScale);

        builder.Property(detail => detail.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(detail => detail.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(detail => detail.Product)
            .WithMany()
            .HasForeignKey(detail => detail.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(detail => detail.TransactionId)
            .HasDatabaseName("IX_TransactionDetails_TransactionId");

        builder.HasIndex(detail => detail.ProductId)
            .HasDatabaseName("IX_TransactionDetails_ProductId");
    }
}
