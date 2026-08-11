import { Routes } from '@angular/router';
import { authGuard, guestGuard } from '@commerce/auth';
import { StorefrontLayoutComponent } from '@commerce/layout';
import { checkoutCartGuard } from './guards/checkout-cart.guard';

export const routes: Routes = [
  {
    path: '',
    component: StorefrontLayoutComponent,
    children: [
      { path: '', loadComponent: () => import('./pages/home.page').then(m => m.HomePageComponent) },
      { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./pages/login.page').then(m => m.LoginPageComponent) },
      { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./pages/register.page').then(m => m.RegisterPageComponent) },
      { path: 'account', canActivate: [authGuard], loadComponent: () => import('./pages/account.page').then(m => m.AccountPageComponent) },
      { path: 'account/addresses', canActivate: [authGuard], loadComponent: () => import('./pages/addresses.page').then(m => m.AddressesPageComponent) },
      { path: 'account/orders', canActivate: [authGuard], loadComponent: () => import('./pages/account-orders.page').then(m => m.AccountOrdersPageComponent) },
      { path: 'account/orders/:id', canActivate: [authGuard], loadComponent: () => import('./pages/account-order-detail.page').then(m => m.AccountOrderDetailPageComponent) },
      { path: 'order-confirmation/:orderNumber', loadComponent: () => import('./pages/order-confirmation.page').then(m => m.OrderConfirmationPageComponent) },
      { path: 'categories', loadComponent: () => import('./pages/categories.page').then(m => m.CategoriesPageComponent) },
      { path: 'category/:slug', loadComponent: () => import('./pages/category-detail.page').then(m => m.CategoryDetailPageComponent) },
      { path: 'products', loadComponent: () => import('./pages/products.page').then(m => m.ProductsPageComponent) },
      { path: 'cart', loadComponent: () => import('./pages/cart.page').then(m => m.CartPageComponent) },
      { path: 'checkout', canActivate: [checkoutCartGuard], loadComponent: () => import('./pages/checkout.page').then(m => m.CheckoutPageComponent) },
      { path: 'product/:slug', loadComponent: () => import('./pages/product-detail.page').then(m => m.ProductDetailPageComponent) }
    ]
  },
  { path: '**', redirectTo: '' }
];
