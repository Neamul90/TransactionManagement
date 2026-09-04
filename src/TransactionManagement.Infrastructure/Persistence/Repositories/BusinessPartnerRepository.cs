using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Infrastructure.Persistence.Repositories;

public sealed class BusinessPartnerRepository : Repository<BusinessPartner>, IBusinessPartnerRepository
{
    public BusinessPartnerRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<bool> ExistsAndIsActiveAsync(
        int businessPartnerId,
        CancellationToken cancellationToken = default) =>
        await Set
            .AsNoTracking()
            .AnyAsync(
                partner => partner.Id == businessPartnerId && partner.IsActive,
                cancellationToken);
}
