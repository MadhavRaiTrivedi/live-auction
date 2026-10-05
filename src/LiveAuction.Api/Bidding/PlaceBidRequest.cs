using LiveAuction.Api.Http;

namespace LiveAuction.Api.Bidding;

public sealed record PlaceBidRequest([PositiveAmount] long AmountInPaise);
