import { Routes } from '@angular/router';
import { authGuard, guestGuard } from '@commerce/auth';
import { StorefrontLayoutComponent } from '@commerce/layout';
import { checkoutCartGuard } from './guards/checkout-cart.guard';

export const routes: Routes = [
  {
    path: '',
    component: StorefrontLayoutComponent,
    children: [
      { path: '', loadComponent: () => import('./pages/home.page').then(m => m.HomePageComponent), data: { themeLayout: 'Homepage' } },
      { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./pages/login.page').then(m => m.LoginPageComponent) },
      { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./pages/register.page').then(m => m.RegisterPageComponent) },
      { path: 'account', canActivate: [authGuard], loadComponent: () => import('./pages/account.page').then(m => m.AccountPageComponent), data: { themeLayout: 'Account' } },
      { path: 'account/addresses', canActivate: [authGuard], loadComponent: () => import('./pages/addresses.page').then(m => m.AddressesPageComponent), data: { themeLayout: 'Account' } },
      { path: 'account/orders', canActivate: [authGuard], loadComponent: () => import('./pages/account-orders.page').then(m => m.AccountOrdersPageComponent), data: { themeLayout: 'Account' } },
      { path: 'account/orders/:id', canActivate: [authGuard], loadComponent: () => import('./pages/account-order-detail.page').then(m => m.AccountOrderDetailPageComponent), data: { themeLayout: 'Account' } },
      { path: 'account/downloads', canActivate: [authGuard], loadComponent: () => import('./pages/account-downloads.page').then(m => m.AccountDownloadsPageComponent), data: { themeLayout: 'Account' } },
      { path: 'account/wishlist', canActivate: [authGuard], loadComponent: () => import('./pages/account-wishlist.page').then(m => m.AccountWishlistPageComponent), data: { themeLayout: 'Account' } },
      { path: 'account/preferences', canActivate: [authGuard], loadComponent: () => import('./pages/account-preferences.page').then(m => m.AccountPreferencesPageComponent), data: { themeLayout: 'Account' } },
      { path: 'account/loyalty', canActivate: [authGuard], loadComponent: () => import('./pages/account-loyalty.page').then(m => m.AccountLoyaltyPageComponent), data: { themeLayout: 'Account' } },
      { path: 'account/activity', canActivate: [authGuard], loadComponent: () => import('./pages/account-activity.page').then(m => m.AccountActivityPageComponent), data: { themeLayout: 'Account' } },
      { path: 'order-confirmation/:orderNumber', loadComponent: () => import('./pages/order-confirmation.page').then(m => m.OrderConfirmationPageComponent) },
      { path: 'categories', loadComponent: () => import('./pages/categories.page').then(m => m.CategoriesPageComponent), data: { themeLayout: 'Search' } },
      { path: 'category/:slug', loadComponent: () => import('./pages/category-detail.page').then(m => m.CategoryDetailPageComponent), data: { themeLayout: 'Category' } },
      { path: 'products', loadComponent: () => import('./pages/products.page').then(m => m.ProductsPageComponent), data: { themeLayout: 'Search' } },
      { path: 'cart', loadComponent: () => import('./pages/cart.page').then(m => m.CartPageComponent), data: { themeLayout: 'Cart' } },
      { path: 'checkout', canActivate: [checkoutCartGuard], loadComponent: () => import('./pages/checkout.page').then(m => m.CheckoutPageComponent), data: { themeLayout: 'Checkout' } },
      { path: 'payment/processing', loadComponent: () => import('./pages/payment-processing.page').then(m => m.PaymentProcessingPageComponent) },
      { path: 'payment/success', loadComponent: () => import('./pages/payment-success.page').then(m => m.PaymentSuccessPageComponent) },
      { path: 'payment/failed', loadComponent: () => import('./pages/payment-failed.page').then(m => m.PaymentFailedPageComponent) },
      { path: 'product/:slug', loadComponent: () => import('./pages/product-detail.page').then(m => m.ProductDetailPageComponent), data: { themeLayout: 'Product' } },
      { path: 'pages/:slug', loadComponent: () => import('./pages/cms-page.page').then(m => m.CmsPageComponent), data: { themeLayout: 'CmsPage' } }
    ]
  },
  { path: '**', redirectTo: '' }
];
