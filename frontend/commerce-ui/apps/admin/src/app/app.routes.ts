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
        canActivate: [permissionGuard('Analytics.View')],
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
        path: 'customers/segments',
        canActivate: [permissionGuard('Customers.Segments.View')],
        loadComponent: () => import('./pages/customers/segment-list.page').then(m => m.SegmentListPageComponent)
      },
      {
        path: 'customers/loyalty-rewards',
        canActivate: [permissionGuard('Customers.Loyalty.View')],
        loadComponent: () => import('./pages/customers/loyalty-reward-list.page').then(m => m.LoyaltyRewardListPageComponent)
      },
      {
        path: 'customers/affiliates',
        canActivate: [permissionGuard('Customers.Affiliates.View')],
        loadComponent: () => import('./pages/customers/affiliate-list.page').then(m => m.AffiliateListPageComponent)
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
        path: 'inventory/warehouses',
        canActivate: [permissionGuard('Inventory.View')],
        loadComponent: () => import('./pages/inventory/warehouse-list.page').then(m => m.WarehouseListPageComponent)
      },
      {
        path: 'inventory/:id',
        canActivate: [permissionGuard('Inventory.View')],
        loadComponent: () => import('./pages/inventory/inventory-detail.page').then(m => m.InventoryDetailPageComponent)
      },
      {
        path: 'pricing/discounts',
        canActivate: [permissionGuard('Discounts.View')],
        loadComponent: () => import('./pages/pricing/discount-list.page').then(m => m.DiscountListPageComponent)
      },
      {
        path: 'pricing/discounts/new',
        canActivate: [permissionGuard('Discounts.Create')],
        loadComponent: () => import('./pages/pricing/discount-form.page').then(m => m.DiscountFormPageComponent)
      },
      {
        path: 'pricing/discounts/:id',
        canActivate: [permissionGuard('Discounts.Update')],
        loadComponent: () => import('./pages/pricing/discount-form.page').then(m => m.DiscountFormPageComponent)
      },
      {
        path: 'pricing/coupons',
        canActivate: [permissionGuard('Coupons.View')],
        loadComponent: () => import('./pages/pricing/coupon-list.page').then(m => m.CouponListPageComponent)
      },
      {
        path: 'pricing/coupons/new',
        canActivate: [permissionGuard('Coupons.Manage')],
        loadComponent: () => import('./pages/pricing/coupon-form.page').then(m => m.CouponFormPageComponent)
      },
      {
        path: 'pricing/coupons/:id',
        canActivate: [permissionGuard('Coupons.Manage')],
        loadComponent: () => import('./pages/pricing/coupon-form.page').then(m => m.CouponFormPageComponent)
      },
      {
        path: 'pricing/customer-groups',
        canActivate: [permissionGuard('CustomerGroups.View')],
        loadComponent: () => import('./pages/pricing/customer-group-list.page').then(m => m.CustomerGroupListPageComponent)
      },
      {
        path: 'marketing/promotions',
        canActivate: [permissionGuard('Promotions.View')],
        loadComponent: () => import('./pages/marketing/promotion-list.page').then(m => m.PromotionListPageComponent)
      },
      {
        path: 'marketing/promotions/new',
        canActivate: [permissionGuard('Promotions.Manage')],
        loadComponent: () => import('./pages/marketing/promotion-form.page').then(m => m.PromotionFormPageComponent)
      },
      {
        path: 'marketing/promotions/:id',
        canActivate: [permissionGuard('Promotions.Manage')],
        loadComponent: () => import('./pages/marketing/promotion-form.page').then(m => m.PromotionFormPageComponent)
      },
      {
        path: 'marketing/seo/settings',
        canActivate: [permissionGuard('Seo.View')],
        loadComponent: () => import('./pages/marketing/seo-settings.page').then(m => m.SeoSettingsPageComponent)
      },
      {
        path: 'marketing/seo/url-records',
        canActivate: [permissionGuard('Seo.View')],
        loadComponent: () => import('./pages/marketing/seo-url-records.page').then(m => m.SeoUrlRecordsPageComponent)
      },
      {
        path: 'notifications/templates',
        canActivate: [permissionGuard('Notifications.View')],
        loadComponent: () => import('./pages/notifications/template-list.page').then(m => m.NotificationTemplateListPageComponent)
      },
      {
        path: 'notifications/templates/new',
        canActivate: [permissionGuard('Notifications.Manage')],
        loadComponent: () => import('./pages/notifications/template-form.page').then(m => m.NotificationTemplateFormPageComponent)
      },
      {
        path: 'notifications/templates/:id',
        canActivate: [permissionGuard('Notifications.Manage')],
        loadComponent: () => import('./pages/notifications/template-form.page').then(m => m.NotificationTemplateFormPageComponent)
      },
      {
        path: 'notifications/logs',
        canActivate: [permissionGuard('Notifications.View')],
        loadComponent: () => import('./pages/notifications/log-list.page').then(m => m.NotificationLogListPageComponent)
      },
      {
        path: 'scheduling/jobs',
        canActivate: [permissionGuard('Scheduling.View')],
        loadComponent: () => import('./pages/scheduling/job-list.page').then(m => m.BackgroundJobListPageComponent)
      },
      {
        path: 'scheduling/recurring',
        canActivate: [permissionGuard('Scheduling.View')],
        loadComponent: () => import('./pages/scheduling/job-list.page').then(m => m.RecurringJobListPageComponent)
      },
      {
        path: 'cms/pages',
        canActivate: [permissionGuard('Cms.Pages.View')],
        loadComponent: () => import('./pages/cms/page-list.page').then(m => m.CmsPageListPageComponent)
      },
      {
        path: 'cms/pages/new',
        canActivate: [permissionGuard('Cms.Pages.Manage')],
        loadComponent: () => import('./pages/cms/page-form.page').then(m => m.CmsPageFormPageComponent)
      },
      {
        path: 'cms/pages/:id',
        canActivate: [permissionGuard('Cms.Pages.Manage')],
        loadComponent: () => import('./pages/cms/page-form.page').then(m => m.CmsPageFormPageComponent)
      },
      {
        path: 'cms/topics',
        canActivate: [permissionGuard('Cms.Topics.View')],
        loadComponent: () => import('./pages/cms/topic-list.page').then(m => m.CmsTopicListPageComponent)
      },
      {
        path: 'cms/menus',
        canActivate: [permissionGuard('Cms.Menus.View')],
        loadComponent: () => import('./pages/cms/menu-list.page').then(m => m.CmsMenuListPageComponent)
      },
      {
        path: 'cms/widgets',
        canActivate: [permissionGuard('Cms.Widgets.View')],
        loadComponent: () => import('./pages/cms/widget-list.page').then(m => m.CmsWidgetListPageComponent)
      },
      {
        path: 'themes',
        canActivate: [permissionGuard('Themes.View')],
        loadComponent: () => import('./pages/themes/theme-list.page').then(m => m.ThemeListPageComponent)
      },
      {
        path: 'themes/:systemName',
        canActivate: [permissionGuard('Themes.Manage')],
        loadComponent: () => import('./pages/themes/theme-detail.page').then(m => m.ThemeDetailPageComponent)
      },
      {
        path: 'shipping/methods',
        canActivate: [permissionGuard('Shipping.View')],
        loadComponent: () => import('./pages/shipping/method-list.page').then(m => m.MethodListPageComponent)
      },
      {
        path: 'shipping/methods/new',
        canActivate: [permissionGuard('Shipping.Manage')],
        loadComponent: () => import('./pages/shipping/method-form.page').then(m => m.MethodFormPageComponent)
      },
      {
        path: 'shipping/methods/:id',
        canActivate: [permissionGuard('Shipping.Manage')],
        loadComponent: () => import('./pages/shipping/method-form.page').then(m => m.MethodFormPageComponent)
      },
      {
        path: 'shipping/zones',
        canActivate: [permissionGuard('Shipping.View')],
        loadComponent: () => import('./pages/shipping/zone-list.page').then(m => m.ZoneListPageComponent)
      },
      {
        path: 'shipping/zones/new',
        canActivate: [permissionGuard('Shipping.Manage')],
        loadComponent: () => import('./pages/shipping/zone-form.page').then(m => m.ZoneFormPageComponent)
      },
      {
        path: 'shipping/zones/:id',
        canActivate: [permissionGuard('Shipping.Manage')],
        loadComponent: () => import('./pages/shipping/zone-form.page').then(m => m.ZoneFormPageComponent)
      },
      {
        path: 'shipping/rates',
        canActivate: [permissionGuard('Shipping.View')],
        loadComponent: () => import('./pages/shipping/rate-list.page').then(m => m.RateListPageComponent)
      },
      {
        path: 'shipping/rates/new',
        canActivate: [permissionGuard('Shipping.Manage')],
        loadComponent: () => import('./pages/shipping/rate-form.page').then(m => m.RateFormPageComponent)
      },
      {
        path: 'shipping/rates/:id',
        canActivate: [permissionGuard('Shipping.Manage')],
        loadComponent: () => import('./pages/shipping/rate-form.page').then(m => m.RateFormPageComponent)
      },
      {
        path: 'tax/categories',
        canActivate: [permissionGuard('Tax.View')],
        loadComponent: () => import('./pages/tax/category-list.page').then(m => m.CategoryListPageComponent)
      },
      {
        path: 'tax/categories/new',
        canActivate: [permissionGuard('Tax.Manage')],
        loadComponent: () => import('./pages/tax/category-form.page').then(m => m.CategoryFormPageComponent)
      },
      {
        path: 'tax/categories/:id',
        canActivate: [permissionGuard('Tax.Manage')],
        loadComponent: () => import('./pages/tax/category-form.page').then(m => m.CategoryFormPageComponent)
      },
      {
        path: 'tax/zones',
        canActivate: [permissionGuard('Tax.View')],
        loadComponent: () => import('./pages/tax/zone-list.page').then(m => m.ZoneListPageComponent)
      },
      {
        path: 'tax/zones/new',
        canActivate: [permissionGuard('Tax.Manage')],
        loadComponent: () => import('./pages/tax/zone-form.page').then(m => m.ZoneFormPageComponent)
      },
      {
        path: 'tax/zones/:id',
        canActivate: [permissionGuard('Tax.Manage')],
        loadComponent: () => import('./pages/tax/zone-form.page').then(m => m.ZoneFormPageComponent)
      },
      {
        path: 'tax/rates',
        canActivate: [permissionGuard('Tax.View')],
        loadComponent: () => import('./pages/tax/rate-list.page').then(m => m.RateListPageComponent)
      },
      {
        path: 'tax/rates/new',
        canActivate: [permissionGuard('Tax.Manage')],
        loadComponent: () => import('./pages/tax/rate-form.page').then(m => m.RateFormPageComponent)
      },
      {
        path: 'tax/rates/:id',
        canActivate: [permissionGuard('Tax.Manage')],
        loadComponent: () => import('./pages/tax/rate-form.page').then(m => m.RateFormPageComponent)
      },
      {
        path: 'tax/settings',
        canActivate: [permissionGuard('Tax.View')],
        loadComponent: () => import('./pages/tax/tax-settings.page').then(m => m.TaxSettingsPageComponent)
      },
      {
        path: 'payments/methods',
        canActivate: [permissionGuard('Payments.Configure')],
        loadComponent: () => import('./pages/payments/method-list.page').then(m => m.PaymentMethodListPageComponent)
      },
      {
        path: 'payments/methods/new',
        canActivate: [permissionGuard('Payments.Configure')],
        loadComponent: () => import('./pages/payments/method-form.page').then(m => m.PaymentMethodFormPageComponent)
      },
      {
        path: 'payments/methods/:id',
        canActivate: [permissionGuard('Payments.Configure')],
        loadComponent: () => import('./pages/payments/method-form.page').then(m => m.PaymentMethodFormPageComponent)
      },
      {
        path: 'payments/gift-cards',
        canActivate: [permissionGuard('Payments.GiftCards.View')],
        loadComponent: () => import('./pages/payments/gift-card-list.page').then(m => m.GiftCardListPageComponent)
      },
      {
        path: 'payments',
        canActivate: [permissionGuard('Payments.View')],
        loadComponent: () => import('./pages/payments/payment-list.page').then(m => m.PaymentListPageComponent)
      },
      {
        path: 'payments/:id',
        canActivate: [permissionGuard('Payments.View')],
        loadComponent: () => import('./pages/payments/payment-detail.page').then(m => m.PaymentDetailPageComponent)
      },
      {
        path: 'reviews',
        canActivate: [permissionGuard('Reviews.View')],
        loadComponent: () => import('./pages/reviews/review-list.page').then(m => m.ReviewListPageComponent)
      },
      {
        path: 'reviews/wishlists',
        canActivate: [permissionGuard('Reviews.View')],
        loadComponent: () => import('./pages/reviews/wishlist-list.page').then(m => m.WishlistListPageComponent)
      },
      {
        path: 'plugins',
        canActivate: [permissionGuard('Plugins.View')],
        loadComponent: () => import('./pages/plugins/plugin-list.page').then(m => m.PluginListPageComponent)
      },
      {
        path: 'plugins/:systemName',
        canActivate: [permissionGuard('Plugins.View')],
        loadComponent: () => import('./pages/plugins/plugin-detail.page').then(m => m.PluginDetailPageComponent)
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
