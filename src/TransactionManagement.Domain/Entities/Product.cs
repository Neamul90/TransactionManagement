using TransactionManagement.Domain.Common;

namespace TransactionManagement.Domain.Entities;

/// <summary>
/// A product that can appear on a transaction detail line.
/// </summary>
public sealed class Product : BaseEntity, IAggregateRoot
{
    private Product()
    {
        // Required by EF Core materialisation.
        Code = null!;
        Name = null!;
        UnitOfMeasure = null!;
    }

    private Product(string code, string name, string unitOfMeasure, decimal defaultUnitPrice)
    {
        Code = code;
        Name = name;
        UnitOfMeasure = unitOfMeasure;
        DefaultUnitPrice = defaultUnitPrice;
        IsActive = true;
    }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public string UnitOfMeasure { get; private set; }

    public decimal DefaultUnitPrice { get; private set; }

    public bool IsActive { get; private set; }

    public static Product Create(string code, string name, string unitOfMeasure, decimal defaultUnitPrice)
    {
        var normalisedCode = Guard.AgainstExceedingLength(
            Guard.AgainstNullOrWhiteSpace(code, "Product code is required."),
            DomainConstants.Lengths.ProductCode,
            $"Product code cannot exceed {DomainConstants.Lengths.ProductCode} characters.");

        var normalisedName = Guard.AgainstExceedingLength(
            Guard.AgainstNullOrWhiteSpace(name, "Product name is required."),
            DomainConstants.Lengths.ProductName,
            $"Product name cannot exceed {DomainConstants.Lengths.ProductName} characters.");

        var normalisedUnit = Guard.AgainstExceedingLength(
            Guard.AgainstNullOrWhiteSpace(unitOfMeasure, "Unit of measure is required."),
            DomainConstants.Lengths.UnitOfMeasure,
            $"Unit of measure cannot exceed {DomainConstants.Lengths.UnitOfMeasure} characters.");

        Guard.AgainstNegative(defaultUnitPrice, "Default unit price cannot be negative.");

        return new Product(normalisedCode, normalisedName, normalisedUnit, defaultUnitPrice);
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
