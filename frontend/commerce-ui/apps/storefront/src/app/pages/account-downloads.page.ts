import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CustomerDownloadEntitlement, DownloadsApi } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [DatePipe, DecimalPipe, RouterLink, TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ 'downloads.title' | translate }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        @if (entitlements.length === 0) {
          <p>{{ 'downloads.empty' | translate }}</p>
        } @else {
          @for (item of entitlements; track item.entitlementId) {
            <article class="card">
              <h2>{{ item.productName }}</h2>
              <p class="meta">{{ 'orders.number' | translate }}: {{ item.orderNumber }}</p>
              <p class="meta">{{ 'downloads.grantedAt' | translate }}: {{ item.grantedAtUtc | date:'medium' }}</p>
              @if (item.expiresAtUtc) {
                <p class="meta">{{ 'downloads.expiresAt' | translate }}: {{ item.expiresAtUtc | date:'medium' }}</p>
              }
              @if (item.remainingDownloads != null) {
                <p class="meta">{{ 'downloads.remaining' | translate }}: {{ item.remainingDownloads }}</p>
              }
              <ul>
                @for (file of item.files; track file.fileId) {
                  <li>
                    <a [href]="downloadsApi.downloadUrl(item.entitlementId, file.fileId)" download>
                      {{ file.displayName || file.fileName }}
                    </a>
                    <span class="size">({{ file.fileSizeBytes | number }} bytes)</span>
                  </li>
                }
              </ul>
            </article>
          }
        }
        <p><a routerLink="/account">{{ 'nav.account' | translate }}</a></p>
      }
    }
  `,
  styles: [`
    .card { background: #fff; border: 1px solid #e5e7eb; border-radius: 0.5rem; padding: 1rem; margin-bottom: 1rem; }
    .meta { color: #6b7280; margin: 0.25rem 0; }
    .size { color: #6b7280; font-size: 0.875rem; margin-inline-start: 0.5rem; }
    ul { padding-inline-start: 1.25rem; }
  `]
})
export class AccountDownloadsPageComponent implements OnInit {
  readonly downloadsApi = inject(DownloadsApi);
  state: PageState = 'loading';
  errorMessage = '';
  entitlements: CustomerDownloadEntitlement[] = [];

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    this.errorMessage = '';
    try {
      this.entitlements = await firstValueFrom(this.downloadsApi.listCustomerDownloads());
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load downloads.';
      this.state = 'error';
    }
  }
}
