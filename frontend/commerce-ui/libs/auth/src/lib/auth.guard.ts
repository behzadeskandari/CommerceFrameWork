import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.session()) {
    await auth.refreshSession();
  }

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const adminAuthGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.session()) {
    await auth.refreshSession();
  }

  if (auth.isAuthenticated() && auth.isAdministrator()) {
    return true;
  }

  if (auth.isAuthenticated()) {
    return router.createUrlTree(['/unauthorized']);
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
