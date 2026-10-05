using LiveAuction.Application.Persistence;
using LiveAuction.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace LiveAuction.Infrastructure.Persistence;

internal sealed class UnitOfWork(AuctionDbContext db) : IUnitOfWork
{
    public async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new EfDatabaseTransaction(await db.Database.BeginTransactionAsync(cancellationToken));

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(exception);
        }
        catch (DbUpdateException exception) when (IsBidSequenceTaken(exception))
        {
            throw new ConcurrencyConflictException(exception);
        }
    }

    public void DiscardChanges() => db.ChangeTracker.Clear();

    // EF may insert the new bid before it updates the auction row, so two concurrent bids can
    // collide on the unique sequence index before the row version check runs. Same meaning: another
    // bid got there first, so the caller should reload and try again.
    private static bool IsBidSequenceTaken(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: BidConfiguration.SequenceIndexName,
        };

    private sealed class EfDatabaseTransaction(IDbContextTransaction transaction) : IDatabaseTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
