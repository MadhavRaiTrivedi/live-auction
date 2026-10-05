import { Auction, AuctionStatus, AuctionUpdate, Bid, BidKind } from './auction.model';
import { applyOwnBid, applyUpdate, LiveAuctionState } from './live-auction-state';

const auction: Auction = {
  id: 'auction-1',
  sellerId: 'seller-1',
  title: 'Vintage camera',
  description: '',
  status: AuctionStatus.Live,
  startingPriceInPaise: 10_000,
  reservePriceInPaise: null,
  hasReserve: false,
  isReserveMet: true,
  currentPriceInPaise: 10_000,
  minimumNextBidInPaise: 11_000,
  bidCount: 1,
  leadingBidderAlias: 'Bidder AAAAAA',
  startsAt: '2026-01-15T10:00:00Z',
  endsAt: '2026-01-15T11:00:00Z',
  closedAt: null,
  yourAlias: 'Bidder AAAAAA',
  isSeller: false,
  isLeading: true,
  yourMaxBidInPaise: 50_000,
  serverTime: '2026-01-15T10:30:00Z',
};

function bid(sequence: number, amountInPaise: number, bidderAlias = 'Bidder BBBBBB'): Bid {
  return {
    sequence,
    bidderAlias,
    amountInPaise,
    kind: BidKind.Manual,
    placedAt: auction.serverTime,
  };
}

function update(overrides: Partial<AuctionUpdate>): AuctionUpdate {
  return {
    auctionId: auction.id,
    status: AuctionStatus.Live,
    currentPriceInPaise: 12_000,
    minimumNextBidInPaise: 13_000,
    bidCount: 2,
    leadingBidderAlias: 'Bidder BBBBBB',
    isReserveMet: true,
    endsAt: auction.endsAt,
    bids: [bid(2, 12_000)],
    serverTime: auction.serverTime,
    ...overrides,
  };
}

describe('applyUpdate', () => {
  const state: LiveAuctionState = { auction, bids: [bid(1, 10_000, auction.yourAlias)] };

  it('moves the price and marks the user as outbid when someone else leads', () => {
    const next = applyUpdate(state, update({}));

    expect(next.auction.currentPriceInPaise).toBe(12_000);
    expect(next.auction.isLeading).toBe(false);
    expect(next.auction.yourMaxBidInPaise).toBeNull();
    expect(next.bids.map((b) => b.sequence)).toEqual([2, 1]);
  });

  it('ignores an update older than the current state', () => {
    const next = applyUpdate(state, update({ bidCount: 0, currentPriceInPaise: null, bids: [] }));

    expect(next).toBe(state);
  });

  it('does not duplicate bids delivered twice', () => {
    const once = applyUpdate(state, update({}));
    const twice = applyUpdate(once, update({}));

    expect(twice.bids.length).toBe(2);
  });
});

describe('applyOwnBid', () => {
  it('keeps the newer pushed state when the HTTP response arrives late', () => {
    const pushed = applyUpdate(
      { auction, bids: [] },
      update({ bidCount: 3, bids: [bid(3, 13_000)] }),
    );

    const next = applyOwnBid(pushed, { ...auction, bidCount: 2 }, [bid(2, 12_000)]);

    expect(next.auction.bidCount).toBe(3);
    expect(next.bids.map((b) => b.sequence)).toEqual([3, 2]);
  });
});
