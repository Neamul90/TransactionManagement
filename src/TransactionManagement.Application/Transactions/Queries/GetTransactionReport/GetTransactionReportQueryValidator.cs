using FluentValidation;

namespace TransactionManagement.Application.Transactions.Queries.GetTransactionReport;

public sealed class GetTransactionReportQueryValidator : AbstractValidator<GetTransactionReportQuery>
{
    public GetTransactionReportQueryValidator()
    {
        RuleFor(query => query.TransactionId)
            .GreaterThan(0)
            .WithMessage("The transaction reference is not valid.");
    }
}
