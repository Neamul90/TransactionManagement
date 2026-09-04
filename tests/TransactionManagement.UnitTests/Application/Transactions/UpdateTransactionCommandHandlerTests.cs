using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Common.Exceptions;
using TransactionManagement.Application.Transactions.Commands.Shared;
using TransactionManagement.Application.Transactions.Commands.UpdateTransaction;
using TransactionManagement.Domain.Entities;
using TransactionManagement.Domain.Exceptions;
using TransactionManagement.UnitTests.Common;
using Xunit;

namespace TransactionManagement.UnitTests.Application.Transactions;

public sealed class UpdateTransactionCommandHandlerTests
{
    private static readonly DateTime Today = TransactionFactory.Today;
    private static readonly byte[] RowVersion = [1, 2, 3, 4, 5, 6, 7, 8];

    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly IBusinessPartnerRepository _businessPartnerRepository = Substitute.For<IBusinessPartnerRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpdateTransactionCommandHandler CreateHandler() =>
        new(
            _transactionRepository,
            _unitOfWork,
            new FixedDateTimeProvider(Today),
            new TransactionReferenceChecker(_businessPartnerRepository, _productRepository),
            NullLogger<UpdateTransactionCommandHandler>.Instance);

    private void GivenReferencesAreValid()
    {
        _businessPartnerRepository
            .ExistsAndIsActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _productRepository
            .GetExistingActiveIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => (IReadOnlySet<int>)callInfo.Arg<IReadOnlyCollection<int>>().ToHashSet());
    }

    private Transaction GivenAPersistedTransaction()
    {
        var transaction = TransactionFactory.CreatePersistedWithThreeDetails();

        _transactionRepository
            .GetWithDetailsAsync(transaction.Id, Arg.Any<CancellationToken>())
            .Returns(transaction);

        return transaction;
    }

    [Fact]
    public async Task Handle_AppliesAddsUpdatesAndDeletesInASingleSave()
    {
        GivenReferencesAreValid();

        var transaction = GivenAPersistedTransaction();

        var command = new UpdateTransactionCommand(
            transaction.Id,
            Today,
            BusinessPartnerId: 1,
            Reference: "PO-1001",
            Remarks: null,
            RowVersion: RowVersion,
            Details:
            [
                // 11 unchanged, 12 modified, 13 omitted (deleted), one added.
                new UpdateTransactionDetailCommand(11, 1, Today, "Line one", 2m, 100m, true),
                new UpdateTransactionDetailCommand(12, 2, Today, "Line two revised", 9m, 900m, true),
                new UpdateTransactionDetailCommand(0, 4, Today, "Brand new", 1m, 25m, true)
            ]);

        await CreateHandler().Handle(command, CancellationToken.None);

        transaction.Details.Should().HaveCount(3);
        transaction.Details.Select(detail => detail.Id).Should().BeEquivalentTo(new[] { 11, 12, 0 });
        transaction.TotalAmount.Should().Be(1025m);

        _transactionRepository.Received(1).SetOriginalRowVersion(transaction, RowVersion);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheTransactionDoesNotExist_Throws()
    {
        _transactionRepository
            .GetWithDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var command = new UpdateTransactionCommand(
            404, Today, 1, null, null, RowVersion,
            [new UpdateTransactionDetailCommand(0, 1, Today, null, 1m, 1m, true)]);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(exception => exception.EntityName == nameof(Transaction));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithADetailThatBelongsToAnotherTransaction_DoesNotSave()
    {
        GivenReferencesAreValid();

        var transaction = GivenAPersistedTransaction();

        var command = new UpdateTransactionCommand(
            transaction.Id, Today, 1, null, null, RowVersion,
            [new UpdateTransactionDetailCommand(9999, 1, Today, null, 1m, 1m, true)]);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
