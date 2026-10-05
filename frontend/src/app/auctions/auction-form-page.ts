import { Component, inject, input, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { describeApiError } from '../shared/api-error';
import { fromDateTimeLocal, toDateTimeLocal } from '../shared/local-date-time';
import { paiseToRupees, rupeesToPaise } from '../shared/money';
import { AuctionTerms } from './auction.model';
import { AuctionsApi } from './auctions.api';

const MAX_TITLE_LENGTH = 120;
const MAX_DESCRIPTION_LENGTH = 4000;
const MIN_PRICE_RUPEES = 1;
const DEFAULT_DURATION_MS = 60 * 60 * 1000;

@Component({
  selector: 'app-auction-form-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './auction-form-page.html',
})
export class AuctionFormPage implements OnInit {
  // Set by the router on /auctions/:id/edit, absent on /auctions/new.
  readonly id = input<string>();

  private readonly auctionsApi = inject(AuctionsApi);
  private readonly router = inject(Router);

  protected readonly maxTitleLength = MAX_TITLE_LENGTH;
  protected readonly maxDescriptionLength = MAX_DESCRIPTION_LENGTH;
  protected readonly error = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);

  protected readonly form = inject(FormBuilder).group({
    title: ['', [Validators.required, Validators.maxLength(MAX_TITLE_LENGTH)]],
    description: ['', Validators.maxLength(MAX_DESCRIPTION_LENGTH)],
    startingPriceInRupees: [100, [Validators.required, Validators.min(MIN_PRICE_RUPEES)]],
    reservePriceInRupees: [null as number | null, Validators.min(MIN_PRICE_RUPEES)],
    startsAt: [toDateTimeLocal(new Date()), Validators.required],
    endsAt: [toDateTimeLocal(new Date(Date.now() + DEFAULT_DURATION_MS)), Validators.required],
  });

  async ngOnInit(): Promise<void> {
    const auctionId = this.id();
    if (!auctionId) {
      return;
    }

    try {
      const auction = await this.auctionsApi.get(auctionId);
      this.form.setValue({
        title: auction.title,
        description: auction.description,
        startingPriceInRupees: paiseToRupees(auction.startingPriceInPaise),
        reservePriceInRupees:
          auction.reservePriceInPaise === null ? null : paiseToRupees(auction.reservePriceInPaise),
        startsAt: toDateTimeLocal(new Date(auction.startsAt)),
        endsAt: toDateTimeLocal(new Date(auction.endsAt)),
      });
    } catch (error) {
      this.error.set(describeApiError(error));
    }
  }

  protected async save(): Promise<void> {
    const auctionId = this.id();
    this.isSubmitting.set(true);
    this.error.set(null);
    try {
      const terms = this.toTerms();
      const auction = auctionId
        ? await this.auctionsApi.revise(auctionId, terms)
        : await this.auctionsApi.create(terms);
      await this.router.navigate(['/auctions', auction.id]);
    } catch (error) {
      this.error.set(describeApiError(error));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private toTerms(): AuctionTerms {
    const value = this.form.getRawValue();
    return {
      title: value.title ?? '',
      description: value.description ?? '',
      startingPriceInPaise: rupeesToPaise(value.startingPriceInRupees ?? 0),
      reservePriceInPaise:
        value.reservePriceInRupees === null ? null : rupeesToPaise(value.reservePriceInRupees),
      startsAt: fromDateTimeLocal(value.startsAt ?? ''),
      endsAt: fromDateTimeLocal(value.endsAt ?? ''),
    };
  }
}
