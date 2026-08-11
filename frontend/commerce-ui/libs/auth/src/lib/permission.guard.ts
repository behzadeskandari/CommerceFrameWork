import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PermissionService } from './permission.service';
import { AuthService } from './auth.service';

export function permissionGuard(requiredPermission: string): CanActivateFn {
  return async () => {
    const permissions = inject(PermissionService);
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.session()) {
      await auth.refreshSession();
    }

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login']);
    }

    if (permissions.hasPermission(requiredPermission)) {
      return true;
    }

    return router.createUrlTree(['/unauthorized']);
  };
}
