import { Injectable, computed, inject, signal } from '@angular/core';
import { StoreApi, StoreSummary } from '@commerce/api';
import { firstValueFrom } from 'rxjs';

const STORAGE_KEY = 'commerce.admin.storeId';

@Injectable({ providedIn: 'root' })
export class AdminContextService {
  private readonly storeApi = inject(StoreApi);

  private readonly storesSignal = signal<StoreSummary[]>([]);
  private readonly storeIdSignal = signal<number>(this.readStoredStoreId());

  readonly stores = this.storesSignal.asReadonly();
  readonly storeId = this.storeIdSignal.asReadonly();
  readonly currentStore = computed(() =>
    this.storesSignal().find(store => store.id === this.storeIdSignal()) ?? null
  );

  async initialize(): Promise<void> {
    const stores = await firstValueFrom(this.storeApi.listStores());
    this.storesSignal.set(stores);

    if (!stores.some(store => store.id === this.storeIdSignal()) && stores.length > 0) {
      this.selectStore(stores[0].id);
    }
  }

  selectStore(storeId: number): void {
    this.storeIdSignal.set(storeId);
    localStorage.setItem(STORAGE_KEY, String(storeId));
  }

  private readStoredStoreId(): number {
    const raw = localStorage.getItem(STORAGE_KEY);
    const parsed = raw ? Number.parseInt(raw, 10) : Number.NaN;
    return Number.isFinite(parsed) && parsed > 0 ? parsed : 1;
  }
}
