using MediatR;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Application.Lookups.Queries.SearchProducts;

/// <summary>
/// Type-ahead over the product catalogue. It is deliberately bounded: the browser is never handed
/// the whole catalogue, which at production volumes would be tens of thousands of rows.
/// An empty term returns the first page, so a picker can show suggestions before anything is typed.
/// </summary>
public sealed record SearchProductsQuery(
    string? SearchTerm = null,
    int MaxResults = LookupDefaults.DefaultMaxResults)
    : IRequest<IReadOnlyCollection<ProductLookupDto>>;
