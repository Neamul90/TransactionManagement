using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionManagement.Domain.Common;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Infrastructure.Persistence.Configurations;

public sealed class BusinessPartnerConfiguration : IEntityTypeConfiguration<BusinessPartner>
{
    public void Configure(EntityTypeBuilder<BusinessPartner> builder)
    {
        builder.ToTable("BusinessPartners");

        builder.HasKey(partner => partner.Id);

        builder.Property(partner => partner.Code)
            .IsRequired()
            .HasMaxLength(DomainConstants.Lengths.PartnerCode);

        builder.Property(partner => partner.Name)
            .IsRequired()
            .HasMaxLength(DomainConstants.Lengths.PartnerName);

        builder.Property(partner => partner.PartnerType)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(partner => partner.Email)
            .HasMaxLength(DomainConstants.Lengths.Email);

        builder.Property(partner => partner.PhoneNumber)
            .HasMaxLength(DomainConstants.Lengths.PhoneNumber);

        builder.Property(partner => partner.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(partner => partner.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(partner => partner.Code)
            .IsUnique()
            .HasDatabaseName("UX_BusinessPartners_Code");

        builder.HasIndex(partner => partner.Name)
            .HasDatabaseName("IX_BusinessPartners_Name");
    }
}
