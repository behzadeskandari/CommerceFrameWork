import { Routes } from '@angular/router';
import { adminAuthGuard } from '@commerce/auth';
import { permissionGuard } from '@commerce/auth';
import { AdminLayoutComponent } from '@commerce/layout';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login.page').then(m => m.AdminLoginPageComponent)
  },
  {
    path: 'unauthorized',
    loadComponent: () => import('./pages/unauthorized.page').then(m => m.UnauthorizedPageComponent)
  },
  {
    path: '',
    component: AdminLayoutComponent,
    canActivate: [adminAuthGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/dashboard.page').then(m => m.DashboardPageComponent)
      },
      {
        path: 'catalog/products',
        canActivate: [permissionGuard('Catalog.Products.View')],
        loadComponent: () => import('./pages/catalog/product-list.page').then(m => m.ProductListPageComponent)
      },
      {
        path: 'catalog/products/new',
        canActivate: [permissionGuard('Catalog.Products.Create')],
        loadComponent: () => import('./pages/catalog/product-form.page').then(m => m.ProductFormPageComponent)
      },
      {
        path: 'catalog/products/:id',
        canActivate: [permissionGuard('Catalog.Products.Update')],
        loadComponent: () => import('./pages/catalog/product-form.page').then(m => m.ProductFormPageComponent)
      },
      {
        path: 'catalog/attributes',
        canActivate: [permissionGuard('Catalog.Attributes.View')],
        loadComponent: () => import('./pages/catalog/attribute-list.page').then(m => m.AttributeListPageComponent)
      },
      {
        path: 'media',
        canActivate: [permissionGuard('Media.View')],
        loadComponent: () => import('./pages/media/media-list.page').then(m => m.MediaListPageComponent)
      },
      {
        path: 'catalog/categories',
        canActivate: [permissionGuard('Catalog.Categories.View')],
        loadComponent: () => import('./pages/catalog/category-list.page').then(m => m.CategoryListPageComponent)
      },
      {
        path: 'catalog/categories/new',
        canActivate: [permissionGuard('Catalog.Categories.Create')],
        loadComponent: () => import('./pages/catalog/category-form.page').then(m => m.CategoryFormPageComponent)
      },
      {
        path: 'catalog/categories/:id',
        canActivate: [permissionGuard('Catalog.Categories.Update')],
        loadComponent: () => import('./pages/catalog/category-form.page').then(m => m.CategoryFormPageComponent)
      },
      {
        path: 'customers',
        canActivate: [permissionGuard('Customers.View')],
        loadComponent: () => import('./pages/customers/customer-list.page').then(m => m.CustomerListPageComponent)
      },
      {
        path: 'customers/:id',
        canActivate: [permissionGuard('Customers.View')],
        loadComponent: () => import('./pages/customers/customer-detail.page').then(m => m.CustomerDetailPageComponent)
      },
      {
        path: 'orders',
        canActivate: [permissionGuard('Orders.View')],
        loadComponent: () => import('./pages/orders/order-list.page').then(m => m.OrderListPageComponent)
      },
      {
        path: 'orders/:id',
        canActivate: [permissionGuard('Orders.View')],
        loadComponent: () => import('./pages/orders/order-detail.page').then(m => m.OrderDetailPageComponent)
      },
      {
        path: 'inventory',
        canActivate: [permissionGuard('Inventory.View')],
        loadComponent: () => import('./pages/inventory/inventory-list.page').then(m => m.InventoryListPageComponent)
      },
      {
        path: 'inventory/:id',
        canActivate: [permissionGuard('Inventory.View')],
        loadComponent: () => import('./pages/inventory/inventory-detail.page').then(m => m.InventoryDetailPageComponent)
      },
      {
        path: 'stores',
        canActivate: [permissionGuard('Stores.View')],
        loadComponent: () => import('./pages/stores/store-list.page').then(m => m.StoreListPageComponent)
      },
      {
        path: 'stores/new',
        canActivate: [permissionGuard('Stores.Create')],
        loadComponent: () => import('./pages/stores/store-form.page').then(m => m.StoreFormPageComponent)
      },
      {
        path: 'stores/:id',
        canActivate: [permissionGuard('Stores.Update')],
        loadComponent: () => import('./pages/stores/store-form.page').then(m => m.StoreFormPageComponent)
      },
      {
        path: 'languages',
        canActivate: [permissionGuard('Languages.View')],
        loadComponent: () => import('./pages/languages/language-list.page').then(m => m.LanguageListPageComponent)
      },
      {
        path: 'currencies',
        canActivate: [permissionGuard('Currencies.View')],
        loadComponent: () => import('./pages/currencies/currency-list.page').then(m => m.CurrencyListPageComponent)
      },
      {
        path: 'settings',
        canActivate: [permissionGuard('Settings.View')],
        loadComponent: () => import('./pages/settings/settings.page').then(m => m.SettingsPageComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
