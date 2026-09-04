namespace TransactionManagement.Application.Common.Models;

/// <summary>
/// Result of a server-side paginated query. <see cref="TotalCount"/> comes from a COUNT executed
/// on the database, never from materialising the whole table.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int PageNumber { get; init; } = PaginationDefaults.FirstPageNumber;

    public int PageSize { get; init; } = PaginationDefaults.DefaultPageSize;

    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => PageNumber > PaginationDefaults.FirstPageNumber;

    public bool HasNextPage => PageNumber < TotalPages;

    public int FirstItemOnPage => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;

    public int LastItemOnPage => Math.Min(PageNumber * PageSize, TotalCount);

    public static PagedResult<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount) =>
        new()
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };

    public static PagedResult<T> Empty(int pageNumber, int pageSize) =>
        Create([], pageNumber, pageSize, 0);
}
