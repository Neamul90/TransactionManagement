using MediatR;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Application.Lookups.Queries.GetProductsByIds;

/// <summary>
/// Resolves display text for products already referenced by a form. Needed when a submitted form
/// fails validation and has to be redisplayed: the rows carry identifiers, not names.
/// </summary>
public sealed record GetProductsByIdsQuery(IReadOnlyCollection<int> ProductIds)
    : IRequest<IReadOnlyCollection<ProductLookupDto>>;
