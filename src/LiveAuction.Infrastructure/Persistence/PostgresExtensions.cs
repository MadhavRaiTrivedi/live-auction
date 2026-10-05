namespace LiveAuction.Infrastructure.Persistence;

internal static class PostgresExtensions
{
    public const string Trigram = "pg_trgm";
    public const string TrigramIndexOperators = "gin_trgm_ops";
    public const string GinIndexMethod = "gin";
}
