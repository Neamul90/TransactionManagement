using TransactionManagement.Domain.Common;
using TransactionManagement.Domain.Enums;

namespace TransactionManagement.Domain.Entities;

/// <summary>
/// A customer or a supplier. Referenced by <see cref="Transaction"/>; it is an aggregate root
/// in its own right because it has an independent lifecycle from any transaction.
/// </summary>
public sealed class BusinessPartner : BaseEntity, IAggregateRoot
{
    private BusinessPartner()
    {
        // Required by EF Core materialisation.
        Code = null!;
        Name = null!;
    }

    private BusinessPartner(string code, string name, PartnerType partnerType, string? email, string? phoneNumber)
    {
        Code = code;
        Name = name;
        PartnerType = partnerType;
        Email = email;
        PhoneNumber = phoneNumber;
        IsActive = true;
    }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public PartnerType PartnerType { get; private set; }

    public string? Email { get; private set; }

    public string? PhoneNumber { get; private set; }

    public bool IsActive { get; private set; }

    public static BusinessPartner Create(
        string code,
        string name,
        PartnerType partnerType,
        string? email = null,
        string? phoneNumber = null)
    {
        var normalisedCode = Guard.AgainstExceedingLength(
            Guard.AgainstNullOrWhiteSpace(code, "Business partner code is required."),
            DomainConstants.Lengths.PartnerCode,
            $"Business partner code cannot exceed {DomainConstants.Lengths.PartnerCode} characters.");

        var normalisedName = Guard.AgainstExceedingLength(
            Guard.AgainstNullOrWhiteSpace(name, "Business partner name is required."),
            DomainConstants.Lengths.PartnerName,
            $"Business partner name cannot exceed {DomainConstants.Lengths.PartnerName} characters.");

        Guard.Against(
            !Enum.IsDefined(partnerType),
            "Business partner type is not valid.");

        return new BusinessPartner(normalisedCode, normalisedName, partnerType, email?.Trim(), phoneNumber?.Trim());
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
