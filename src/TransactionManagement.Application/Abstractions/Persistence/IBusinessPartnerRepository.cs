using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Application.Abstractions.Persistence;

public interface IBusinessPartnerRepository : IRepository<BusinessPartner>
{
    Task<bool> ExistsAndIsActiveAsync(int businessPartnerId, CancellationToken cancellationToken = default);
}
