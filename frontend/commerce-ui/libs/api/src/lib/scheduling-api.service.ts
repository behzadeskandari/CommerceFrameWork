import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ApiResponse } from '@commerce/core';
import { map, Observable } from 'rxjs';
import {
  BackgroundJobDetail,
  BackgroundJobStatus,
  BackgroundJobSummary,
  RecurringJobScheduleSummary
} from './models/scheduling.models';

@Injectable({ providedIn: 'root' })
export class SchedulingApi {
  private readonly http = inject(HttpClient);
  private readonly jobsBase = '/api/admin/scheduling/jobs';
  private readonly recurringBase = '/api/admin/scheduling/recurring';

  listJobs(status?: BackgroundJobStatus, jobType?: string, take = 100): Observable<BackgroundJobSummary[]> {
    const params = new URLSearchParams();
    if (status) {
      params.set('status', status);
    }
    if (jobType) {
      params.set('jobType', jobType);
    }
    params.set('take', String(take));
    return this.http
      .get<ApiResponse<BackgroundJobSummary[]>>(`${this.jobsBase}?${params.toString()}`)
      .pipe(map(response => response.data!));
  }

  getJob(id: number): Observable<BackgroundJobDetail> {
    return this.http
      .get<ApiResponse<BackgroundJobDetail>>(`${this.jobsBase}/${id}`)
      .pipe(map(response => response.data!));
  }

  cancelJob(id: number): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`${this.jobsBase}/${id}/cancel`, {}).pipe(map(() => undefined));
  }

  retryJob(id: number): Observable<void> {
    return this.http.post<ApiResponse<unknown>>(`${this.jobsBase}/${id}/retry`, {}).pipe(map(() => undefined));
  }

  listRecurring(): Observable<RecurringJobScheduleSummary[]> {
    return this.http
      .get<ApiResponse<RecurringJobScheduleSummary[]>>(this.recurringBase)
      .pipe(map(response => response.data!));
  }

  enableRecurring(scheduleKey: string): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.recurringBase}/${encodeURIComponent(scheduleKey)}/enable`, {})
      .pipe(map(() => undefined));
  }

  disableRecurring(scheduleKey: string): Observable<void> {
    return this.http
      .post<ApiResponse<unknown>>(`${this.recurringBase}/${encodeURIComponent(scheduleKey)}/disable`, {})
      .pipe(map(() => undefined));
  }
}
