namespace TransactionManagement.Domain.Common;

/// <summary>
/// Marker interface identifying the consistency boundary of an aggregate.
/// Only aggregate roots may be loaded, persisted or deleted directly by a repository.
/// </summary>
public interface IAggregateRoot;
