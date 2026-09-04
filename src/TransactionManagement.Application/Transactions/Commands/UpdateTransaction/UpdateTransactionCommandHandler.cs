using MediatR;
using Microsoft.Extensions.Logging;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Abstractions.Services;
using TransactionManagement.Application.Common.Exceptions;
using TransactionManagement.Application.Transactions.Commands.Shared;
using TransactionManagement.Domain.Entities;
using TransactionManagement.Domain.ValueObjects;

namespace TransactionManagement.Application.Transactions.Commands.UpdateTransaction;

/// <summary>
/// Master–detail update. The handler orchestrates; the aggregate decides which lines are new,
/// modified, deleted or unchanged. Everything is written in one unit of work.
/// </summary>
public sealed class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly TransactionReferenceChecker _referenceChecker;
    private readonly ILogger<UpdateTransactionCommandHandler> _logger;

    public UpdateTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        TransactionReferenceChecker referenceChecker,
        ILogger<UpdateTransactionCommandHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _referenceChecker = referenceChecker;
        _logger = logger;
    }

    public async Task Handle(UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _transactionRepository
            .GetWithDetailsAsync(request.TransactionId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Transaction), request.TransactionId);

        // The token the user was shown, not the one just read from the database.
        _transactionRepository.SetOriginalRowVersion(transaction, request.RowVersion);

        await _referenceChecker.EnsureBusinessPartnerIsUsableAsync(request.BusinessPartnerId, cancellationToken);

        await _referenceChecker.EnsureProductsAreUsableAsync(
            request.Details.Select(detail => detail.ProductId).ToList(),
            cancellationToken);

        transaction.ChangeHeader(
            request.TransactionDate,
            request.BusinessPartnerId,
            request.Reference,
            request.Remarks,
            _dateTimeProvider.UtcNow);

        var reconciliation = transaction.ApplyDetails(MapToDomainInputs(request.Details));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated transaction {TransactionNumber} (Id {TransactionId}); detail changes: {Reconciliation}",
            transaction.TransactionNumber,
            transaction.Id,
            reconciliation.ToString());
    }

    private static IReadOnlyCollection<TransactionDetailInput> MapToDomainInputs(
        IReadOnlyCollection<UpdateTransactionDetailCommand> details) =>
        details
            .Select(detail => new TransactionDetailInput(
                detail.Id,
                detail.ProductId,
                detail.DetailDate,
                detail.Description,
                detail.Quantity,
                detail.Amount,
                detail.IsActive))
            .ToList();
}
