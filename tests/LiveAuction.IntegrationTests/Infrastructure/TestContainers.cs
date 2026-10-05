using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace LiveAuction.IntegrationTests.Infrastructure;

// Started once per test run and shared by every test class; each test works on its own auctions.
internal static class TestContainers
{
    private static readonly Lazy<Task> Startup = new(StartAsync);

    public static PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public static RedisContainer Redis { get; } = new RedisBuilder("redis:8.2-alpine").Build();

    public static Task EnsureStartedAsync() => Startup.Value;

    private static Task StartAsync() => Task.WhenAll(Postgres.StartAsync(), Redis.StartAsync());
}
