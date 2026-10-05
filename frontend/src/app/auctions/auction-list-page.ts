import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { describeApiError } from '../shared/api-error';
import { PaisePipe } from '../shared/paise.pipe';
import { AuctionStatus, AuctionSummary } from './auction.model';
import { AuctionsApi } from './auctions.api';

interface StatusTab {
  label: string;
  status: AuctionStatus | null;
}

@Component({
  selector: 'app-auction-list-page',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, PaisePipe],
  templateUrl: './auction-list-page.html',
})
export class AuctionListPage implements OnInit {
  private readonly auctionsApi = inject(AuctionsApi);

  protected readonly AuctionStatus = AuctionStatus;

  protected readonly tabs: StatusTab[] = [
    { label: 'Live and upcoming', status: null },
    { label: 'Live', status: AuctionStatus.Live },
    { label: 'Upcoming', status: AuctionStatus.Scheduled },
    { label: 'Sold', status: AuctionStatus.Sold },
  ];

  protected readonly auctions = signal<AuctionSummary[]>([]);
  protected readonly activeStatus = signal<AuctionStatus | null>(null);
  protected readonly page = signal(1);
  protected readonly hasMore = signal(false);
  protected readonly isLoading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly searchForm = inject(FormBuilder).nonNullable.group({ search: '' });

  async ngOnInit(): Promise<void> {
    await this.load(1);
  }

  protected async selectStatus(status: AuctionStatus | null): Promise<void> {
    this.activeStatus.set(status);
    await this.load(1);
  }

  protected async search(): Promise<void> {
    await this.load(1);
  }

  protected async loadMore(): Promise<void> {
    await this.load(this.page() + 1);
  }

  private async load(page: number): Promise<void> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const result = await this.auctionsApi.list(
        this.searchForm.getRawValue().search.trim(),
        this.activeStatus(),
        page,
      );
      this.auctions.update((current) =>
        page === 1 ? result.items : [...current, ...result.items],
      );
      this.page.set(result.page);
      this.hasMore.set(result.hasMore);
    } catch (error) {
      this.error.set(describeApiError(error));
    } finally {
      this.isLoading.set(false);
    }
  }
}
