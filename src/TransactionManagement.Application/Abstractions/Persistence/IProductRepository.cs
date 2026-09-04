using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Application.Abstractions.Persistence;

public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Returns the subset of <paramref name="productIds"/> that exist and are active.
    /// A single round trip keeps detail validation free of N+1 queries.
    /// </summary>
    Task<IReadOnlySet<int>> GetExistingActiveIdsAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default);
}
