namespace LiveAuction.Application.Persistence;

public sealed class ConcurrencyConflictException(Exception innerException)
    : Exception("The data changed while the request was being processed.", innerException);
