import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  ContentPageDetail,
  ContentPageSummary,
  CreateContentPageRequest,
  CreateMenuRequest,
  CreateTopicRequest,
  CreateWidgetInstanceRequest,
  MenuDetail,
  MenuSummary,
  StorefrontMenu,
  StorefrontPage,
  StorefrontWidget,
  TopicDetail,
  TopicSummary,
  UpdateContentPageRequest,
  UpdateMenuRequest,
  UpdateTopicRequest,
  UpdateWidgetInstanceRequest,
  WidgetInstance,
  WidgetZone
} from './models/cms.models';

@Injectable({ providedIn: 'root' })
export class CmsApi {
  private readonly http = inject(HttpClient);

  listPages(storeId?: number): Observable<ContentPageSummary[]> {
    const params = storeId != null ? { storeId: String(storeId) } : undefined;
    return this.http.get<ApiResponse<ContentPageSummary[]>>('/api/admin/cms/pages', { params }).pipe(map(r => r.data ?? []));
  }

  getPage(id: number): Observable<ContentPageDetail> {
    return this.http.get<ApiResponse<ContentPageDetail>>(`/api/admin/cms/pages/${id}`).pipe(map(r => r.data!));
  }

  createPage(body: CreateContentPageRequest): Observable<ContentPageDetail> {
    return this.http.post<ApiResponse<ContentPageDetail>>('/api/admin/cms/pages', body).pipe(map(r => r.data!));
  }

  updatePage(id: number, body: UpdateContentPageRequest): Observable<ContentPageDetail> {
    return this.http.put<ApiResponse<ContentPageDetail>>(`/api/admin/cms/pages/${id}`, body).pipe(map(r => r.data!));
  }

  deletePage(id: number): Observable<void> {
    return this.http.delete<ApiResponse<unknown>>(`/api/admin/cms/pages/${id}`).pipe(map(() => undefined));
  }

  publishPage(id: number): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`/api/admin/cms/pages/${id}/publish`, {}).pipe(map(() => undefined));
  }

  listTopics(storeId?: number): Observable<TopicSummary[]> {
    const params = storeId != null ? { storeId: String(storeId) } : undefined;
    return this.http.get<ApiResponse<TopicSummary[]>>('/api/admin/cms/topics', { params }).pipe(map(r => r.data ?? []));
  }

  getTopic(id: number): Observable<TopicDetail> {
    return this.http.get<ApiResponse<TopicDetail>>(`/api/admin/cms/topics/${id}`).pipe(map(r => r.data!));
  }

  createTopic(body: CreateTopicRequest): Observable<TopicDetail> {
    return this.http.post<ApiResponse<TopicDetail>>('/api/admin/cms/topics', body).pipe(map(r => r.data!));
  }

  updateTopic(id: number, body: UpdateTopicRequest): Observable<TopicDetail> {
    return this.http.put<ApiResponse<TopicDetail>>(`/api/admin/cms/topics/${id}`, body).pipe(map(r => r.data!));
  }

  deleteTopic(id: number): Observable<void> {
    return this.http.delete<ApiResponse<unknown>>(`/api/admin/cms/topics/${id}`).pipe(map(() => undefined));
  }

  listMenus(storeId?: number): Observable<MenuSummary[]> {
    const params = storeId != null ? { storeId: String(storeId) } : undefined;
    return this.http.get<ApiResponse<MenuSummary[]>>('/api/admin/cms/menus', { params }).pipe(map(r => r.data ?? []));
  }

  getMenu(id: number): Observable<MenuDetail> {
    return this.http.get<ApiResponse<MenuDetail>>(`/api/admin/cms/menus/${id}`).pipe(map(r => r.data!));
  }

  createMenu(body: CreateMenuRequest): Observable<MenuDetail> {
    return this.http.post<ApiResponse<MenuDetail>>('/api/admin/cms/menus', body).pipe(map(r => r.data!));
  }

  updateMenu(id: number, body: UpdateMenuRequest): Observable<MenuDetail> {
    return this.http.put<ApiResponse<MenuDetail>>(`/api/admin/cms/menus/${id}`, body).pipe(map(r => r.data!));
  }

  listWidgetZones(): Observable<WidgetZone[]> {
    return this.http.get<ApiResponse<WidgetZone[]>>('/api/admin/cms/widgets/zones').pipe(map(r => r.data ?? []));
  }

  listWidgetInstances(storeId?: number, zoneSystemName?: string): Observable<WidgetInstance[]> {
    const params: Record<string, string> = {};
    if (storeId != null) params['storeId'] = String(storeId);
    if (zoneSystemName) params['zoneSystemName'] = zoneSystemName;
    return this.http.get<ApiResponse<WidgetInstance[]>>('/api/admin/cms/widgets/instances', { params }).pipe(map(r => r.data ?? []));
  }

  createWidgetInstance(body: CreateWidgetInstanceRequest): Observable<WidgetInstance> {
    return this.http.post<ApiResponse<WidgetInstance>>('/api/admin/cms/widgets/instances', body).pipe(map(r => r.data!));
  }

  getStorefrontPage(slug: string): Observable<StorefrontPage> {
    return this.http.get<ApiResponse<StorefrontPage>>(`/api/cms/pages/by-slug/${encodeURIComponent(slug)}`).pipe(map(r => r.data!));
  }

  getStorefrontMenu(systemName: string): Observable<StorefrontMenu> {
    return this.http.get<ApiResponse<StorefrontMenu>>(`/api/cms/menus/${encodeURIComponent(systemName)}`).pipe(map(r => r.data!));
  }

  getStorefrontWidgets(zoneSystemName: string): Observable<StorefrontWidget[]> {
    return this.http.get<ApiResponse<StorefrontWidget[]>>(`/api/cms/widgets/${encodeURIComponent(zoneSystemName)}`).pipe(map(r => r.data ?? []));
  }
}
