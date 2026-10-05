using System.ComponentModel.DataAnnotations;
using LiveAuction.Api.Http;
using LiveAuction.Domain.Auctions;

namespace LiveAuction.Api.Auctions;

public sealed record AuctionTermsRequest(
    [Required, StringLength(AuctionTerms.MaxTitleLength)] string Title,
    [StringLength(AuctionTerms.MaxDescriptionLength)] string? Description,
    [PositiveAmount] long StartingPriceInPaise,
    [PositiveAmount] long? ReservePriceInPaise,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt)
{
    // PostgreSQL timestamptz only accepts UTC values from Npgsql, and clients may send any offset.
    public AuctionTerms ToTerms() =>
        new(
            Title.Trim(),
            Description?.Trim() ?? string.Empty,
            StartingPriceInPaise,
            ReservePriceInPaise,
            StartsAt.ToUniversalTime(),
            EndsAt.ToUniversalTime());
}
