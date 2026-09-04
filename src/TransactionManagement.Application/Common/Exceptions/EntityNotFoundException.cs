namespace TransactionManagement.Application.Common.Exceptions;

/// <summary>
/// Raised when a requested aggregate or referenced entity does not exist.
/// The message deliberately contains no query, table or connection detail.
/// </summary>
public sealed class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName} with identifier '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }

    public string EntityName { get; }

    public object Key { get; }
}
