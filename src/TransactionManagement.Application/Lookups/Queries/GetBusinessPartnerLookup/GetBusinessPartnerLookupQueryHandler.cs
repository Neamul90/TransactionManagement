using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Application.Lookups.Queries.GetBusinessPartnerLookup;

public sealed class GetBusinessPartnerLookupQueryHandler
    : IRequestHandler<GetBusinessPartnerLookupQuery, IReadOnlyCollection<BusinessPartnerLookupDto>>
{
    private readonly IBusinessPartnerRepository _businessPartnerRepository;

    public GetBusinessPartnerLookupQueryHandler(IBusinessPartnerRepository businessPartnerRepository)
    {
        _businessPartnerRepository = businessPartnerRepository;
    }

    public async Task<IReadOnlyCollection<BusinessPartnerLookupDto>> Handle(
        GetBusinessPartnerLookupQuery request,
        CancellationToken cancellationToken) =>
        await _businessPartnerRepository
            .Query()
            .AsNoTracking()
            .Where(partner => partner.IsActive)
            .OrderBy(partner => partner.Name)
            .Select(partner => new BusinessPartnerLookupDto(
                partner.Id,
                partner.Code,
                partner.Name,
                partner.PartnerType))
            .ToListAsync(cancellationToken);
}
