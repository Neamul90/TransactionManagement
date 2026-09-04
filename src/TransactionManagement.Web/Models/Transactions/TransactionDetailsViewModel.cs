using TransactionManagement.Application.Dtos.Transactions;

namespace TransactionManagement.Web.Models.Transactions;

/// <summary>
/// Read-only details screen. It wraps the query DTO rather than re-declaring its fields, because
/// nothing on this screen is posted back.
/// </summary>
public sealed class TransactionDetailsViewModel
{
    public required TransactionDetailsDto Transaction { get; init; }

    public string RowVersionToken => Convert.ToBase64String(Transaction.RowVersion);

    public int ActiveDetailCount => Transaction.Details.Count(detail => detail.IsActive);
}
