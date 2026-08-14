import { Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  CreateTaxCategoryRequest,
  TaxApi,
  UpdateTaxCategoryRequest
} from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe, BreadcrumbsComponent, LoadingStateComponent, ErrorStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('tax.categories.title' | translate), link: '/tax/categories' },
      { label: isEdit ? form.name : ('action.create' | translate) }
    ]" />
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="form" (ngSubmit)="save()">
          <h1>{{ isEdit ? form.name : ('tax.categories.create' | translate) }}</h1>

          @if (!isEdit) {
            <label>{{ 'tax.categories.systemName' | translate }}
              <input [(ngModel)]="form.systemName" name="systemName" required />
            </label>
            <label>{{ 'tax.storeId' | translate }}
              <input type="number" [(ngModel)]="form.storeId" name="storeId" required />
            </label>
          }
          <label>{{ 'tax.categories.name' | translate }}
            <input [(ngModel)]="form.name" name="name" required />
          </label>
          <label>{{ 'tax.categories.description' | translate }}
            <textarea [(ngModel)]="form.description" name="description" rows="3"></textarea>
          </label>
          <label>{{ 'tax.displayOrder' | translate }}
            <input type="number" [(ngModel)]="form.displayOrder" name="displayOrder" required />
          </label>
          <label class="checkbox">
            <input type="checkbox" [(ngModel)]="form.isExempt" name="isExempt" />
            {{ 'tax.categories.isExempt' | translate }}
          </label>
          <label class="checkbox">
            <input type="checkbox" [(ngModel)]="form.isActive" name="isActive" />
            {{ 'tax.active' | translate }}
          </label>

          <div class="actions">
            <button type="submit" class="btn btn--primary">{{ 'action.save' | translate }}</button>
            <a routerLink="/tax/categories">{{ 'action.cancel' | translate }}</a>
          </div>
        </form>
      }
    }
  `,
  styles: [`
    .form { display: grid; gap: 0.75rem; max-width: 40rem; background: #fff; padding: 1rem; border-radius: 0.5rem; }
    label { display: grid; gap: 0.25rem; }
    label.checkbox { display: flex; align-items: center; gap: 0.5rem; }
    input, select, textarea { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .actions { display: flex; gap: 0.75rem; align-items: center; margin-top: 0.5rem; }
    .btn { padding: 0.5rem 1rem; border-radius: 0.375rem; border: none; cursor: pointer; }
    .btn--primary { background: #2563eb; color: #fff; }
  `]
})
export class CategoryFormPageComponent implements OnInit {
  readonly id = input<number | undefined>();

  private readonly taxApi = inject(TaxApi);
  private readonly router = inject(Router);

  state: PageState = 'loading';
  errorMessage = '';
  isEdit = false;

  form = {
    storeId: 1,
    systemName: '',
    name: '',
    description: '' as string | null,
    isExempt: false,
    isActive: true,
    displayOrder: 0
  };

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const categoryId = this.id();
      if (categoryId) {
        this.isEdit = true;
        const detail = await firstValueFrom(this.taxApi.getCategory(categoryId));
        this.form = {
          storeId: detail.storeId,
          systemName: detail.systemName,
          name: detail.name,
          description: detail.description,
          isExempt: detail.isExempt,
          isActive: detail.isActive,
          displayOrder: detail.displayOrder
        };
      }
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load tax category.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    try {
      if (this.isEdit && this.id()) {
        const request: UpdateTaxCategoryRequest = {
          name: this.form.name,
          description: this.form.description,
          isExempt: this.form.isExempt,
          isActive: this.form.isActive,
          displayOrder: this.form.displayOrder
        };
        await firstValueFrom(this.taxApi.updateCategory(this.id()!, request));
      } else {
        const request: CreateTaxCategoryRequest = {
          storeId: this.form.storeId,
          name: this.form.name,
          systemName: this.form.systemName,
          description: this.form.description,
          isExempt: this.form.isExempt,
          isActive: this.form.isActive,
          displayOrder: this.form.displayOrder
        };
        await firstValueFrom(this.taxApi.createCategory(request));
      }
      await this.router.navigateByUrl('/tax/categories');
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
      this.state = 'error';
    }
  }
}
