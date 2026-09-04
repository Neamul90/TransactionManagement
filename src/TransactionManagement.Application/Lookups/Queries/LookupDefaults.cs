namespace TransactionManagement.Application.Lookups.Queries;

public static class LookupDefaults
{
    /// <summary>Rows returned by a type-ahead lookup when the caller does not say otherwise.</summary>
    public const int DefaultMaxResults = 20;

    /// <summary>Hard ceiling, so no caller can turn a type-ahead into a full table scan.</summary>
    public const int MaxAllowedResults = 50;

    public static int Clamp(int maxResults) =>
        maxResults <= 0 ? DefaultMaxResults : Math.Min(maxResults, MaxAllowedResults);
}
