using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    /// <summary>
    /// One round trip for the whole detail grid rather than one query per line.
    /// </summary>
    public async Task<IReadOnlySet<int>> GetExistingActiveIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return new HashSet<int>();
        }

        var existingIds = await Set
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id) && product.IsActive)
            .Select(product => product.Id)
            .ToListAsync(cancellationToken);

        return existingIds.ToHashSet();
    }
}
