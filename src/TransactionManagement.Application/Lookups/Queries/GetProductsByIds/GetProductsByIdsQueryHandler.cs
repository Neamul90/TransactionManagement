using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Application.Lookups.Queries.GetProductsByIds;

public sealed class GetProductsByIdsQueryHandler
    : IRequestHandler<GetProductsByIdsQuery, IReadOnlyCollection<ProductLookupDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductsByIdsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyCollection<ProductLookupDto>> Handle(
        GetProductsByIdsQuery request,
        CancellationToken cancellationToken)
    {
        var ids = request.ProductIds.Where(id => id > 0).Distinct().ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        return await _productRepository
            .Query()
            .AsNoTracking()
            .Where(product => ids.Contains(product.Id))
            .Select(product => new ProductLookupDto(
                product.Id,
                product.Code,
                product.Name,
                product.UnitOfMeasure,
                product.DefaultUnitPrice))
            .ToListAsync(cancellationToken);
    }
}
