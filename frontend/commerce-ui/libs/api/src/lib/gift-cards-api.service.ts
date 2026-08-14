import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  CreateGiftCardRequest,
  GiftCardDetail,
  GiftCardSummary,
  GiftCardTransaction,
  UpdateGiftCardRequest
} from './models/gift-cards.models';

@Injectable({ providedIn: 'root' })
export class GiftCardsApi {
  private readonly http = inject(HttpClient);

  list(storeId?: number) {
    const params = storeId != null ? { storeId: String(storeId) } : undefined;
    return firstValueFrom(
      this.http.get<{ success: boolean; data: GiftCardSummary[] }>('/api/admin/gift-cards', { params })
    );
  }

  get(id: number) {
    return firstValueFrom(
      this.http.get<{ success: boolean; data: GiftCardDetail }>(`/api/admin/gift-cards/${id}`)
    );
  }

  create(request: CreateGiftCardRequest) {
    return firstValueFrom(
      this.http.post<{ success: boolean; data: GiftCardDetail }>('/api/admin/gift-cards', request)
    );
  }

  update(id: number, request: UpdateGiftCardRequest) {
    return firstValueFrom(
      this.http.put<{ success: boolean; data: GiftCardDetail }>(`/api/admin/gift-cards/${id}`, request)
    );
  }

  delete(id: number) {
    return firstValueFrom(
      this.http.delete<{ success: boolean }>(`/api/admin/gift-cards/${id}`)
    );
  }

  listTransactions(id: number) {
    return firstValueFrom(
      this.http.get<{ success: boolean; data: GiftCardTransaction[] }>(`/api/admin/gift-cards/${id}/transactions`)
    );
  }
}
