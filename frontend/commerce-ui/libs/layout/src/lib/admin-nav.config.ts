export interface AdminNavItem {
  labelKey: string;
  route: string;
  permission?: string;
  exact?: boolean;
}

export interface AdminNavGroup {
  labelKey: string;
  items: AdminNavItem[];
}

export const ADMIN_NAV_GROUPS: AdminNavGroup[] = [
  {
    labelKey: 'admin.nav.overview',
    items: [{ labelKey: 'nav.dashboard', route: '/dashboard' }]
  },
  {
    labelKey: 'admin.nav.catalog',
    items: [
      { labelKey: 'nav.products', route: '/catalog/products', permission: 'Catalog.Products.View' },
      { labelKey: 'nav.categories', route: '/catalog/categories', permission: 'Catalog.Categories.View' },
      { labelKey: 'nav.attributes', route: '/catalog/attributes', permission: 'Catalog.Attributes.View' },
      { labelKey: 'nav.media', route: '/media', permission: 'Media.View' }
    ]
  },
  {
    labelKey: 'admin.nav.sales',
    items: [
      { labelKey: 'nav.customers', route: '/customers', permission: 'Customers.View' },
      { labelKey: 'nav.customerSegments', route: '/customers/segments', permission: 'Customers.View' },
      { labelKey: 'nav.loyaltyRewards', route: '/customers/loyalty-rewards', permission: 'Customers.View' },
      { labelKey: 'nav.affiliates', route: '/customers/affiliates', permission: 'Customers.View' },
      { labelKey: 'nav.orders', route: '/orders', permission: 'Orders.View' },
      { labelKey: 'nav.payments', route: '/payments', permission: 'Payments.View' },
      { labelKey: 'nav.paymentMethods', route: '/payments/methods', permission: 'Payments.Configure' },
      { labelKey: 'nav.giftCards', route: '/payments/gift-cards', permission: 'Payments.View' }
    ]
  },
  {
    labelKey: 'admin.nav.inventory',
    items: [
      { labelKey: 'nav.inventory', route: '/inventory', permission: 'Inventory.View' },
      { labelKey: 'nav.warehouses', route: '/inventory/warehouses', permission: 'Inventory.View' }
    ]
  },
  {
    labelKey: 'admin.nav.pricing',
    items: [
      { labelKey: 'nav.discounts', route: '/pricing/discounts', permission: 'Discounts.View' },
      { labelKey: 'nav.coupons', route: '/pricing/coupons', permission: 'Coupons.View' },
      { labelKey: 'nav.customerGroups', route: '/pricing/customer-groups', permission: 'CustomerGroups.View' },
      { labelKey: 'nav.promotions', route: '/marketing/promotions', permission: 'Promotions.View' }
    ]
  },
  {
    labelKey: 'admin.nav.marketing',
    items: [
      { labelKey: 'nav.seoSettings', route: '/marketing/seo/settings', permission: 'Seo.View' },
      { labelKey: 'nav.seoUrlRecords', route: '/marketing/seo/url-records', permission: 'Seo.View' },
      { labelKey: 'nav.reviews', route: '/reviews', permission: 'Reviews.View' },
      { labelKey: 'nav.wishlists', route: '/reviews/wishlists', permission: 'Reviews.View' }
    ]
  },
  {
    labelKey: 'admin.nav.content',
    items: [
      { labelKey: 'nav.cmsPages', route: '/cms/pages', permission: 'Cms.Pages.View' },
      { labelKey: 'nav.cmsTopics', route: '/cms/topics', permission: 'Cms.Topics.View' },
      { labelKey: 'nav.cmsMenus', route: '/cms/menus', permission: 'Cms.Menus.View' },
      { labelKey: 'nav.cmsWidgets', route: '/cms/widgets', permission: 'Cms.Widgets.View' },
      { labelKey: 'nav.themes', route: '/themes', permission: 'Themes.View' }
    ]
  },
  {
    labelKey: 'admin.nav.operations',
    items: [
      { labelKey: 'nav.notificationTemplates', route: '/notifications/templates', permission: 'Notifications.View' },
      { labelKey: 'nav.notificationLogs', route: '/notifications/logs', permission: 'Notifications.View' },
      { labelKey: 'nav.backgroundJobs', route: '/scheduling/jobs', permission: 'Scheduling.View' },
      { labelKey: 'nav.recurringJobs', route: '/scheduling/recurring', permission: 'Scheduling.View' },
      { labelKey: 'nav.shippingMethods', route: '/shipping/methods', permission: 'Shipping.View' },
      { labelKey: 'nav.shippingZones', route: '/shipping/zones', permission: 'Shipping.View' },
      { labelKey: 'nav.shippingRates', route: '/shipping/rates', permission: 'Shipping.View' },
      { labelKey: 'nav.taxCategories', route: '/tax/categories', permission: 'Tax.View' },
      { labelKey: 'nav.taxZones', route: '/tax/zones', permission: 'Tax.View' },
      { labelKey: 'nav.taxRates', route: '/tax/rates', permission: 'Tax.View' },
      { labelKey: 'nav.taxSettings', route: '/tax/settings', permission: 'Tax.View' }
    ]
  },
  {
    labelKey: 'admin.nav.system',
    items: [
      { labelKey: 'nav.plugins', route: '/plugins', permission: 'Plugins.View' },
      { labelKey: 'nav.stores', route: '/stores', permission: 'Stores.View' },
      { labelKey: 'nav.languages', route: '/languages', permission: 'Languages.View' },
      { labelKey: 'nav.currencies', route: '/currencies', permission: 'Currencies.View' },
      { labelKey: 'nav.settings', route: '/settings', permission: 'Settings.View' }
    ]
  }
];
