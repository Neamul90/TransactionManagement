using MediatR;
using Microsoft.Extensions.Logging;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Abstractions.Services;
using TransactionManagement.Application.Transactions.Commands.Shared;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Application.Transactions.Commands.CreateTransaction;

/// <summary>
/// Creates the aggregate and persists master and details in a single unit of work.
/// Structural validation has already run in the pipeline; what remains here is the
/// referential and domain validation that needs the database or the aggregate itself.
/// </summary>
public sealed class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, int>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionNumberGenerator _transactionNumberGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly TransactionReferenceChecker _referenceChecker;
    private readonly ILogger<CreateTransactionCommandHandler> _logger;

    public CreateTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        ITransactionNumberGenerator transactionNumberGenerator,
        IDateTimeProvider dateTimeProvider,
        TransactionReferenceChecker referenceChecker,
        ILogger<CreateTransactionCommandHandler> logger)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _transactionNumberGenerator = transactionNumberGenerator;
        _dateTimeProvider = dateTimeProvider;
        _referenceChecker = referenceChecker;
        _logger = logger;
    }

    public async Task<int> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        await _referenceChecker.EnsureBusinessPartnerIsUsableAsync(request.BusinessPartnerId, cancellationToken);

        await _referenceChecker.EnsureProductsAreUsableAsync(
            request.Details.Select(detail => detail.ProductId).ToList(),
            cancellationToken);

        var transactionNumber = await _transactionNumberGenerator
            .GenerateAsync(request.TransactionDate, cancellationToken);

        var transaction = Transaction.Create(
            transactionNumber,
            request.TransactionDate,
            request.BusinessPartnerId,
            request.Reference,
            request.Remarks,
            _dateTimeProvider.UtcNow);

        foreach (var detail in request.Details)
        {
            transaction.AddDetail(
                detail.ProductId,
                detail.DetailDate,
                detail.Description,
                detail.Quantity,
                detail.Amount,
                detail.IsActive);
        }

        transaction.EnsureAtLeastOneActiveDetail();

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        // One SaveChangesAsync for the master and every detail: EF Core wraps the whole
        // batch in a database transaction, so the aggregate is persisted atomically.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created transaction {TransactionNumber} (Id {TransactionId}) with {DetailCount} detail line(s)",
            transaction.TransactionNumber,
            transaction.Id,
            transaction.Details.Count);

        return transaction.Id;
    }
}
