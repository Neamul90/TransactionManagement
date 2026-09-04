using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Application.Lookups.Queries.GetProductLookup;

public sealed class GetProductLookupQueryHandler
    : IRequestHandler<GetProductLookupQuery, IReadOnlyCollection<ProductLookupDto>>
{
    private readonly IProductRepository _productRepository;

    public GetProductLookupQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyCollection<ProductLookupDto>> Handle(
        GetProductLookupQuery request,
        CancellationToken cancellationToken) =>
        await _productRepository
            .Query()
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .Select(product => new ProductLookupDto(
                product.Id,
                product.Code,
                product.Name,
                product.UnitOfMeasure,
                product.DefaultUnitPrice))
            .ToListAsync(cancellationToken);
}
