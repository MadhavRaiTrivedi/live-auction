using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LiveAuction.Api.Security;
using LiveAuction.Application.Auctions;
using LiveAuction.Application.Bidding;

namespace LiveAuction.IntegrationTests.Infrastructure;

internal sealed class AuctionClient(HttpClient http, Guid userId, string accessToken)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public Guid UserId { get; } = userId;

    public string AccessToken { get; } = accessToken;

    public HttpClient Http { get; } = http;

    public static Task<AuctionClient> UserAsync(AuctionApiFactory factory, Guid? userId = null) =>
        CreateAsync(factory, userId ?? Guid.NewGuid(), UserRole.User);

    public static Task<AuctionClient> AdminAsync(AuctionApiFactory factory) =>
        CreateAsync(factory, Guid.NewGuid(), UserRole.Admin);

    public Task<HttpResponseMessage> CreateAuctionAsync(
        long startingPriceInPaise = 10_000,
        long? reservePriceInPaise = null,
        TimeSpan? duration = null,
        TimeSpan? startsIn = null)
    {
        var startsAt = DateTimeOffset.UtcNow + (startsIn ?? TimeSpan.Zero);
        return Http.PostAsJsonAsync(
            "/api/auctions",
            new
            {
                title = "Vintage camera",
                description = "Working condition, original lens.",
                startingPriceInPaise,
                reservePriceInPaise,
                startsAt,
                endsAt = startsAt + (duration ?? TimeSpan.FromHours(1)),
            },
            JsonOptions,
            TestContext.Current.CancellationToken);
    }

    public Task<HttpResponseMessage> BidAsync(Guid auctionId, long amountInPaise) =>
        Http.PostAsJsonAsync($"/api/auctions/{auctionId}/bids", new { amountInPaise }, JsonOptions);

    public async Task<PlaceBidResponse> PlaceBidAsync(Guid auctionId, long amountInPaise) =>
        await ReadAsync<PlaceBidResponse>(await BidAsync(auctionId, amountInPaise));

    public Task<HttpResponseMessage> ProxyBidAsync(Guid auctionId, long maxAmountInPaise) =>
        Http.PostAsJsonAsync($"/api/auctions/{auctionId}/proxy-bids", new { maxAmountInPaise }, JsonOptions);

    public Task<HttpResponseMessage> CancelAsync(Guid auctionId) =>
        Http.PostAsync($"/api/auctions/{auctionId}/cancel", null, TestContext.Current.CancellationToken);

    public Task<AuctionResponse> GetAuctionAsync(Guid auctionId) => GetAsync<AuctionResponse>($"/api/auctions/{auctionId}");

    public Task<List<BidResponse>> GetBidsAsync(Guid auctionId) => GetAsync<List<BidResponse>>($"/api/auctions/{auctionId}/bids");

    public async Task<T> GetAsync<T>(string path) =>
        (await Http.GetFromJsonAsync<T>(path, JsonOptions, TestContext.Current.CancellationToken))!;

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"{(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    public static async Task<ProblemResponse> ReadProblemAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions, TestContext.Current.CancellationToken))!;

    private static async Task<AuctionClient> CreateAsync(AuctionApiFactory factory, Guid userId, UserRole role)
    {
        var http = factory.CreateClient();
        var tokenResponse = await http.PostAsJsonAsync("/api/auth/dev-token", new { userId, role }, JsonOptions);
        var token = await ReadAsync<DevTokenResponse>(tokenResponse);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return new AuctionClient(http, userId, token.AccessToken);
    }
}
