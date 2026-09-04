using MediatR;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Application.Lookups.Queries.SearchBusinessPartners;

/// <summary>Bounded type-ahead over customers and suppliers. See <see cref="LookupDefaults"/>.</summary>
public sealed record SearchBusinessPartnersQuery(
    string? SearchTerm = null,
    int MaxResults = LookupDefaults.DefaultMaxResults)
    : IRequest<IReadOnlyCollection<BusinessPartnerLookupDto>>;
