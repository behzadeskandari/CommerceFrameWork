import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { APP_CONFIG, ApiResponse } from '@commerce/core';
import { SessionResponse } from './models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  getSession(): Observable<SessionResponse> {
    return this.http
      .get<ApiResponse<SessionResponse>>(`${this.config.apiBaseUrl}/api/auth/session`)
      .pipe(map(response => response.data!));
  }
}
