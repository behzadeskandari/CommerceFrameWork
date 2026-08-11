import { Injectable, inject } from '@angular/core';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private readonly auth = inject(AuthService);

  hasPermission(permission: string): boolean {
    const permissions = this.auth.session()?.permissions ?? [];
    return permissions.some(value => value.localeCompare(permission, undefined, { sensitivity: 'accent' }) === 0);
  }

  hasAnyPermission(permissions: string[]): boolean {
    return permissions.some(permission => this.hasPermission(permission));
  }

  hasAllPermissions(permissions: string[]): boolean {
    return permissions.every(permission => this.hasPermission(permission));
  }
}
