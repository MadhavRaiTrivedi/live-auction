using LiveAuction.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
    }

    public void DiscardChanges() => db.ChangeTracker.Clear();

    private sealed class EfDatabaseTransaction(IDbContextTransaction transaction) : IDatabaseTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
