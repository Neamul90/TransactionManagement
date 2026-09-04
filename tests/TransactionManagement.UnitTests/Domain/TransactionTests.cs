using FluentAssertions;
using TransactionManagement.Domain.Entities;
using TransactionManagement.Domain.Exceptions;
using TransactionManagement.UnitTests.Common;
using Xunit;

namespace TransactionManagement.UnitTests.Domain;

public sealed class TransactionTests
{
    [Fact]
    public void Create_WithValidValues_InitialisesTheAggregate()
    {
        var transaction = TransactionFactory.CreateNew();

        transaction.TransactionNumber.Should().Be("TRX-202609-00001");
        transaction.TransactionDate.Should().Be(TransactionFactory.Today);
        transaction.BusinessPartnerId.Should().Be(1);
        transaction.Details.Should().BeEmpty();
        transaction.TotalAmount.Should().Be(0m);
    }

    [Fact]
    public void Create_WithFutureTransactionDate_IsRejected()
    {
        var act = () => Transaction.Create(
            "TRX-202609-00002",
            TransactionFactory.Today.AddDays(1),
            businessPartnerId: 1,
            reference: null,
            remarks: null,
            referenceDateUtc: TransactionFactory.Today);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("Transaction date cannot be in the future.");
    }

    [Fact]
    public void Create_WithoutBusinessPartner_IsRejected()
    {
        var act = () => Transaction.Create(
            "TRX-202609-00003",
            TransactionFactory.Today,
            businessPartnerId: 0,
            reference: null,
            remarks: null,
            referenceDateUtc: TransactionFactory.Today);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("A customer or supplier must be selected.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddDetail_WithNonPositiveQuantity_IsRejected(decimal quantity)
    {
        var transaction = TransactionFactory.CreateNew();

        var act = () => transaction.AddDetail(1, TransactionFactory.Today, "Line", quantity, 10m, true);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("Quantity must be greater than zero.");
    }

    [Fact]
    public void AddDetail_WithNegativeAmount_IsRejected()
    {
        var transaction = TransactionFactory.CreateNew();

        var act = () => transaction.AddDetail(1, TransactionFactory.Today, "Line", 1m, -0.01m, true);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("Amount cannot be negative.");
    }

    [Fact]
    public void AddDetail_WithoutProduct_IsRejected()
    {
        var transaction = TransactionFactory.CreateNew();

        var act = () => transaction.AddDetail(0, TransactionFactory.Today, "Line", 1m, 10m, true);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("A product must be selected for every detail line.");
    }

    [Fact]
    public void AddDetail_DatedAfterTheTransaction_IsRejected()
    {
        var transaction = TransactionFactory.CreateNew();

        var act = () => transaction.AddDetail(
            1, TransactionFactory.Today.AddDays(1), "Line", 1m, 10m, true);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("A detail date cannot be later than the transaction date.");
    }

    [Fact]
    public void Totals_IncludeActiveLinesOnly()
    {
        var transaction = TransactionFactory.CreateNew();

        transaction.AddDetail(1, TransactionFactory.Today, "Active", 2m, 100m, isActive: true);
        transaction.AddDetail(2, TransactionFactory.Today, "Inactive", 5m, 500m, isActive: false);

        transaction.TotalQuantity.Should().Be(2m);
        transaction.TotalAmount.Should().Be(100m);
    }

    [Fact]
    public void EnsureAtLeastOneActiveDetail_WithOnlyInactiveLines_IsRejected()
    {
        var transaction = TransactionFactory.CreateNew();

        transaction.AddDetail(1, TransactionFactory.Today, "Inactive", 1m, 10m, isActive: false);

        Action act = transaction.EnsureAtLeastOneActiveDetail;

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("A transaction must contain at least one active detail line.");
    }

    [Fact]
    public void RemoveDetail_ForAnUnknownIdentifier_IsRejected()
    {
        var transaction = TransactionFactory.CreatePersistedWithThreeDetails();

        var act = () => transaction.RemoveDetail(999);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("Detail line 999 does not belong to transaction TRX-202609-00001.");
    }

    [Fact]
    public void ChangeHeader_UpdatesMasterFieldsButNeverTheTransactionNumber()
    {
        var transaction = TransactionFactory.CreatePersistedWithThreeDetails();

        transaction.ChangeHeader(
            TransactionFactory.Today,
            businessPartnerId: 42,
            reference: "PO-2002",
            remarks: "Revised",
            referenceDateUtc: TransactionFactory.Today);

        transaction.BusinessPartnerId.Should().Be(42);
        transaction.Reference.Should().Be("PO-2002");
        transaction.Remarks.Should().Be("Revised");
        transaction.TransactionNumber.Should().Be("TRX-202609-00001");
    }
}
