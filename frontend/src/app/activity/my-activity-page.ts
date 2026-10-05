import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { describeApiError } from '../shared/api-error';
import { PaisePipe } from '../shared/paise.pipe';
import { BidderAuction, SellerAuction } from './activity.model';
import { ActivityApi } from './activity.api';

@Component({
  selector: 'app-my-activity-page',
  imports: [RouterLink, DatePipe, PaisePipe],
  templateUrl: './my-activity-page.html',
})
export class MyActivityPage implements OnInit {
  private readonly activityApi = inject(ActivityApi);

  protected readonly bids = signal<BidderAuction[]>([]);
  protected readonly sales = signal<SellerAuction[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      const [bids, sales] = await Promise.all([
        this.activityApi.myBids(),
        this.activityApi.myAuctions(),
      ]);
      this.bids.set(bids);
      this.sales.set(sales);
    } catch (error) {
      this.error.set(describeApiError(error));
    } finally {
      this.isLoading.set(false);
    }
  }
}
