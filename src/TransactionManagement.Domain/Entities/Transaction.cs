using TransactionManagement.Domain.Common;
using TransactionManagement.Domain.ValueObjects;

namespace TransactionManagement.Domain.Entities;

/// <summary>
/// Master record of the master–detail model and the aggregate root of the transaction aggregate.
/// <para>
/// The detail collection is exposed read-only; all structural changes go through
/// <see cref="ApplyDetails"/>, <see cref="AddDetail"/>, <see cref="UpdateDetail"/> and
/// <see cref="RemoveDetail"/> so that invariants and totals can never drift.
/// </para>
/// </summary>
public sealed class Transaction : BaseEntity, IAggregateRoot
{
    private readonly List<TransactionDetail> _details = [];

    private Transaction()
    {
        // Required by EF Core materialisation.
        TransactionNumber = null!;
    }

    private Transaction(
        string transactionNumber,
        DateTime transactionDate,
        int businessPartnerId,
        string? reference,
        string? remarks)
    {
        TransactionNumber = transactionNumber;
        TransactionDate = transactionDate.Date;
        BusinessPartnerId = businessPartnerId;
        Reference = reference;
        Remarks = remarks;
    }

    public string TransactionNumber { get; private set; }

    public DateTime TransactionDate { get; private set; }

    public int BusinessPartnerId { get; private set; }

    public BusinessPartner? BusinessPartner { get; private set; }

    public string? Reference { get; private set; }

    public string? Remarks { get; private set; }

    /// <summary>Sum of the quantities of the active detail lines.</summary>
    public decimal TotalQuantity { get; private set; }

    /// <summary>Sum of the amounts of the active detail lines.</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>SQL Server <c>rowversion</c> used for optimistic concurrency on the aggregate.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<TransactionDetail> Details => _details.AsReadOnly();

    public static Transaction Create(
        string transactionNumber,
        DateTime transactionDate,
        int businessPartnerId,
        string? reference,
        string? remarks,
        DateTime referenceDateUtc)
    {
        var normalisedNumber = Guard.AgainstExceedingLength(
            Guard.AgainstNullOrWhiteSpace(transactionNumber, "Transaction number is required."),
            DomainConstants.Lengths.TransactionNumber,
            $"Transaction number cannot exceed {DomainConstants.Lengths.TransactionNumber} characters.");

        var transaction = new Transaction(
            normalisedNumber,
            transactionDate,
            businessPartnerId,
            reference,
            remarks);

        transaction.ValidateHeader(referenceDateUtc);

        return transaction;
    }

    /// <summary>
    /// Updates the master fields. The transaction number is immutable once issued.
    /// </summary>
    public void ChangeHeader(
        DateTime transactionDate,
        int businessPartnerId,
        string? reference,
        string? remarks,
        DateTime referenceDateUtc)
    {
        TransactionDate = transactionDate.Date;
        BusinessPartnerId = businessPartnerId;
        Reference = reference;
        Remarks = remarks;

        ValidateHeader(referenceDateUtc);
    }

    public TransactionDetail AddDetail(
        int productId,
        DateTime detailDate,
        string? description,
        decimal quantity,
        decimal amount,
        bool isActive)
    {
        var detail = TransactionDetail.Create(productId, detailDate, description, quantity, amount, isActive);
        _details.Add(detail);

        RecalculateTotals();

        return detail;
    }

    public void UpdateDetail(
        int detailId,
        int productId,
        DateTime detailDate,
        string? description,
        decimal quantity,
        decimal amount,
        bool isActive)
    {
        var detail = FindOwnedDetail(detailId);

        detail.Update(productId, detailDate, description, quantity, amount, isActive);

        RecalculateTotals();
    }

    public void RemoveDetail(int detailId)
    {
        var detail = FindOwnedDetail(detailId);

        _details.Remove(detail);

        RecalculateTotals();
    }

    /// <summary>
    /// Reconciles the persisted detail collection with the collection submitted by the caller.
    /// <list type="bullet">
    ///   <item><description><c>Id == 0</c> — a new line, appended to the aggregate.</description></item>
    ///   <item><description><c>Id</c> found on this aggregate — updated only when a value actually changed.</description></item>
    ///   <item><description><c>Id</c> present on this aggregate but absent from the submission — removed.</description></item>
    ///   <item><description><c>Id</c> not owned by this aggregate — rejected; client supplied identifiers are never trusted.</description></item>
    /// </list>
    /// </summary>
    public DetailReconciliationResult ApplyDetails(IReadOnlyCollection<TransactionDetailInput> submittedDetails)
    {
        ArgumentNullException.ThrowIfNull(submittedDetails);

        var duplicateId = submittedDetails
            .Where(detail => detail.Id != 0)
            .GroupBy(detail => detail.Id)
            .FirstOrDefault(group => group.Count() > 1);

        Guard.Against(
            duplicateId is not null,
            "The same detail line was submitted more than once.");

        var added = 0;
        var modified = 0;
        var unchanged = 0;

        var submittedExistingIds = new HashSet<int>();

        foreach (var submitted in submittedDetails)
        {
            if (submitted.Id == 0)
            {
                AddDetail(
                    submitted.ProductId,
                    submitted.DetailDate,
                    submitted.Description,
                    submitted.Quantity,
                    submitted.Amount,
                    submitted.IsActive);

                added++;
                continue;
            }

            var existing = FindOwnedDetail(submitted.Id);
            submittedExistingIds.Add(existing.Id);

            var isUnchanged = existing.HasSameValuesAs(
                submitted.ProductId,
                submitted.DetailDate,
                submitted.Description,
                submitted.Quantity,
                submitted.Amount,
                submitted.IsActive);

            if (isUnchanged)
            {
                unchanged++;
                continue;
            }

            existing.Update(
                submitted.ProductId,
                submitted.DetailDate,
                submitted.Description,
                submitted.Quantity,
                submitted.Amount,
                submitted.IsActive);

            modified++;
        }

        var removedDetails = _details
            .Where(detail => detail.Id != 0 && !submittedExistingIds.Contains(detail.Id))
            .ToList();

        foreach (var removed in removedDetails)
        {
            _details.Remove(removed);
        }

        RecalculateTotals();
        EnsureAtLeastOneActiveDetail();

        return new DetailReconciliationResult(added, modified, removedDetails.Count, unchanged);
    }

    /// <summary>
    /// Invariant enforced before a transaction may be persisted.
    /// </summary>
    public void EnsureAtLeastOneActiveDetail() =>
        Guard.Against(
            _details.Count(detail => detail.IsActive) == 0,
            "A transaction must contain at least one active detail line.");

    private TransactionDetail FindOwnedDetail(int detailId)
    {
        var detail = _details.SingleOrDefault(candidate => candidate.Id == detailId);

        Guard.Against(
            detail is null,
            $"Detail line {detailId} does not belong to transaction {TransactionNumber}.");

        return detail!;
    }

    private void ValidateHeader(DateTime referenceDateUtc)
    {
        Guard.Against(TransactionDate == default, "Transaction date is required.");
        Guard.Against(
            TransactionDate > referenceDateUtc.Date,
            "Transaction date cannot be in the future.");
        Guard.AgainstNonPositive(BusinessPartnerId, "A customer or supplier must be selected.");

        if (Reference is not null)
        {
            Reference = Reference.Trim();
            Reference = Reference.Length == 0 ? null : Reference;

            if (Reference is not null)
            {
                Guard.AgainstExceedingLength(
                    Reference,
                    DomainConstants.Lengths.Reference,
                    $"Reference cannot exceed {DomainConstants.Lengths.Reference} characters.");
            }
        }

        if (Remarks is not null)
        {
            Remarks = Remarks.Trim();
            Remarks = Remarks.Length == 0 ? null : Remarks;

            if (Remarks is not null)
            {
                Guard.AgainstExceedingLength(
                    Remarks,
                    DomainConstants.Lengths.Remarks,
                    $"Remarks cannot exceed {DomainConstants.Lengths.Remarks} characters.");
            }
        }
    }

    private void RecalculateTotals()
    {
        TotalQuantity = _details.Where(detail => detail.IsActive).Sum(detail => detail.Quantity);
        TotalAmount = _details.Where(detail => detail.IsActive).Sum(detail => detail.Amount);
    }
}
