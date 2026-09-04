using FluentValidation;

namespace TransactionManagement.Application.Transactions.Queries.GetTransactionById;

public sealed class GetTransactionByIdQueryValidator : AbstractValidator<GetTransactionByIdQuery>
{
    public GetTransactionByIdQueryValidator()
    {
        RuleFor(query => query.TransactionId)
            .GreaterThan(0)
            .WithMessage("The transaction reference is not valid.");
    }
}
