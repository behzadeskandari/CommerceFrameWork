import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { WishlistApi, WishlistItem } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [DatePipe, RouterLink, TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ 'wishlist.title' | translate }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        @if (items.length === 0) {
          <p>{{ 'wishlist.empty' | translate }}</p>
        } @else {
          @for (item of items; track item.productId) {
            <article class="card">
              <h2>
                @if (item.slug) {
                  <a [routerLink]="['/product', item.slug]">{{ item.productName }}</a>
                } @else {
                  {{ item.productName }}
                }
              </h2>
              <p class="meta">{{ 'wishlist.addedAt' | translate }}: {{ item.addedAtUtc | date:'medium' }}</p>
              <p class="availability" [class.unavailable]="!item.isAvailable">
                {{ item.isAvailable ? ('wishlist.available' | translate) : ('wishlist.unavailable' | translate) }}
              </p>
              <button type="button" class="danger" [disabled]="removingId === item.productId" (click)="remove(item)">
                {{ 'wishlist.remove' | translate }}
              </button>
            </article>
          }
        }
        <p><a routerLink="/account">{{ 'nav.account' | translate }}</a></p>
      }
    }
  `,
  styles: [`
    .card { padding: 1rem; border: 1px solid #e5e7eb; border-radius: 0.5rem; margin-bottom: 1rem; }
    .meta { color: #6b7280; }
    .availability { color: #047857; font-weight: 600; }
    .availability.unavailable { color: #b91c1c; }
    button.danger { margin-top: 0.5rem; padding: 0.5rem 0.75rem; border: 1px solid #fecaca; background: #fff; color: #b91c1c; border-radius: 0.375rem; cursor: pointer; }
  `]
})
export class AccountWishlistPageComponent implements OnInit {
  private readonly wishlistApi = inject(WishlistApi);

  state: PageState = 'loading';
  errorMessage = '';
  items: WishlistItem[] = [];
  removingId: number | null = null;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const wishlist = await firstValueFrom(this.wishlistApi.get());
      this.items = wishlist.items;
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load wishlist.';
      this.state = 'error';
    }
  }

  async remove(item: WishlistItem): Promise<void> {
    this.removingId = item.productId;
    try {
      await firstValueFrom(this.wishlistApi.removeItem(item.productId));
      this.items = this.items.filter(x => x.productId !== item.productId);
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Remove failed.';
    } finally {
      this.removingId = null;
    }
  }
}
