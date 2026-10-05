using System.Net;
using System.Net.Http.Json;
using LiveAuction.Application.Auctions;
using LiveAuction.Domain;
using LiveAuction.Domain.Auctions;
using LiveAuction.IntegrationTests.Infrastructure;

namespace LiveAuction.IntegrationTests.Auctions;

public class AuctionManagementTests(AuctionApiFactory factory) : IClassFixture<AuctionApiFactory>
{
    [Fact]
    public async Task Create_WithReserve_ShowsReserveOnlyToSeller()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var bidder = await AuctionClient.UserAsync(factory);

        var created = await AuctionClient.ReadAsync<AuctionResponse>(
            await seller.CreateAuctionAsync(startingPriceInPaise: 10_000, reservePriceInPaise: 50_000));

        created.Status.ShouldBe(AuctionStatus.Scheduled);
        created.ReservePriceInPaise.ShouldBe(50_000);
        var seenByBidder = await bidder.GetAuctionAsync(created.Id);
        seenByBidder.ReservePriceInPaise.ShouldBeNull();
        seenByBidder.HasReserve.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_WithIndianTimeOffset_StoresTheSameInstant()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var startsAt = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(5.5));

        var response = await seller.Http.PostAsJsonAsync(
            "/api/auctions",
            new { title = "Sitar", description = "", startingPriceInPaise = 10_000, startsAt, endsAt = startsAt.AddHours(1) },
            AuctionClient.JsonOptions,
            TestContext.Current.CancellationToken);

        (await AuctionClient.ReadAsync<AuctionResponse>(response)).StartsAt.ShouldBe(startsAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Create_WithMissingTitle_ReturnsValidationProblem()
    {
        var seller = await AuctionClient.UserAsync(factory);

        var response = await seller.Http.PostAsJsonAsync(
            "/api/auctions",
            new { startingPriceInPaise = 10_000, startsAt = DateTimeOffset.UtcNow, endsAt = DateTimeOffset.UtcNow.AddHours(1) },
            AuctionClient.JsonOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_FindsLiveAuctionsByTitleFragment()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var title = $"Brass telescope {Guid.NewGuid():N}";
        var startsAt = DateTimeOffset.UtcNow;
        await seller.Http.PostAsJsonAsync(
            "/api/auctions",
            new { title, description = "", startingPriceInPaise = 10_000, startsAt, endsAt = startsAt.AddHours(1) },
            AuctionClient.JsonOptions,
            TestContext.Current.CancellationToken);

        var page = await seller.GetAsync<AuctionPage>($"/api/auctions?search={Uri.EscapeDataString(title[6..20])}");

        page.Items.ShouldContain(auction => auction.Title == title);
    }

    [Fact]
    public async Task CancelBySeller_WithBids_IsRejectedButAdminCanCancel()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var bidder = await AuctionClient.UserAsync(factory);
        var admin = await AuctionClient.AdminAsync(factory);
        var auction = await LiveAuctions.OpenAsync(factory, seller);
        await bidder.PlaceBidAsync(auction.Id, auction.MinimumNextBidInPaise);

        var bySeller = await seller.CancelAsync(auction.Id);
        var byAdmin = await admin.CancelAsync(auction.Id);

        bySeller.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await AuctionClient.ReadProblemAsync(bySeller)).Code.ShouldBe(nameof(DomainErrorCode.AuctionHasBids));
        (await AuctionClient.ReadAsync<AuctionResponse>(byAdmin)).Status.ShouldBe(AuctionStatus.Cancelled);
    }

    [Fact]
    public async Task Revise_ByAnotherUser_IsForbidden()
    {
        var seller = await AuctionClient.UserAsync(factory);
        var stranger = await AuctionClient.UserAsync(factory);
        var created = await AuctionClient.ReadAsync<AuctionResponse>(await seller.CreateAuctionAsync());

        var response = await stranger.Http.PutAsJsonAsync(
            $"/api/auctions/{created.Id}",
            new { title = "Mine now", description = "", startingPriceInPaise = 1, created.StartsAt, created.EndsAt },
            AuctionClient.JsonOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
