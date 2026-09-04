using TransactionManagement.Domain.Enums;

namespace TransactionManagement.Application.Dtos.Lookups;

public sealed record BusinessPartnerLookupDto(
    int Id,
    string Code,
    string Name,
    PartnerType PartnerType)
{
    public string DisplayText => $"{Code} - {Name} ({PartnerType})";
}
