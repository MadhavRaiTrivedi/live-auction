namespace LiveAuction.IntegrationTests.Infrastructure;

internal sealed record ProblemResponse(int Status, string Title, string? Detail, string? Code);
