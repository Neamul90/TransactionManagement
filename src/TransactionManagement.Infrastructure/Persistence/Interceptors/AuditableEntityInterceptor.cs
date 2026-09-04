using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TransactionManagement.Application.Abstractions.Services;
using TransactionManagement.Domain.Common;

namespace TransactionManagement.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Maintains <see cref="BaseEntity.CreatedAtUtc"/> and <see cref="BaseEntity.ModifiedAtUtc"/> in one
/// place, so no handler or repository has to remember to stamp them.
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditableEntityInterceptor(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditStamps(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAuditStamps(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private void ApplyAuditStamps(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var utcNow = _dateTimeProvider.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(BaseEntity.CreatedAtUtc)).CurrentValue = utcNow;
                    break;

                case EntityState.Modified:
                    entry.Property(nameof(BaseEntity.ModifiedAtUtc)).CurrentValue = utcNow;
                    break;
            }
        }
    }
}
