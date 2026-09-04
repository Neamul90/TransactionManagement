namespace TransactionManagement.Domain.Exceptions;

/// <summary>
/// Base type for every exception raised by the domain model.
/// Messages carried by domain exceptions are safe to show to end users.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
