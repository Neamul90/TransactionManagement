using MediatR;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Application.Lookups.Queries.GetBusinessPartnerById;

/// <summary>
/// Resolves the display text of the partner a form already has selected, so the picker can render
/// its one selected option without fetching the whole list.
/// </summary>
public sealed record GetBusinessPartnerByIdQuery(int BusinessPartnerId)
    : IRequest<BusinessPartnerLookupDto?>;
