export enum AuctionStatus {
  Scheduled = 'Scheduled',
  Live = 'Live',
  Sold = 'Sold',
  Unsold = 'Unsold',
  Cancelled = 'Cancelled',
}

export enum BidKind {
  Manual = 'Manual',
  Proxy = 'Proxy',
  Auto = 'Auto',
}

export interface Auction {
  id: string;
  sellerId: string;
  title: string;
  description: string;
  status: AuctionStatus;
  startingPriceInPaise: number;
  reservePriceInPaise: number | null;
  hasReserve: boolean;
  isReserveMet: boolean;
  currentPriceInPaise: number | null;
  minimumNextBidInPaise: number;
  bidCount: number;
  leadingBidderAlias: string | null;
  startsAt: string;
  endsAt: string;
  closedAt: string | null;
  yourAlias: string;
  isSeller: boolean;
  isLeading: boolean;
  yourMaxBidInPaise: number | null;
  serverTime: string;
}

export interface AuctionSummary {
  id: string;
  title: string;
  status: AuctionStatus;
  priceInPaise: number;
  bidCount: number;
  startsAt: string;
  endsAt: string;
}

export interface AuctionPage {
  items: AuctionSummary[];
  page: number;
  hasMore: boolean;
}

export interface AuctionTerms {
  title: string;
  description: string;
  startingPriceInPaise: number;
  reservePriceInPaise: number | null;
  startsAt: string;
  endsAt: string;
}

export interface Bid {
  sequence: number;
  bidderAlias: string;
  amountInPaise: number;
  kind: BidKind;
  placedAt: string;
}

export interface PlaceBidResult {
  isLeading: boolean;
  placedBids: Bid[];
  auction: Auction;
}

export interface AuctionUpdate {
  auctionId: string;
  status: AuctionStatus;
  currentPriceInPaise: number | null;
  minimumNextBidInPaise: number;
  bidCount: number;
  leadingBidderAlias: string | null;
  isReserveMet: boolean;
  endsAt: string;
  bids: Bid[];
  serverTime: string;
}

export interface OutbidNotice {
  auctionId: string;
  title: string;
  currentPriceInPaise: number;
  minimumNextBidInPaise: number;
}
