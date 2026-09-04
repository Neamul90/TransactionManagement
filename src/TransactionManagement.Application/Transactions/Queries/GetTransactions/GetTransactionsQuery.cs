using MediatR;
using TransactionManagement.Application.Common.Models;
using TransactionManagement.Application.Dtos.Transactions;

namespace TransactionManagement.Application.Transactions.Queries.GetTransactions;

/// <summary>
/// Server-side paginated, searchable and sortable transaction list.
/// </summary>
public sealed record GetTransactionsQuery(
    int PageNumber = PaginationDefaults.FirstPageNumber,
    int PageSize = PaginationDefaults.DefaultPageSize,
    string? SearchTerm = null,
    TransactionSortField SortBy = TransactionSortField.TransactionDate,
    bool SortDescending = true)
    : IRequest<PagedResult<TransactionListItemDto>>;
