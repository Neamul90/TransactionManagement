using MediatR;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Application.Lookups.Queries.GetBusinessPartnerLookup;

/// <summary>Active customers and suppliers for the master combo box.</summary>
public sealed record GetBusinessPartnerLookupQuery : IRequest<IReadOnlyCollection<BusinessPartnerLookupDto>>;
