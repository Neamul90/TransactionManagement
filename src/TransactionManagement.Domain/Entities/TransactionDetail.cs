using TransactionManagement.Domain.Common;

namespace TransactionManagement.Domain.Entities;

/// <summary>
/// A single line of a <see cref="Transaction"/>. It is part of the transaction aggregate and is
/// therefore created, modified and removed exclusively through its parent; every state changing
/// member is <c>internal</c> so that no code outside the domain can bypass aggregate invariants.
/// </summary>
public sealed class TransactionDetail : BaseEntity
{
    private TransactionDetail()
    {
        // Required by EF Core materialisation.
    }

    private TransactionDetail(
        int productId,
        DateTime detailDate,
        string? description,
        decimal quantity,
        decimal amount,
        bool isActive)
    {
        ProductId = productId;
        DetailDate = detailDate;
        Description = description;
        Quantity = quantity;
        Amount = amount;
        IsActive = isActive;
    }

    public int TransactionId { get; private set; }

    public int ProductId { get; private set; }

    public Product? Product { get; private set; }

    public DateTime DetailDate { get; private set; }

    public string? Description { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal Amount { get; private set; }

    /// <summary>
    /// Indicates whether the line currently contributes to the transaction totals.
    /// Inactive lines are retained for reference but excluded from quantity and amount totals.
    /// </summary>
    public bool IsActive { get; private set; }

    internal static TransactionDetail Create(
        int productId,
        DateTime detailDate,
        string? description,
        decimal quantity,
        decimal amount,
        bool isActive)
    {
        var detail = new TransactionDetail(productId, detailDate, description, quantity, amount, isActive);
        detail.Validate();
        return detail;
    }

    internal void Update(
        int productId,
        DateTime detailDate,
        string? description,
        decimal quantity,
        decimal amount,
        bool isActive)
    {
        ProductId = productId;
        DetailDate = detailDate;
        Description = description;
        Quantity = quantity;
        Amount = amount;
        IsActive = isActive;

        Validate();
    }

    /// <summary>
    /// Value comparison used by the update workflow to tell modified lines from unchanged ones,
    /// so that unchanged rows are never marked dirty and never generate an UPDATE statement.
    /// </summary>
    internal bool HasSameValuesAs(
        int productId,
        DateTime detailDate,
        string? description,
        decimal quantity,
        decimal amount,
        bool isActive) =>
        ProductId == productId
        && DetailDate.Date == detailDate.Date
        && string.Equals(NormaliseDescription(Description), NormaliseDescription(description), StringComparison.Ordinal)
        && Quantity == quantity
        && Amount == amount
        && IsActive == isActive;

    private void Validate()
    {
        Guard.AgainstNonPositive(ProductId, "A product must be selected for every detail line.");
        Guard.AgainstNonPositive(Quantity, "Quantity must be greater than zero.");
        Guard.AgainstNegative(Amount, "Amount cannot be negative.");
        Guard.Against(DetailDate == default, "Detail date is required.");

        if (Description is not null)
        {
            Description = Description.Trim();

            if (Description.Length == 0)
            {
                Description = null;
            }
            else
            {
                Guard.AgainstExceedingLength(
                    Description,
                    DomainConstants.Lengths.Description,
                    $"Description cannot exceed {DomainConstants.Lengths.Description} characters.");
            }
        }
    }

    private static string NormaliseDescription(string? value) => value?.Trim() ?? string.Empty;
}
