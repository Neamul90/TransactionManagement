namespace TransactionManagement.Application.Dtos.Lookups;

public sealed record ProductLookupDto(
    int Id,
    string Code,
    string Name,
    string UnitOfMeasure,
    decimal DefaultUnitPrice)
{
    public string DisplayText => $"{Code} - {Name}";
}
