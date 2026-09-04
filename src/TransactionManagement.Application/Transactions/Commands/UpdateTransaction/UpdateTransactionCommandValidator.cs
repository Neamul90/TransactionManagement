using FluentValidation;
using TransactionManagement.Application.Abstractions.Services;
using TransactionManagement.Domain.Common;

namespace TransactionManagement.Application.Transactions.Commands.UpdateTransaction;

public sealed class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateTransactionCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;

        RuleFor(command => command.TransactionId)
            .GreaterThan(0)
            .WithMessage("The transaction reference is not valid.");

        RuleFor(command => command.RowVersion)
            .NotEmpty()
            .WithMessage("The concurrency token is missing. Please reload the transaction.");

        RuleFor(command => command.TransactionDate)
            .NotEmpty()
            .WithMessage("Transaction date is required.")
            .Must(BeOnOrBeforeToday)
            .WithMessage("Transaction date cannot be in the future.");

        RuleFor(command => command.BusinessPartnerId)
            .GreaterThan(0)
            .WithMessage("Please select a customer or supplier.");

        RuleFor(command => command.Reference)
            .MaximumLength(DomainConstants.Lengths.Reference);

        RuleFor(command => command.Remarks)
            .MaximumLength(DomainConstants.Lengths.Remarks);

        RuleFor(command => command.Details)
            .NotEmpty()
            .WithMessage("A transaction must contain at least one detail line.");

        RuleFor(command => command.Details)
            .Must(details => details is not null && details.Any(detail => detail.IsActive))
            .WithMessage("A transaction must contain at least one active detail line.")
            .When(command => command.Details is not null && command.Details.Count > 0);

        RuleFor(command => command.Details)
            .Must(HaveUniqueExistingLineIdentifiers)
            .WithMessage("The same detail line was submitted more than once.")
            .When(command => command.Details is not null && command.Details.Count > 0);

        RuleForEach(command => command.Details)
            .SetValidator(new UpdateTransactionDetailCommandValidator());
    }

    private static bool HaveUniqueExistingLineIdentifiers(
        IReadOnlyCollection<UpdateTransactionDetailCommand> details)
    {
        var existingIds = details.Where(detail => detail.Id != 0).Select(detail => detail.Id).ToList();

        return existingIds.Count == existingIds.Distinct().Count();
    }

    private bool BeOnOrBeforeToday(DateTime transactionDate) =>
        transactionDate.Date <= _dateTimeProvider.UtcNow.Date;
}
