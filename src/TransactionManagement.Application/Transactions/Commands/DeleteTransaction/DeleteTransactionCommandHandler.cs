using MediatR;
using Microsoft.Extensions.Logging;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Common.Exceptions;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Application.Transactions.Commands.DeleteTransaction;

/// <summary>
/// Deletion is a hard delete of the whole aggregate: the detail rows are removed by the
/// cascade configured on the required Transaction–TransactionDetail relationship.
/// See README section 26 for why hard delete was chosen here.
/// </summary>
public sealed class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteTransactionCommandHandler> _logger;

    public DeleteTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteTransactionCommandHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository
            .GetWithDetailsAsync(request.TransactionId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Transaction), request.TransactionId);

        _transactionRepository.SetOriginalRowVersion(transaction, request.RowVersion);

        _transactionRepository.Remove(transaction);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted transaction {TransactionNumber} (Id {TransactionId})",
            transaction.TransactionNumber,
            transaction.Id);
    }
}
