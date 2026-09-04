using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Common.Exceptions;
using TransactionManagement.Application.Transactions.Commands.CreateTransaction;
using TransactionManagement.Application.Transactions.Commands.Shared;
using TransactionManagement.Domain.Entities;
using TransactionManagement.UnitTests.Common;
using Xunit;

namespace TransactionManagement.UnitTests.Application.Transactions;

public sealed class CreateTransactionCommandHandlerTests
{
    private static readonly DateTime Today = TransactionFactory.Today;

    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly IBusinessPartnerRepository _businessPartnerRepository = Substitute.For<IBusinessPartnerRepository>();
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateTransactionCommandHandler CreateHandler() =>
        new(
            _transactionRepository,
            _unitOfWork,
            new StubTransactionNumberGenerator(),
            new FixedDateTimeProvider(Today),
            new TransactionReferenceChecker(_businessPartnerRepository, _productRepository),
            NullLogger<CreateTransactionCommandHandler>.Instance);

    private static CreateTransactionCommand ValidCommand() =>
        new(
            Today,
            BusinessPartnerId: 1,
            Reference: "PO-1001",
            Remarks: null,
            Details:
            [
                new CreateTransactionDetailCommand(5, Today, "Line one", 2m, 100m, true),
                new CreateTransactionDetailCommand(6, Today, "Line two", 3m, 150m, true)
            ]);

    private void GivenReferencesAreValid()
    {
        _businessPartnerRepository
            .ExistsAndIsActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _productRepository
            .GetExistingActiveIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => (IReadOnlySet<int>)callInfo.Arg<IReadOnlyCollection<int>>().ToHashSet());
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsTheAggregateInOneUnitOfWork()
    {
        GivenReferencesAreValid();

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        var addCall = _transactionRepository
            .ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(ITransactionRepository.AddAsync));

        var persisted = addCall.GetArguments()[0] as Transaction;

        persisted.Should().NotBeNull();
        persisted!.TransactionNumber.Should().Be("TRX-202609-00001");
        persisted.Details.Should().HaveCount(2);
        persisted.TotalQuantity.Should().Be(5m);
        persisted.TotalAmount.Should().Be(250m);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheBusinessPartnerDoesNotExist_Throws()
    {
        _businessPartnerRepository
            .ExistsAndIsActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(exception => exception.EntityName == nameof(BusinessPartner));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAProductDoesNotExist_Throws()
    {
        _businessPartnerRepository
            .ExistsAndIsActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _productRepository
            .GetExistingActiveIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlySet<int>)new HashSet<int> { 5 });

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(exception => exception.EntityName == nameof(Product));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
