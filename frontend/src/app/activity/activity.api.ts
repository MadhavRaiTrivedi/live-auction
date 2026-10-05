import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { BidderAuction, SellerAuction } from './activity.model';

@Injectable({ providedIn: 'root' })
export class ActivityApi {
  private readonly http = inject(HttpClient);

  myBids(): Promise<BidderAuction[]> {
    return firstValueFrom(this.http.get<BidderAuction[]>('/api/me/bids'));
  }

  myAuctions(): Promise<SellerAuction[]> {
    return firstValueFrom(this.http.get<SellerAuction[]>('/api/me/auctions'));
  }
}
