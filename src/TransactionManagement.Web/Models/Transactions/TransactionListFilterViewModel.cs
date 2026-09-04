using TransactionManagement.Application.Common.Models;
using TransactionManagement.Application.Transactions.Queries.GetTransactions;

namespace TransactionManagement.Web.Models.Transactions;

/// <summary>
/// Bindable list criteria.
/// <para>
/// The Application layer's <see cref="GetTransactionsQuery"/> is an immutable record, which is the
/// right shape for a query but the wrong shape for MVC model binding. Binding this class and
/// translating it keeps the query immutable and keeps the Web layer from binding an Application
/// type straight off the wire.
/// </para>
/// </summary>
public sealed class TransactionListFilterViewModel
{
    public int PageNumber { get; set; } = PaginationDefaults.FirstPageNumber;

    public int PageSize { get; set; } = PaginationDefaults.DefaultPageSize;

    public string? SearchTerm { get; set; }

    public TransactionSortField SortBy { get; set; } = TransactionSortField.TransactionDate;

    public bool SortDescending { get; set; } = true;

    public GetTransactionsQuery ToQuery() =>
        new(PageNumber, PageSize, SearchTerm, SortBy, SortDescending);
}
