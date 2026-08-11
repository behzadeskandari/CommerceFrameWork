import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const guestGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.session()) {
    await auth.refreshSession();
  }

  if (!auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/']);
};
