namespace TransactionManagement.Application.Transactions.Queries.GetTransactions;

/// <summary>
/// Whitelist of sortable columns. Sorting is expressed as an enum rather than a free-text column
/// name so that no user supplied string can ever influence the generated SQL.
/// </summary>
public enum TransactionSortField
{
    TransactionDate = 0,
    TransactionNumber = 1,
    BusinessPartnerName = 2,
    TotalAmount = 3
}
