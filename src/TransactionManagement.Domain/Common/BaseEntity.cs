namespace TransactionManagement.Domain.Common;

/// <summary>
/// Base type for persistent domain entities. Deliberately minimal: identity plus
/// creation/modification stamps that the persistence layer maintains through an interceptor.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; protected set; }

    public DateTime CreatedAtUtc { get; protected set; }

    public DateTime? ModifiedAtUtc { get; protected set; }

    public bool IsTransient => Id == 0;
}
