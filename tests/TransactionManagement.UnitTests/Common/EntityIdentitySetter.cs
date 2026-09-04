using System.Reflection;
using TransactionManagement.Domain.Common;

namespace TransactionManagement.UnitTests.Common;

/// <summary>
/// Assigns identifiers to entities in tests. Production code never does this — identifiers come
/// from the database — but unit tests need to simulate an aggregate that has already been saved.
/// </summary>
internal static class EntityIdentitySetter
{
    private static readonly PropertyInfo IdProperty =
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id), BindingFlags.Public | BindingFlags.Instance)
        ?? throw new InvalidOperationException("BaseEntity.Id was not found.");

    internal static TEntity WithId<TEntity>(this TEntity entity, int id)
        where TEntity : BaseEntity
    {
        IdProperty.SetValue(entity, id);

        return entity;
    }
}
