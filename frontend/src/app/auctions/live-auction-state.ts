import { Auction, AuctionUpdate, Bid } from './auction.model';

const MAX_VISIBLE_BIDS = 50;

export interface LiveAuctionState {
  auction: Auction;
  bids: Bid[];
}

// Updates can arrive out of order with the HTTP response of the user's own bid, or twice after a
// reconnect. The bid count only grows, so anything with a lower count is stale and ignored.
export function applyUpdate(state: LiveAuctionState, update: AuctionUpdate): LiveAuctionState {
  if (update.auctionId !== state.auction.id || update.bidCount < state.auction.bidCount) {
    return state;
  }

  const isLeading = update.leadingBidderAlias === state.auction.yourAlias;
  return {
    auction: {
      ...state.auction,
      status: update.status,
      currentPriceInPaise: update.currentPriceInPaise,
      minimumNextBidInPaise: update.minimumNextBidInPaise,
      bidCount: update.bidCount,
      leadingBidderAlias: update.leadingBidderAlias,
      isReserveMet: update.isReserveMet,
      endsAt: update.endsAt,
      isLeading,
      yourMaxBidInPaise: isLeading ? state.auction.yourMaxBidInPaise : null,
    },
    bids: mergeBids(state.bids, update.bids),
  };
}

export function applyOwnBid(
  state: LiveAuctionState,
  auction: Auction,
  placed: Bid[],
): LiveAuctionState {
  if (auction.bidCount < state.auction.bidCount) {
    return { ...state, bids: mergeBids(state.bids, placed) };
  }
  return { auction, bids: mergeBids(state.bids, placed) };
}

function mergeBids(current: Bid[], incoming: Bid[]): Bid[] {
  const bySequence = new Map(current.map((bid) => [bid.sequence, bid]));
  incoming.forEach((bid) => bySequence.set(bid.sequence, bid));
  return [...bySequence.values()]
    .sort((left, right) => right.sequence - left.sequence)
    .slice(0, MAX_VISIBLE_BIDS);
}
