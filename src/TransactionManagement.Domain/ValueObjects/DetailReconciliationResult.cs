namespace TransactionManagement.Domain.ValueObjects;

/// <summary>
/// Outcome of reconciling a submitted detail collection against the persisted one.
/// Returned so that the application layer can log precisely what a save actually changed.
/// </summary>
public sealed record DetailReconciliationResult(
    int AddedCount,
    int ModifiedCount,
    int RemovedCount,
    int UnchangedCount)
{
    public bool HasChanges => AddedCount > 0 || ModifiedCount > 0 || RemovedCount > 0;

    public override string ToString() =>
        $"added={AddedCount}, modified={ModifiedCount}, removed={RemovedCount}, unchanged={UnchangedCount}";
}
