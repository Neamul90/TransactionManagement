using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransactionManagement.Application.Common.Models;
using TransactionManagement.Application.Dtos.Transactions;
using TransactionManagement.Application.Transactions.Queries.GetTransactions;

namespace TransactionManagement.Web.Models.Transactions;

/// <summary>
/// List screen state: the current page of results plus the search and sort settings needed to
/// rebuild every link on the page.
/// </summary>
public sealed class TransactionListViewModel
{
    [BindNever]
    public PagedResult<TransactionListItemDto> Page { get; init; } =
        PagedResult<TransactionListItemDto>.Empty(1, PaginationDefaults.DefaultPageSize);

    public string? SearchTerm { get; init; }

    public TransactionSortField SortBy { get; init; } = TransactionSortField.TransactionDate;

    public bool SortDescending { get; init; } = true;

    /// <summary>Toggles direction when the user clicks the column that is already sorted.</summary>
    public bool NextSortDescendingFor(TransactionSortField field) =>
        SortBy == field ? !SortDescending : true;

    public string SortIndicatorFor(TransactionSortField field) =>
        SortBy != field ? string.Empty : SortDescending ? "▼" : "▲";
}
