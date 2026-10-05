using LiveAuction.Api.Http;

namespace LiveAuction.Api.Bidding;

public sealed record PlaceProxyBidRequest([PositiveAmount] long MaxAmountInPaise);
