namespace TransactionManagement.Domain.Common;

/// <summary>
/// Domain-wide constants. Keeps magic numbers out of entities, configurations and validators.
/// </summary>
public static class DomainConstants
{
    public static class Lengths
    {
        public const int TransactionNumber = 30;
        public const int PartnerCode = 20;
        public const int PartnerName = 150;
        public const int ProductCode = 30;
        public const int ProductName = 150;
        public const int UnitOfMeasure = 20;
        public const int Description = 250;
        public const int Reference = 50;
        public const int Remarks = 500;
        public const int Email = 150;
        public const int PhoneNumber = 30;
    }

    public static class Precision
    {
        public const int AmountPrecision = 18;
        public const int AmountScale = 2;

        public const int QuantityPrecision = 18;
        public const int QuantityScale = 3;
    }
}
