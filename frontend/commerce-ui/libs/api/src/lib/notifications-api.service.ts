import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  CreateNotificationTemplateRequest,
  NotificationDeliveryStatus,
  NotificationEventType,
  NotificationLogSummary,
  NotificationTemplateDetail,
  NotificationTemplateSummary,
  UpdateNotificationTemplateRequest
} from './models/notifications.models';

@Injectable({ providedIn: 'root' })
export class NotificationsApi {
  private readonly http = inject(HttpClient);
  private readonly templatesBase = '/api/admin/notifications/templates';
  private readonly logsBase = '/api/admin/notifications/logs';

  listTemplates(storeId?: number, eventType?: NotificationEventType): Observable<NotificationTemplateSummary[]> {
    const params = new URLSearchParams();
    if (storeId != null) {
      params.set('storeId', String(storeId));
    }
    if (eventType) {
      params.set('eventType', eventType);
    }
    const query = params.toString() ? `?${params.toString()}` : '';
    return this.http
      .get<ApiResponse<NotificationTemplateSummary[]>>(`${this.templatesBase}${query}`)
      .pipe(map(response => response.data!));
  }

  getTemplate(id: number): Observable<NotificationTemplateDetail> {
    return this.http
      .get<ApiResponse<NotificationTemplateDetail>>(`${this.templatesBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  createTemplate(request: CreateNotificationTemplateRequest): Observable<NotificationTemplateDetail> {
    return this.http
      .post<ApiResponse<NotificationTemplateDetail>>(this.templatesBase, request)
      .pipe(map(response => response.data!));
  }

  updateTemplate(id: number, request: UpdateNotificationTemplateRequest): Observable<NotificationTemplateDetail> {
    return this.http
      .put<ApiResponse<NotificationTemplateDetail>>(`${this.templatesBase}/${id}`, request)
      .pipe(map(response => response.data!));
  }

  activateTemplate(id: number): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`${this.templatesBase}/${id}/activate`, {}).pipe(map(() => undefined));
  }

  deactivateTemplate(id: number): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`${this.templatesBase}/${id}/deactivate`, {}).pipe(map(() => undefined));
  }

  deleteTemplate(id: number): Observable<void> {
    return this.http.delete<ApiResponse<unknown>>(`${this.templatesBase}/${id}`).pipe(map(() => undefined));
  }

  listLogs(
    storeId?: number,
    status?: NotificationDeliveryStatus,
    customerId?: number,
    take = 100
  ): Observable<NotificationLogSummary[]> {
    const params = new URLSearchParams();
    if (storeId != null) {
      params.set('storeId', String(storeId));
    }
    if (status) {
      params.set('status', status);
    }
    if (customerId != null) {
      params.set('customerId', String(customerId));
    }
    params.set('take', String(take));
    return this.http
      .get<ApiResponse<NotificationLogSummary[]>>(`${this.logsBase}?${params.toString()}`)
      .pipe(map(response => response.data!));
  }

  retryLog(id: number): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`${this.logsBase}/${id}/retry`, {}).pipe(map(() => undefined));
  }
}
