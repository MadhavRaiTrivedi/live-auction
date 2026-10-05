namespace LiveAuction.Application.Persistence;

public interface IDatabaseTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
