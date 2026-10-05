import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  Auction,
  AuctionPage,
  AuctionStatus,
  AuctionTerms,
  Bid,
  PlaceBidResult,
} from './auction.model';

@Injectable({ providedIn: 'root' })
export class AuctionsApi {
  private readonly http = inject(HttpClient);

  list(search: string, status: AuctionStatus | null, page: number): Promise<AuctionPage> {
    let params = new HttpParams().set('page', page);
    if (search) {
      params = params.set('search', search);
    }
    if (status) {
      params = params.set('status', status);
    }
    return firstValueFrom(this.http.get<AuctionPage>('/api/auctions', { params }));
  }

  get(auctionId: string): Promise<Auction> {
    return firstValueFrom(this.http.get<Auction>(`/api/auctions/${auctionId}`));
  }

  create(terms: AuctionTerms): Promise<Auction> {
    return firstValueFrom(this.http.post<Auction>('/api/auctions', terms));
  }

  revise(auctionId: string, terms: AuctionTerms): Promise<Auction> {
    return firstValueFrom(this.http.put<Auction>(`/api/auctions/${auctionId}`, terms));
  }

  cancel(auctionId: string): Promise<Auction> {
    return firstValueFrom(this.http.post<Auction>(`/api/auctions/${auctionId}/cancel`, {}));
  }

  bids(auctionId: string): Promise<Bid[]> {
    return firstValueFrom(this.http.get<Bid[]>(`/api/auctions/${auctionId}/bids`));
  }

  placeBid(auctionId: string, amountInPaise: number): Promise<PlaceBidResult> {
    return firstValueFrom(
      this.http.post<PlaceBidResult>(`/api/auctions/${auctionId}/bids`, { amountInPaise }),
    );
  }

  placeProxyBid(auctionId: string, maxAmountInPaise: number): Promise<PlaceBidResult> {
    return firstValueFrom(
      this.http.post<PlaceBidResult>(`/api/auctions/${auctionId}/proxy-bids`, {
        maxAmountInPaise,
      }),
    );
  }
}
