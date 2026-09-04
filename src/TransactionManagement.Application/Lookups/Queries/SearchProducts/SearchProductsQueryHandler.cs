using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Application.Lookups.Queries.SearchProducts;

public sealed class SearchProductsQueryHandler
    : IRequestHandler<SearchProductsQuery, IReadOnlyCollection<ProductLookupDto>>
{
    private readonly IProductRepository _productRepository;

    public SearchProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyCollection<ProductLookupDto>> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _productRepository
            .Query()
            .AsNoTracking()
            .Where(product => product.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();

            query = query.Where(product =>
                EF.Functions.Like(product.Name, $"%{term}%")
                || EF.Functions.Like(product.Code, $"%{term}%"));
        }

        return await query
            .OrderBy(product => product.Name)
            .Take(LookupDefaults.Clamp(request.MaxResults))
            .Select(product => new ProductLookupDto(
                product.Id,
                product.Code,
                product.Name,
                product.UnitOfMeasure,
                product.DefaultUnitPrice))
            .ToListAsync(cancellationToken);
    }
}
