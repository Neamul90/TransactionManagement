using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Common.Exceptions;
using TransactionManagement.Application.Transactions.Commands.DeleteTransaction;
using TransactionManagement.Domain.Entities;
using TransactionManagement.UnitTests.Common;
using Xunit;

namespace TransactionManagement.UnitTests.Application.Transactions;

public sealed class DeleteTransactionCommandHandlerTests
{
    private static readonly byte[] RowVersion = [1, 2, 3, 4, 5, 6, 7, 8];

    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteTransactionCommandHandler CreateHandler() =>
        new(_transactionRepository, _unitOfWork, NullLogger<DeleteTransactionCommandHandler>.Instance);

    [Fact]
    public async Task Handle_RemovesTheAggregateAndCommitsOnce()
    {
        var transaction = TransactionFactory.CreatePersistedWithThreeDetails();

        _transactionRepository
            .GetWithDetailsAsync(transaction.Id, Arg.Any<CancellationToken>())
            .Returns(transaction);

        await CreateHandler().Handle(
            new DeleteTransactionCommand(transaction.Id, RowVersion),
            CancellationToken.None);

        _transactionRepository.Received(1).SetOriginalRowVersion(transaction, RowVersion);
        _transactionRepository.Received(1).Remove(transaction);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTheTransactionDoesNotExist_Throws()
    {
        _transactionRepository
            .GetWithDetailsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Transaction?)null);

        var act = () => CreateHandler().Handle(
            new DeleteTransactionCommand(404, RowVersion),
            CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .Where(exception => exception.EntityName == nameof(Transaction));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
