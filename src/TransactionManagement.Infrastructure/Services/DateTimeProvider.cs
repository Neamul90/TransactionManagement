using TransactionManagement.Application.Abstractions.Services;

namespace TransactionManagement.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
