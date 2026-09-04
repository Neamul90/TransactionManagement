using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Web.Models.Transactions;

/// <summary>
/// Model for the shared master–detail grid partial, so the create and edit screens render
/// exactly the same grid and the row markup exists in exactly one file.
/// </summary>
public sealed class DetailGridViewModel
{
    public required IReadOnlyList<TransactionDetailRowViewModel> Details { get; init; }

    public required IReadOnlyCollection<ProductLookupDto> Products { get; init; }
}
