namespace LiveAuction.Application.Persistence;

public interface IUnitOfWork
{
    Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    // Throws ConcurrencyConflictException when a row changed since it was read.
    Task SaveChangesAsync(CancellationToken cancellationToken);

    void DiscardChanges();
}
