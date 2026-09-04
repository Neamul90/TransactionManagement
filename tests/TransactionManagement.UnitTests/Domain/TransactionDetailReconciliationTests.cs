using FluentAssertions;
using TransactionManagement.Domain.Exceptions;
using TransactionManagement.Domain.ValueObjects;
using TransactionManagement.UnitTests.Common;
using Xunit;

namespace TransactionManagement.UnitTests.Domain;

/// <summary>
/// The heart of the master–detail requirement: given the persisted lines and the submitted lines,
/// the aggregate must classify each one as new, modified, deleted or unchanged.
/// </summary>
public sealed class TransactionDetailReconciliationTests
{
    private static TransactionDetailInput Input(
        int id,
        int productId = 1,
        string? description = "Line one",
        decimal quantity = 2m,
        decimal amount = 100m,
        bool isActive = true) =>
        new(id, productId, TransactionFactory.Today, description, quantity, amount, isActive);

    [Fact]
    public void ApplyDetails_ClassifiesNewModifiedDeletedAndUnchangedLines()
    {
        var transaction = TransactionFactory.CreatePersistedWithThreeDetails();

        // 11 unchanged, 12 modified, 13 absent (deleted), plus one brand new line.
        var submitted = new List<TransactionDetailInput>
        {
            Input(11),
            Input(12, productId: 2, description: "Line two revised", quantity: 9m, amount: 900m),
            Input(0, productId: 4, description: "Brand new", quantity: 1m, amount: 25m)
        };

        var result = transaction.ApplyDetails(submitted);

        result.UnchangedCount.Should().Be(1);
        result.ModifiedCount.Should().Be(1);
        result.RemovedCount.Should().Be(1);
        result.AddedCount.Should().Be(1);
        result.HasChanges.Should().BeTrue();

        transaction.Details.Should().HaveCount(3);
        transaction.Details.Select(detail => detail.Id).Should().BeEquivalentTo(new[] { 11, 12, 0 });
    }

    [Fact]
    public void ApplyDetails_RecalculatesTotalsFromTheResultingLines()
    {
        var transaction = TransactionFactory.CreatePersistedWithThreeDetails();

        var submitted = new List<TransactionDetailInput>
        {
            Input(11, quantity: 1m, amount: 10m),
            Input(12, quantity: 2m, amount: 20m, isActive: false)
        };

        transaction.ApplyDetails(submitted);

        transaction.TotalQuantity.Should().Be(1m);
        transaction.TotalAmount.Should().Be(10m);
    }

    [Fact]
    public void ApplyDetails_WithADetailIdentifierFromAnotherTransaction_IsRejected()
    {
        var transaction = TransactionFactory.CreatePersistedWithThreeDetails();

        var act = () => transaction.ApplyDetails([Input(9999)]);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("Detail line 9999 does not belong to transaction TRX-202609-00001.");
    }

    [Fact]
    public void ApplyDetails_WithTheSameLineSubmittedTwice_IsRejected()
    {
        var transaction = TransactionFactory.CreatePersistedWithThreeDetails();

        var act = () => transaction.ApplyDetails([Input(11), Input(11, quantity: 5m)]);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("The same detail line was submitted more than once.");
    }

    [Fact]
    public void ApplyDetails_LeavingNoActiveLine_IsRejected()
    {
        var transaction = TransactionFactory.CreatePersistedWithThreeDetails();

        var act = () => transaction.ApplyDetails([Input(11, isActive: false)]);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("A transaction must contain at least one active detail line.");
    }

    [Fact]
    public void ApplyDetails_WithAnEmptySubmission_IsRejected()
    {
        var transaction = TransactionFactory.CreatePersistedWithThreeDetails();

        var act = () => transaction.ApplyDetails([]);

        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("A transaction must contain at least one active detail line.");
    }
}
