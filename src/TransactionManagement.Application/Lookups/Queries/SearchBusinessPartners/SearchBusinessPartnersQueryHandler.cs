using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Application.Lookups.Queries.SearchBusinessPartners;

public sealed class SearchBusinessPartnersQueryHandler
    : IRequestHandler<SearchBusinessPartnersQuery, IReadOnlyCollection<BusinessPartnerLookupDto>>
{
    private readonly IBusinessPartnerRepository _businessPartnerRepository;

    public SearchBusinessPartnersQueryHandler(IBusinessPartnerRepository businessPartnerRepository)
    {
        _businessPartnerRepository = businessPartnerRepository;
    }

    public async Task<IReadOnlyCollection<BusinessPartnerLookupDto>> Handle(
        SearchBusinessPartnersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _businessPartnerRepository
            .Query()
            .AsNoTracking()
            .Where(partner => partner.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();

            query = query.Where(partner =>
                EF.Functions.Like(partner.Name, $"%{term}%")
                || EF.Functions.Like(partner.Code, $"%{term}%"));
        }

        return await query
            .OrderBy(partner => partner.Name)
            .Take(LookupDefaults.Clamp(request.MaxResults))
            .Select(partner => new BusinessPartnerLookupDto(
                partner.Id,
                partner.Code,
                partner.Name,
                partner.PartnerType))
            .ToListAsync(cancellationToken);
    }
}
