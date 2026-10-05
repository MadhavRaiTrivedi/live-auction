import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, inject, input, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Session } from '../auth/session';
import { AuctionHub } from '../realtime/auction-hub';
import { describeApiError } from '../shared/api-error';
import { formatCountdown } from '../shared/countdown';
import { formatPaise, paiseToRupees, rupeesToPaise } from '../shared/money';
import { PaisePipe } from '../shared/paise.pipe';
import { ServerClock } from '../shared/server-clock';
import { AuctionStatus, AuctionUpdate, BidKind, PlaceBidResult } from './auction.model';
import { AuctionsApi } from './auctions.api';
import { applyOwnBid, applyUpdate, LiveAuctionState } from './live-auction-state';

const TICK_INTERVAL_MS = 250;
const CLOSING_SOON_MS = 60_000;
const MIN_BID_RUPEES = 0.01;

@Component({
  selector: 'app-auction-page',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, PaisePipe],
  templateUrl: './auction-page.html',
})
export class AuctionPage implements OnInit {
  readonly id = input.required<string>();

  private readonly auctionsApi = inject(AuctionsApi);
  private readonly hub = inject(AuctionHub);
  private readonly clock = inject(ServerClock);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(FormBuilder).nonNullable;
  protected readonly session = inject(Session);

  protected readonly AuctionStatus = AuctionStatus;
  protected readonly BidKind = BidKind;

  protected readonly state = signal<LiveAuctionState | null>(null);
  protected readonly now = signal(Date.now());
  protected readonly error = signal<string | null>(null);
  protected readonly liveError = signal<string | null>(null);
  protected readonly bidMessage = signal<string | null>(null);
  protected readonly bidError = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);

  protected readonly auction = computed(() => this.state()?.auction ?? null);
  protected readonly remainingMs = computed(() => {
    const auction = this.auction();
    return auction ? Date.parse(auction.endsAt) - this.now() : 0;
  });
  protected readonly countdown = computed(() => formatCountdown(this.remainingMs()));
  protected readonly isClosingSoon = computed(
    () => this.auction()?.status === AuctionStatus.Live && this.remainingMs() < CLOSING_SOON_MS,
  );
  protected readonly canBid = computed(() => {
    const auction = this.auction();
    return (
      !!auction &&
      auction.status === AuctionStatus.Live &&
      !auction.isSeller &&
      this.remainingMs() > 0
    );
  });
  protected readonly canEdit = computed(
    () => this.auction()?.isSeller === true && this.auction()?.status === AuctionStatus.Scheduled,
  );
  protected readonly canCancel = computed(() => {
    const auction = this.auction();
    if (!auction || ![AuctionStatus.Scheduled, AuctionStatus.Live].includes(auction.status)) {
      return false;
    }
    return this.session.isAdmin() || (auction.isSeller && auction.bidCount === 0);
  });

  protected readonly bidForm = this.formBuilder.group({
    amountInRupees: [0, [Validators.required, Validators.min(MIN_BID_RUPEES)]],
  });
  protected readonly proxyForm = this.formBuilder.group({
    maxInRupees: [0, [Validators.required, Validators.min(MIN_BID_RUPEES)]],
  });

  async ngOnInit(): Promise<void> {
    const ticker = setInterval(() => this.now.set(this.clock.now()), TICK_INTERVAL_MS);
    this.destroyRef.onDestroy(() => clearInterval(ticker));

    try {
      const [auction, bids] = await Promise.all([
        this.auctionsApi.get(this.id()),
        this.auctionsApi.bids(this.id()),
      ]);
      this.clock.sync(auction.serverTime);
      this.state.set({ auction, bids });
      this.suggestAmounts(true);
      this.watchLiveUpdates();
    } catch (error) {
      this.error.set(describeApiError(error));
    }
  }

  protected async placeBid(): Promise<void> {
    const amountInPaise = rupeesToPaise(this.bidForm.getRawValue().amountInRupees);
    await this.submit(
      () => this.auctionsApi.placeBid(this.id(), amountInPaise),
      (result) =>
        result.isLeading
          ? 'You are the highest bidder.'
          : 'Your bid was recorded, but another bidder’s automatic bid is higher.',
    );
  }

  protected async placeProxyBid(): Promise<void> {
    const maxInPaise = rupeesToPaise(this.proxyForm.getRawValue().maxInRupees);
    await this.submit(
      () => this.auctionsApi.placeProxyBid(this.id(), maxInPaise),
      (result) =>
        result.isLeading
          ? `You lead at ${formatPaise(result.auction.currentPriceInPaise ?? 0)}. The system bids for you up to ${formatPaise(maxInPaise)}.`
          : 'Another bidder’s maximum is higher. Your maximum was used up.',
    );
  }

  protected async cancel(): Promise<void> {
    this.bidError.set(null);
    try {
      const auction = await this.auctionsApi.cancel(this.id());
      this.state.update((state) => state && { ...state, auction });
    } catch (error) {
      this.bidError.set(describeApiError(error));
    }
  }

  private watchLiveUpdates(): void {
    this.hub
      .watch(this.id())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (update) => this.onUpdate(update),
        error: (error: unknown) =>
          this.liveError.set(`Live updates stopped: ${describeApiError(error)} Reload to retry.`),
      });
  }

  private onUpdate(update: AuctionUpdate): void {
    this.clock.sync(update.serverTime);
    this.liveError.set(null);
    this.state.update((state) => state && applyUpdate(state, update));
    this.suggestAmounts(false);
  }

  private async submit(
    send: () => Promise<PlaceBidResult>,
    describe: (result: PlaceBidResult) => string,
  ): Promise<void> {
    this.isSubmitting.set(true);
    this.bidMessage.set(null);
    this.bidError.set(null);
    try {
      const result = await send();
      this.clock.sync(result.auction.serverTime);
      this.state.update((state) => state && applyOwnBid(state, result.auction, result.placedBids));
      this.bidMessage.set(describe(result));
      this.suggestAmounts(true);
    } catch (error) {
      this.bidError.set(describeApiError(error));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  // Keeps the bid fields at the current minimum unless the user has started typing their own amount.
  private suggestAmounts(force: boolean): void {
    const minimum = this.auction()?.minimumNextBidInPaise;
    if (minimum === undefined) {
      return;
    }

    const minimumInRupees = paiseToRupees(minimum);
    if (force || this.bidForm.pristine) {
      this.bidForm.setValue({ amountInRupees: minimumInRupees });
      this.bidForm.markAsPristine();
    }
    if (force || this.proxyForm.pristine) {
      this.proxyForm.setValue({ maxInRupees: minimumInRupees });
      this.proxyForm.markAsPristine();
    }
  }
}
