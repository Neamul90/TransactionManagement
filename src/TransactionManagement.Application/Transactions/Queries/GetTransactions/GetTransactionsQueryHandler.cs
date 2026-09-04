using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Persistence;
using TransactionManagement.Application.Common.Models;
using TransactionManagement.Application.Dtos.Transactions;
using TransactionManagement.Domain.Entities;

namespace TransactionManagement.Application.Transactions.Queries.GetTransactions;

/// <summary>
/// Read-side handler. It never materialises entities: filtering, ordering, counting and paging all
/// execute on the database and the result set is projected straight into the list DTO.
/// </summary>
public sealed class GetTransactionsQueryHandler
    : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionListItemDto>>
{
    private readonly ITransactionRepository _transactionRepository;

    public GetTransactionsQueryHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<PagedResult<TransactionListItemDto>> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(request.PageNumber, PaginationDefaults.FirstPageNumber);

        var pageSize = Math.Clamp(
            request.PageSize <= 0 ? PaginationDefaults.DefaultPageSize : request.PageSize,
            PaginationDefaults.MinPageSize,
            PaginationDefaults.MaxPageSize);

        var query = _transactionRepository
            .Query()
            .AsNoTracking();

        query = ApplySearch(query, request.SearchTerm);

        var totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PagedResult<TransactionListItemDto>.Empty(pageNumber, pageSize);
        }

        // Guard against a page number beyond the end of the result set after a search narrows it.
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        pageNumber = Math.Min(pageNumber, totalPages);

        var items = await ApplySort(query, request.SortBy, request.SortDescending)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(transaction => new TransactionListItemDto(
                transaction.Id,
                transaction.TransactionNumber,
                transaction.TransactionDate,
                transaction.BusinessPartner!.Name,
                transaction.BusinessPartner.PartnerType,
                transaction.Details.Count,
                transaction.TotalQuantity,
                transaction.TotalAmount))
            .ToListAsync(cancellationToken);

        return PagedResult<TransactionListItemDto>.Create(items, pageNumber, pageSize, totalCount);
    }

    private static IQueryable<Transaction> ApplySearch(IQueryable<Transaction> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        var term = searchTerm.Trim();

        return query.Where(transaction =>
            EF.Functions.Like(transaction.TransactionNumber, $"%{term}%")
            || EF.Functions.Like(transaction.BusinessPartner!.Name, $"%{term}%")
            || EF.Functions.Like(transaction.BusinessPartner!.Code, $"%{term}%")
            || (transaction.Reference != null && EF.Functions.Like(transaction.Reference, $"%{term}%")));
    }

    private static IQueryable<Transaction> ApplySort(
        IQueryable<Transaction> query,
        TransactionSortField sortBy,
        bool sortDescending) =>
        (sortBy, sortDescending) switch
        {
            (TransactionSortField.TransactionNumber, false) => query.OrderBy(t => t.TransactionNumber),
            (TransactionSortField.TransactionNumber, true) => query.OrderByDescending(t => t.TransactionNumber),
            (TransactionSortField.BusinessPartnerName, false) => query.OrderBy(t => t.BusinessPartner!.Name).ThenBy(t => t.Id),
            (TransactionSortField.BusinessPartnerName, true) => query.OrderByDescending(t => t.BusinessPartner!.Name).ThenBy(t => t.Id),
            (TransactionSortField.TotalAmount, false) => query.OrderBy(t => t.TotalAmount).ThenBy(t => t.Id),
            (TransactionSortField.TotalAmount, true) => query.OrderByDescending(t => t.TotalAmount).ThenBy(t => t.Id),
            (_, false) => query.OrderBy(t => t.TransactionDate).ThenBy(t => t.Id),
            (_, true) => query.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
        };
}
