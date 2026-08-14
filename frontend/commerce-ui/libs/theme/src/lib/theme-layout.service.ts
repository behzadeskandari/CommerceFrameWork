import { Injectable, inject } from '@angular/core';
import { ActivatedRouteSnapshot, Data } from '@angular/router';
import { ThemeLayoutType } from '@commerce/api';

@Injectable({ providedIn: 'root' })
export class ThemeLayoutService {
  resolveLayout(route: ActivatedRouteSnapshot): ThemeLayoutType {
    const data = this.collectRouteData(route);
    const layout = data['themeLayout'] as ThemeLayoutType | undefined;
    if (layout) {
      return layout;
    }

    const url = route.url.map(segment => segment.path).join('/');
    if (!url) return 'Homepage';
    if (url.startsWith('product/')) return 'Product';
    if (url.startsWith('category/')) return 'Category';
    if (url === 'products') return 'Search';
    if (url === 'cart') return 'Cart';
    if (url === 'checkout') return 'Checkout';
    if (url.startsWith('account')) return 'Account';
    if (url.startsWith('pages/')) return 'CmsPage';
    return 'CmsPage';
  }

  private collectRouteData(route: ActivatedRouteSnapshot): Data {
    let current: ActivatedRouteSnapshot | null = route;
    const merged: Data = {};
    while (current) {
      Object.assign(merged, current.data);
      current = current.parent;
    }

    return merged;
  }
}
