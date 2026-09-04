using FluentValidation;
using TransactionManagement.Domain.Common;

namespace TransactionManagement.Application.Transactions.Commands.CreateTransaction;

public sealed class CreateTransactionDetailCommandValidator
    : AbstractValidator<CreateTransactionDetailCommand>
{
    public CreateTransactionDetailCommandValidator()
    {
        RuleFor(detail => detail.ProductId)
            .GreaterThan(0)
            .WithMessage("Please select a product for every detail line.");

        RuleFor(detail => detail.DetailDate)
            .NotEmpty()
            .WithMessage("Please supply a date for every detail line.");

        RuleFor(detail => detail.Description)
            .MaximumLength(DomainConstants.Lengths.Description)
            .WithMessage($"Description cannot exceed {DomainConstants.Lengths.Description} characters.");

        RuleFor(detail => detail.Quantity)
            .GreaterThan(0m)
            .WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(9_999_999m)
            .WithMessage("Quantity is unrealistically large.");

        RuleFor(detail => detail.Amount)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Amount cannot be negative.")
            .LessThanOrEqualTo(999_999_999_999m)
            .WithMessage("Amount is unrealistically large.");
    }
}
