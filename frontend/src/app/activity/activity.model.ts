import { AuctionStatus } from '../auctions/auction.model';

export enum BidStanding {
  Leading = 'Leading',
  Outbid = 'Outbid',
  Won = 'Won',
  Lost = 'Lost',
  Cancelled = 'Cancelled',
}

export interface BidderAuction {
  auctionId: string;
  title: string;
  status: AuctionStatus;
  currentPriceInPaise: number | null;
  yourHighestBidInPaise: number;
  endsAt: string;
  standing: BidStanding;
}

export interface SellerAuction {
  id: string;
  title: string;
  status: AuctionStatus;
  startingPriceInPaise: number;
  reservePriceInPaise: number | null;
  currentPriceInPaise: number | null;
  bidCount: number;
  startsAt: string;
  endsAt: string;
  winnerId: string | null;
}
