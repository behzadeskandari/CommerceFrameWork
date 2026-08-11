import { Component, OnInit, inject, input } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CatalogApi, CategorySummary } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';
import { CatalogAdminFacade } from '../../services/catalog-admin.facade';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LoadingStateComponent, ErrorStateComponent],
  template: `
    @if (state === 'loading') { <cmr-loading-state /> } @else {
      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }
      <form [formGroup]="form" (ngSubmit)="save()">
        <h1>{{ isEdit() ? 'Edit category' : 'Create category' }}</h1>
        <label>Name<input formControlName="name" required /></label>
        <label>Slug<input formControlName="slug" /></label>
        <label>Description<textarea formControlName="description"></textarea></label>
        <label>Parent
          <select formControlName="parentCategoryId">
            <option [ngValue]="null">None</option>
            @for (category of categories; track category.id) {
              @if (!isEdit() || category.id !== numericId()) {
                <option [ngValue]="category.id">{{ category.name }}</option>
              }
            }
          </select>
        </label>
        <label><input type="checkbox" formControlName="published" /> Published</label>
        <label>Display order<input type="number" formControlName="displayOrder" /></label>
        <div class="actions">
          <button type="submit" [disabled]="form.invalid || saving">Save</button>
          <a routerLink="/catalog/categories">Cancel</a>
        </div>
      </form>
    }
  `,
  styles: [`
    form { display: grid; gap: 1rem; max-width: 40rem; background: #fff; padding: 1.5rem; border-radius: 0.5rem; }
    label { display: grid; gap: 0.375rem; }
    input, textarea, select { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .actions { display: flex; gap: 0.75rem; }
    button { padding: 0.625rem 1rem; border: none; background: #2563eb; color: #fff; border-radius: 0.375rem; }
  `]
})
export class CategoryFormPageComponent implements OnInit {
  readonly id = input<string | undefined>();
  private readonly catalogApi = inject(CatalogApi);
  private readonly facade = inject(CatalogAdminFacade);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  state: PageState = 'success';
  errorMessage = '';
  saving = false;
  categories: CategorySummary[] = [];

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    slug: [''],
    description: [''],
    parentCategoryId: this.fb.control<number | null>(null),
    published: [false],
    displayOrder: [0]
  });

  isEdit(): boolean {
    return !!this.id() && this.id() !== 'new';
  }

  numericId(): number {
    return Number(this.id());
  }

  ngOnInit(): void {
    void this.initialize();
  }

  async initialize(): Promise<void> {
    this.state = 'loading';
    try {
      this.categories = await this.facade.listCategories();
      if (this.isEdit()) {
        const category = await firstValueFrom(this.catalogApi.getCategory(this.numericId()));
        this.form.patchValue({
          name: category.name,
          slug: category.slug ?? '',
          description: category.description ?? '',
          parentCategoryId: category.parentCategoryId,
          published: category.published,
          displayOrder: category.displayOrder
        });
      }
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load category.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    if (this.form.invalid) return;
    this.saving = true;
    const value = this.form.getRawValue();
    try {
      const payload = {
        name: value.name,
        parentCategoryId: value.parentCategoryId,
        description: value.description || null,
        slug: value.slug || null,
        published: value.published,
        displayOrder: value.displayOrder
      };
      if (this.isEdit()) {
        await firstValueFrom(this.catalogApi.updateCategory(this.numericId(), payload));
      } else {
        await firstValueFrom(this.catalogApi.createCategory(payload));
      }
      await this.router.navigateByUrl('/catalog/categories');
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
    } finally {
      this.saving = false;
    }
  }
}
