namespace TransactionManagement.Domain.Enums;

/// <summary>
/// Discriminates the role a business partner plays. A single partner table with a type column
/// keeps the transaction aggregate to a single foreign key while still distinguishing
/// customers from suppliers on screens and reports.
/// </summary>
public enum PartnerType
{
    Customer = 1,
    Supplier = 2
}
