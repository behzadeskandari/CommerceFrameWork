import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CartStateService } from '@commerce/api';

export const checkoutCartGuard: CanActivateFn = async () => {
  const cart = inject(CartStateService);
  const router = inject(Router);

  if (cart.itemCount() <= 0) {
    await cart.refresh();
  }

  if (cart.itemCount() <= 0) {
    return router.createUrlTree(['/cart'], { queryParams: { error: 'empty-cart' } });
  }

  return true;
};
