import { NgTemplateOutlet } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CatalogApi, CategoryTreeNode } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import {
  ConfirmDialogComponent,
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState
} from '@commerce/shared';
import { firstValueFrom } from 'rxjs';
import { CatalogAdminFacade } from '../../services/catalog-admin.facade';

@Component({
  standalone: true,
  imports: [
    NgTemplateOutlet,
    RouterLink,
    TranslatePipe,
    BreadcrumbsComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    ConfirmDialogComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('catalog.categories.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'catalog.categories.title' | translate }}</h1>
      @if (permissions.hasPermission('Catalog.Categories.Create')) {
        <a routerLink="/catalog/categories/new" class="btn">Create category</a>
      }
    </header>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state /> }
      @default {
        <ul class="tree">
          @for (node of tree; track node.id) {
            <li><ng-container *ngTemplateOutlet="nodeTpl; context: { $implicit: node, depth: 0 }" /></li>
          }
        </ul>
      }
    }
    <ng-template #nodeTpl let-node let-depth="depth">
      <div class="tree-node" [style.paddingInlineStart.px]="depth * 16">
        <span>{{ node.name }}</span>
        <span class="meta">{{ node.published ? 'Published' : 'Draft' }} · Order {{ node.displayOrder }}</span>
        <span class="actions">
          @if (permissions.hasPermission('Catalog.Categories.Update')) {
            <a [routerLink]="['/catalog/categories', node.id]">Edit</a>
          }
          @if (permissions.hasPermission('Catalog.Categories.Delete')) {
            <button type="button" (click)="confirmDelete(node)">Delete</button>
          }
        </span>
      </div>
      @if (node.children.length) {
        <ul>
          @for (child of node.children; track child.id) {
            <li><ng-container *ngTemplateOutlet="nodeTpl; context: { $implicit: child, depth: depth + 1 }" /></li>
          }
        </ul>
      }
    </ng-template>
    <cmr-confirm-dialog
      [open]="deleteTarget !== null"
      title="Delete category"
      [message]="'Delete ' + (deleteTarget?.name ?? '') + '?'"
      (confirm)="deleteConfirmed()"
      (cancel)="deleteTarget = null" />
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; }
    .btn { padding: 0.5rem 1rem; background: #2563eb; color: #fff; text-decoration: none; border-radius: 0.375rem; }
    .tree, .tree ul { list-style: none; padding: 0; margin: 0; }
    .tree-node { display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; padding: 0.5rem 0; border-bottom: 1px solid #e5e7eb; background: #fff; }
    .meta { color: #6b7280; font-size: 0.875rem; }
    .actions { display: flex; gap: 0.5rem; margin-inline-start: auto; }
  `]
})
export class CategoryListPageComponent implements OnInit {
  private readonly facade = inject(CatalogAdminFacade);
  private readonly catalogApi = inject(CatalogApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  tree: CategoryTreeNode[] = [];
  deleteTarget: CategoryTreeNode | null = null;

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const categories = await this.facade.listCategories();
      this.tree = this.facade.buildCategoryTree(categories);
      this.state = this.tree.length ? 'success' : 'empty';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load categories.';
      this.state = 'error';
    }
  }

  confirmDelete(node: CategoryTreeNode): void {
    this.deleteTarget = node;
  }

  async deleteConfirmed(): Promise<void> {
    if (!this.deleteTarget) return;
    try {
      await firstValueFrom(this.catalogApi.deleteCategory(this.deleteTarget.id));
      this.deleteTarget = null;
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Delete failed.';
      this.state = 'error';
    }
  }
}
