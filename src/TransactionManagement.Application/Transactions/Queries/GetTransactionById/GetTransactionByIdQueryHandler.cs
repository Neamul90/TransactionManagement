using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Dtos.Transactions;

namespace TransactionManagement.Application.Transactions.Queries.GetTransactionById;

/// <summary>
/// Single-round-trip read of one transaction and its details, projected directly into DTOs.
/// No <c>Include</c>, no change tracking and no entity ever reaches the presentation layer.
/// </summary>
public sealed class GetTransactionByIdQueryHandler
    : IRequestHandler<GetTransactionByIdQuery, TransactionDetailsDto?>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionByIdQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<TransactionDetailsDto?> Handle(
        GetTransactionByIdQuery request,
        CancellationToken cancellationToken) =>
        await _transactionRepository
            .Query()
            .AsNoTracking()
            .Where(transaction => transaction.Id == request.TransactionId)
            .Select(transaction => new TransactionDetailsDto(
                transaction.Id,
                transaction.TransactionNumber,
                transaction.TransactionDate,
                transaction.BusinessPartnerId,
                transaction.BusinessPartner!.Name,
                transaction.BusinessPartner.PartnerType,
                transaction.Reference,
                transaction.Remarks,
                transaction.TotalQuantity,
                transaction.TotalAmount,
                transaction.RowVersion,
                transaction.Details
                    .OrderBy(detail => detail.Id)
                    .Select(detail => new TransactionDetailDto(
                        detail.Id,
                        detail.ProductId,
                        detail.Product!.Code,
                        detail.Product.Name,
                        detail.Product.UnitOfMeasure,
                        detail.DetailDate,
                        detail.Description,
                        detail.Quantity,
                        detail.Amount,
                        detail.IsActive))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}
