using FluentValidation;
using TransactionManagement.Application.Abstractions.Services;
using TransactionManagement.Domain.Common;

namespace TransactionManagement.Application.Transactions.Commands.CreateTransaction;

public sealed class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateTransactionCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;

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

        RuleForEach(command => command.Details)
            .SetValidator(new CreateTransactionDetailCommandValidator());
    }

    private bool BeOnOrBeforeToday(DateTime transactionDate) =>
        transactionDate.Date <= _dateTimeProvider.UtcNow.Date;
}
