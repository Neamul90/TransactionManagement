using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Common.Exceptions;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Application.Transactions.Commands.Shared;

/// <summary>
/// Verifies that the foreign keys a transaction points at actually exist and are usable.
/// Shared by the create and update handlers so the rule is expressed exactly once,
/// and written as set-based lookups so detail validation never degenerates into N+1 queries.
/// </summary>
public sealed class TransactionReferenceChecker
{
    private readonly IBusinessPartnerRepository _businessPartnerRepository;
    private readonly IProductRepository _productRepository;

    public TransactionReferenceChecker(
        IBusinessPartnerRepository businessPartnerRepository,
        IProductRepository productRepository)
    {
        _businessPartnerRepository = businessPartnerRepository;
        _productRepository = productRepository;
    }

    public async Task EnsureBusinessPartnerIsUsableAsync(
        int businessPartnerId,
        CancellationToken cancellationToken)
    {
        var exists = await _businessPartnerRepository
            .ExistsAndIsActiveAsync(businessPartnerId, cancellationToken);

        if (!exists)
        {
            throw new EntityNotFoundException(nameof(BusinessPartner), businessPartnerId);
        }
    }

    public async Task EnsureProductsAreUsableAsync(
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken)
    {
        var distinctIds = productIds.Distinct().ToList();

        if (distinctIds.Count == 0)
        {
            return;
        }

        var existingIds = await _productRepository
            .GetExistingActiveIdsAsync(distinctIds, cancellationToken);

        var missingId = distinctIds.FirstOrDefault(id => !existingIds.Contains(id));

        if (missingId != 0)
        {
            throw new EntityNotFoundException(nameof(Product), missingId);
        }
    }
}
