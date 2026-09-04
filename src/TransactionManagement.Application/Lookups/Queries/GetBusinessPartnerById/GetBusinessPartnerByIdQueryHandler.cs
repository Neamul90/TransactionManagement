using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Application.Lookups.Queries.GetBusinessPartnerById;

public sealed class GetBusinessPartnerByIdQueryHandler
    : IRequestHandler<GetBusinessPartnerByIdQuery, BusinessPartnerLookupDto?>
{
    private readonly IBusinessPartnerRepository _businessPartnerRepository;

    public GetBusinessPartnerByIdQueryHandler(IBusinessPartnerRepository businessPartnerRepository)
    {
        _businessPartnerRepository = businessPartnerRepository;
    }

    public async Task<BusinessPartnerLookupDto?> Handle(
        GetBusinessPartnerByIdQuery request,
        CancellationToken cancellationToken) =>
        await _businessPartnerRepository
            .Query()
            .AsNoTracking()
            .Where(partner => partner.Id == request.BusinessPartnerId)
            .Select(partner => new BusinessPartnerLookupDto(
                partner.Id,
                partner.Code,
                partner.Name,
                partner.PartnerType))
            .FirstOrDefaultAsync(cancellationToken);
}
