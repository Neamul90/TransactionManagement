namespace TransactionManagement.Domain.Exceptions;

/// <summary>
/// Raised when an operation would leave an aggregate in a state that violates a business invariant.
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message)
        : base(message)
    {
    }
}
