namespace TransactionManagement.Application.Common.Exceptions;

/// <summary>
/// Raised when the record was changed by someone else between load and save.
/// The user is asked to reload rather than having a colleague's work silently overwritten.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    private const string MessageTemplate =
        "This {0} was changed by another user while you were editing it. "
        + "Please reload the record and apply your changes again.";

    public ConcurrencyConflictException(string entityName)
        : base(string.Format(MessageTemplate, entityName.ToLowerInvariant()))
    {
        EntityName = entityName;
    }

    public ConcurrencyConflictException(string entityName, Exception innerException)
        : base(string.Format(MessageTemplate, entityName.ToLowerInvariant()), innerException)
    {
        EntityName = entityName;
    }

    public string EntityName { get; }
}
