import { Injectable, inject } from '@angular/core';
import { CatalogApi, CategorySummary, ProductSummary } from '@commerce/api';
import { CategoryTreeNode } from '@commerce/api';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class CatalogAdminFacade {
  private readonly catalogApi = inject(CatalogApi);

  listProducts(): Promise<ProductSummary[]> {
    return firstValueFrom(this.catalogApi.listProducts());
  }

  listCategories(): Promise<CategorySummary[]> {
    return firstValueFrom(this.catalogApi.listCategories());
  }

  buildCategoryTree(categories: CategorySummary[]): CategoryTreeNode[] {
    const nodes = new Map<number, CategoryTreeNode>();
    for (const category of categories) {
      nodes.set(category.id, { ...category, children: [] });
    }
    const roots: CategoryTreeNode[] = [];
    for (const node of nodes.values()) {
      if (node.parentCategoryId && nodes.has(node.parentCategoryId)) {
        nodes.get(node.parentCategoryId)!.children.push(node);
      } else {
        roots.push(node);
      }
    }
    const sortNodes = (items: CategoryTreeNode[]): void => {
      items.sort((a, b) => a.displayOrder - b.displayOrder || a.name.localeCompare(b.name));
      items.forEach(item => sortNodes(item.children));
    };
    sortNodes(roots);
    return roots;
  }
}
