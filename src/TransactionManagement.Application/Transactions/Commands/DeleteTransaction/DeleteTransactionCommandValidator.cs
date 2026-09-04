using FluentValidation;

namespace TransactionManagement.Application.Transactions.Commands.DeleteTransaction;

public sealed class DeleteTransactionCommandValidator : AbstractValidator<DeleteTransactionCommand>
{
    public DeleteTransactionCommandValidator()
    {
        RuleFor(command => command.TransactionId)
            .GreaterThan(0)
            .WithMessage("The transaction reference is not valid.");

        RuleFor(command => command.RowVersion)
            .NotEmpty()
            .WithMessage("The concurrency token is missing. Please reload the transaction list.");
    }
}
