using FluentAssertions;
using TransactionManagement.Application.Transactions.Commands.CreateTransaction;
using TransactionManagement.Application.Transactions.Commands.UpdateTransaction;
using TransactionManagement.UnitTests.Common;
using Xunit;

namespace TransactionManagement.UnitTests.Application.Transactions;

/// <summary>
/// The validators are the outermost server-side gate. These tests assert that the browser cannot
/// slip past them, whatever the client-side script does or does not do.
/// </summary>
public sealed class TransactionCommandValidatorTests
{
    private static readonly DateTime Today = TransactionFactory.Today;

    private static readonly CreateTransactionCommandValidator CreateValidator =
        new(new FixedDateTimeProvider(Today));

    private static readonly UpdateTransactionCommandValidator UpdateValidator =
        new(new FixedDateTimeProvider(Today));

    private static CreateTransactionDetailCommand ValidCreateDetail() =>
        new(1, Today, "Line", 1m, 10m, true);

    [Fact]
    public void CreateValidator_AcceptsAWellFormedCommand()
    {
        var command = new CreateTransactionCommand(Today, 1, "PO-1", null, [ValidCreateDetail()]);

        CreateValidator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_RejectsAFutureTransactionDate()
    {
        var command = new CreateTransactionCommand(Today.AddDays(1), 1, null, null, [ValidCreateDetail()]);

        var result = CreateValidator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Transaction date cannot be in the future.");
    }

    [Fact]
    public void CreateValidator_RejectsATransactionWithNoDetails()
    {
        var command = new CreateTransactionCommand(Today, 1, null, null, []);

        var result = CreateValidator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "A transaction must contain at least one detail line.");
    }

    [Fact]
    public void CreateValidator_RejectsATransactionWithNoActiveDetail()
    {
        var command = new CreateTransactionCommand(
            Today, 1, null, null, [new CreateTransactionDetailCommand(1, Today, "Line", 1m, 10m, false)]);

        var result = CreateValidator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "A transaction must contain at least one active detail line.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateValidator_RejectsANonPositiveQuantity(decimal quantity)
    {
        var command = new CreateTransactionCommand(
            Today, 1, null, null, [new CreateTransactionDetailCommand(1, Today, "Line", quantity, 10m, true)]);

        var result = CreateValidator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Quantity must be greater than zero.");
    }

    [Fact]
    public void CreateValidator_RejectsANegativeAmount()
    {
        var command = new CreateTransactionCommand(
            Today, 1, null, null, [new CreateTransactionDetailCommand(1, Today, "Line", 1m, -1m, true)]);

        var result = CreateValidator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Amount cannot be negative.");
    }

    [Fact]
    public void CreateValidator_RejectsAMissingBusinessPartner()
    {
        var command = new CreateTransactionCommand(Today, 0, null, null, [ValidCreateDetail()]);

        var result = CreateValidator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == "Please select a customer or supplier.");
    }

    [Fact]
    public void UpdateValidator_RejectsAMissingConcurrencyToken()
    {
        var command = new UpdateTransactionCommand(
            1, Today, 1, null, null, [],
            [new UpdateTransactionDetailCommand(0, 1, Today, null, 1m, 1m, true)]);

        var result = UpdateValidator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "The concurrency token is missing. Please reload the transaction.");
    }

    [Fact]
    public void UpdateValidator_RejectsTheSameLineSubmittedTwice()
    {
        var command = new UpdateTransactionCommand(
            1, Today, 1, null, null, [1, 2, 3, 4, 5, 6, 7, 8],
            [
                new UpdateTransactionDetailCommand(11, 1, Today, null, 1m, 1m, true),
                new UpdateTransactionDetailCommand(11, 1, Today, null, 2m, 2m, true)
            ]);

        var result = UpdateValidator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == "The same detail line was submitted more than once.");
    }
}
