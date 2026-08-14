import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  AffiliateDetail,
  AffiliateSummary,
  AffiliateCommissionTransaction,
  AffiliateReferral,
  CreateAffiliateRequest,
  UpdateAffiliateRequest
} from './models/affiliates.models';

@Injectable({ providedIn: 'root' })
export class AffiliatesApi {
  private readonly http = inject(HttpClient);

  list(storeId?: number) {
    const params = storeId != null ? { storeId: String(storeId) } : undefined;
    return firstValueFrom(
      this.http.get<{ success: boolean; data: AffiliateSummary[] }>('/api/admin/affiliates', { params })
    );
  }

  get(id: number) {
    return firstValueFrom(
      this.http.get<{ success: boolean; data: AffiliateDetail }>(`/api/admin/affiliates/${id}`)
    );
  }

  create(request: CreateAffiliateRequest) {
    return firstValueFrom(
      this.http.post<{ success: boolean; data: AffiliateDetail }>('/api/admin/affiliates', request)
    );
  }

  update(id: number, request: UpdateAffiliateRequest) {
    return firstValueFrom(
      this.http.put<{ success: boolean; data: AffiliateDetail }>(`/api/admin/affiliates/${id}`, request)
    );
  }

  delete(id: number) {
    return firstValueFrom(
      this.http.delete<{ success: boolean }>(`/api/admin/affiliates/${id}`)
    );
  }

  listCommissions(id: number) {
    return firstValueFrom(
      this.http.get<{ success: boolean; data: AffiliateCommissionTransaction[] }>(`/api/admin/affiliates/${id}/commissions`)
    );
  }

  listReferrals(id: number) {
    return firstValueFrom(
      this.http.get<{ success: boolean; data: AffiliateReferral[] }>(`/api/admin/affiliates/${id}/referrals`)
    );
  }
}
