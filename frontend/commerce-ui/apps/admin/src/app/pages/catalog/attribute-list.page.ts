import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AttributeDefinition, AttributeType, CatalogApi } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import {
  EmptyStateComponent,
  ErrorStateComponent,
  LoadingStateComponent,
  PageState
} from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TranslatePipe,
    BreadcrumbsComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent
  ],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('catalog.attributes.title' | translate) }
    ]" />
    <header class="page-header">
      <h1>{{ 'catalog.attributes.title' | translate }}</h1>
      @if (permissions.hasPermission('Catalog.Attributes.Create') && !showCreateForm) {
        <button type="button" class="btn btn--primary" (click)="openCreateForm()">Create attribute</button>
      }
    </header>

    @if (showCreateForm) {
      <section class="panel">
        <h2>{{ editingId ? 'Edit attribute' : 'New attribute' }}</h2>
        <form [formGroup]="definitionForm" (ngSubmit)="saveDefinition()">
          <label>Name<input formControlName="name" required /></label>
          <label>Code<input formControlName="code" [readonly]="!!editingId" required /></label>
          <label>Type
            <select formControlName="attributeType">
              @for (type of attributeTypes; track type) {
                <option [value]="type">{{ type }}</option>
              }
            </select>
          </label>
          <label>Display order<input type="number" formControlName="displayOrder" /></label>
          <label><input type="checkbox" formControlName="isActive" /> Active</label>
          <div class="actions">
            <button type="submit" [disabled]="definitionForm.invalid || saving">{{ 'action.save' | translate }}</button>
            <button type="button" (click)="cancelForm()">{{ 'action.cancel' | translate }}</button>
          </div>
        </form>
      </section>
    }

    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state messageKey="state.empty" /> }
      @default {
        @for (attribute of attributes; track attribute.id) {
          <section class="panel">
            <header class="attribute-header">
              <div>
                <strong>{{ attribute.name }}</strong>
                <span class="meta">{{ attribute.code }} · {{ attribute.attributeType }}</span>
              </div>
              <div class="actions">
                @if (permissions.hasPermission('Catalog.Attributes.Update')) {
                  <button type="button" (click)="editDefinition(attribute)">{{ 'action.edit' | translate }}</button>
                }
              </div>
            </header>

            @if (attribute.attributeType === 'Option') {
              <table>
                <thead>
                  <tr><th>Value</th><th>Order</th><th>Active</th><th>Actions</th></tr>
                </thead>
                <tbody>
                  @for (option of attribute.options; track option.id) {
                    <tr>
                      @if (editingOptionId === option.id) {
                        <td colspan="4">
                          <form [formGroup]="optionForm" (ngSubmit)="saveOption(attribute.id, option.id)" class="inline-form">
                            <input formControlName="value" required />
                            <input type="number" formControlName="displayOrder" />
                            <label><input type="checkbox" formControlName="isActive" /> Active</label>
                            <button type="submit" [disabled]="optionForm.invalid || saving">Save</button>
                            <button type="button" (click)="editingOptionId = null">Cancel</button>
                          </form>
                        </td>
                      } @else {
                        <td>{{ option.value }}</td>
                        <td>{{ option.displayOrder }}</td>
                        <td>{{ option.isActive ? 'Yes' : 'No' }}</td>
                        <td>
                          @if (permissions.hasPermission('Catalog.Attributes.Update')) {
                            <button type="button" (click)="editOption(option)">Edit</button>
                          }
                        </td>
                      }
                    </tr>
                  }
                </tbody>
              </table>

              @if (permissions.hasPermission('Catalog.Attributes.Create') && addingOptionForId !== attribute.id) {
                <button type="button" class="btn-link" (click)="startAddOption(attribute.id)">Add option</button>
              }
              @if (addingOptionForId === attribute.id) {
                <form [formGroup]="optionForm" (ngSubmit)="saveOption(attribute.id)" class="inline-form">
                  <input formControlName="value" placeholder="Option value" required />
                  <input type="number" formControlName="displayOrder" />
                  <label><input type="checkbox" formControlName="isActive" /> Active</label>
                  <button type="submit" [disabled]="optionForm.invalid || saving">Add</button>
                  <button type="button" (click)="addingOptionForId = null">Cancel</button>
                </form>
              }
            }
          </section>
        }
      }
    }
  `,
  styles: [`
    .page-header { display: flex; justify-content: space-between; align-items: center; gap: 1rem; flex-wrap: wrap; margin-bottom: 1rem; }
    .panel { background: #fff; border-radius: 0.5rem; padding: 1rem 1.25rem; margin-bottom: 1rem; border: 1px solid #e5e7eb; }
    .attribute-header { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; margin-bottom: 0.75rem; }
    .meta { display: block; color: #6b7280; font-size: 0.875rem; margin-top: 0.25rem; }
    form { display: grid; gap: 0.75rem; max-width: 32rem; }
    label { display: grid; gap: 0.375rem; }
    input, select { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .actions, .inline-form { display: flex; gap: 0.5rem; align-items: center; flex-wrap: wrap; }
    .btn, button[type="submit"] { padding: 0.5rem 1rem; border: none; background: #2563eb; color: #fff; border-radius: 0.375rem; cursor: pointer; }
    button[type="button"] { padding: 0.375rem 0.75rem; border: 1px solid #d1d5db; background: #fff; border-radius: 0.375rem; cursor: pointer; }
    .btn--primary { text-decoration: none; }
    .btn-link { background: none; border: none; color: #2563eb; cursor: pointer; padding: 0; margin-top: 0.5rem; }
    table { width: 100%; border-collapse: collapse; margin-top: 0.5rem; }
    th, td { padding: 0.5rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
  `]
})
export class AttributeListPageComponent implements OnInit {
  private readonly catalogApi = inject(CatalogApi);
  private readonly fb = inject(FormBuilder);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  saving = false;
  attributes: AttributeDefinition[] = [];
  showCreateForm = false;
  editingId: number | null = null;
  editingOptionId: number | null = null;
  addingOptionForId: number | null = null;
  attributeTypes: AttributeType[] = ['Text', 'Option', 'Boolean', 'Number'];

  readonly definitionForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    code: ['', Validators.required],
    attributeType: ['Option' as AttributeType, Validators.required],
    displayOrder: [0],
    isActive: [true]
  });

  readonly optionForm = this.fb.nonNullable.group({
    value: ['', Validators.required],
    displayOrder: [0],
    isActive: [true]
  });

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.attributes = await firstValueFrom(this.catalogApi.listAttributes(true));
      this.state = this.attributes.length ? 'success' : 'empty';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load attributes.';
      this.state = 'error';
    }
  }

  openCreateForm(): void {
    this.editingId = null;
    this.definitionForm.reset({ name: '', code: '', attributeType: 'Option', displayOrder: 0, isActive: true });
    this.showCreateForm = true;
  }

  editDefinition(attribute: AttributeDefinition): void {
    this.editingId = attribute.id;
    this.definitionForm.patchValue({
      name: attribute.name,
      code: attribute.code,
      attributeType: attribute.attributeType,
      displayOrder: attribute.displayOrder,
      isActive: attribute.isActive
    });
    this.showCreateForm = true;
  }

  cancelForm(): void {
    this.showCreateForm = false;
    this.editingId = null;
  }

  async saveDefinition(): Promise<void> {
    if (this.definitionForm.invalid) return;
    this.saving = true;
    this.errorMessage = '';
    const value = this.definitionForm.getRawValue();
    try {
      if (this.editingId) {
        await firstValueFrom(this.catalogApi.updateAttribute(this.editingId, {
          name: value.name,
          attributeType: value.attributeType,
          displayOrder: value.displayOrder,
          isActive: value.isActive
        }));
      } else {
        await firstValueFrom(this.catalogApi.createAttribute({
          name: value.name,
          code: value.code,
          attributeType: value.attributeType,
          displayOrder: value.displayOrder,
          isActive: value.isActive
        }));
      }
      this.showCreateForm = false;
      this.editingId = null;
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
    } finally {
      this.saving = false;
    }
  }

  startAddOption(attributeId: number): void {
    this.addingOptionForId = attributeId;
    this.editingOptionId = null;
    this.optionForm.reset({ value: '', displayOrder: 0, isActive: true });
  }

  editOption(option: { id: number; value: string; displayOrder: number; isActive: boolean }): void {
    this.editingOptionId = option.id;
    this.addingOptionForId = null;
    this.optionForm.patchValue({
      value: option.value,
      displayOrder: option.displayOrder,
      isActive: option.isActive
    });
  }

  async saveOption(attributeId: number, optionId?: number): Promise<void> {
    if (this.optionForm.invalid) return;
    this.saving = true;
    const value = this.optionForm.getRawValue();
    try {
      if (optionId) {
        await firstValueFrom(this.catalogApi.updateAttributeOption(optionId, value));
      } else {
        await firstValueFrom(this.catalogApi.createAttributeOption(attributeId, value));
      }
      this.editingOptionId = null;
      this.addingOptionForId = null;
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save option failed.';
    } finally {
      this.saving = false;
    }
  }
}
