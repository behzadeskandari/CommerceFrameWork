import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LanguageSummary, StoreApi } from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { EmptyStateComponent, ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [FormsModule, TranslatePipe, BreadcrumbsComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[{ label: 'Dashboard', link: '/dashboard' }, { label: ('nav.languages' | translate) }]" />
    <header class="page-header">
      <h1>{{ 'nav.languages' | translate }}</h1>
    </header>
    @if (permissions.hasPermission('Languages.Create')) {
      <form class="inline-form" (ngSubmit)="create()">
        <input [(ngModel)]="newLanguage.name" name="name" placeholder="Name" required />
        <input [(ngModel)]="newLanguage.languageCode" name="languageCode" placeholder="Code (en)" required />
        <input [(ngModel)]="newLanguage.cultureCode" name="cultureCode" placeholder="Culture (en-US)" required />
        <label><input type="checkbox" [(ngModel)]="newLanguage.isRtl" name="isRtl" /> RTL</label>
        <button type="submit">{{ 'action.create' | translate }}</button>
      </form>
    }
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('empty') { <cmr-empty-state messageKey="state.empty" /> }
      @default {
        <table>
          <thead><tr><th>Name</th><th>Code</th><th>Culture</th><th>RTL</th><th>Active</th><th></th></tr></thead>
          <tbody>
            @for (language of languages; track language.id) {
              <tr>
                <td>{{ language.nativeName || language.name }}</td>
                <td>{{ language.languageCode }}</td>
                <td>{{ language.cultureCode }}</td>
                <td>{{ language.isRtl ? 'Yes' : 'No' }}</td>
                <td>{{ language.isActive ? 'Yes' : 'No' }}</td>
                <td>
                  @if (permissions.hasPermission('Languages.Update') && editingId !== language.id) {
                    <button type="button" (click)="startEdit(language)">{{ 'action.edit' | translate }}</button>
                  }
                </td>
              </tr>
              @if (editingId === language.id) {
                <tr>
                  <td colspan="6">
                    <form class="inline-form" (ngSubmit)="saveEdit(language.id)">
                      <input [(ngModel)]="editForm.name" name="editName" />
                      <input [(ngModel)]="editForm.cultureCode" name="editCulture" />
                      <label><input type="checkbox" [(ngModel)]="editForm.isRtl" name="editRtl" /> RTL</label>
                      <label><input type="checkbox" [(ngModel)]="editForm.isActive" name="editActive" /> Active</label>
                      <button type="submit">{{ 'action.save' | translate }}</button>
                      <button type="button" (click)="editingId = null">{{ 'action.cancel' | translate }}</button>
                    </form>
                  </td>
                </tr>
              }
            }
          </tbody>
        </table>
      }
    }
  `,
  styles: [`
    .page-header { margin-block-end: 1rem; }
    .inline-form { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-block-end: 1rem; align-items: center; }
    input, button { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 0.5rem; overflow: hidden; }
    th, td { padding: 0.75rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
  `]
})
export class LanguageListPageComponent implements OnInit {
  private readonly storeApi = inject(StoreApi);
  readonly permissions = inject(PermissionService);

  state: PageState = 'loading';
  errorMessage = '';
  languages: LanguageSummary[] = [];
  editingId: number | null = null;
  newLanguage = { name: '', languageCode: '', cultureCode: '', isRtl: false };
  editForm = { name: '', cultureCode: '', nativeName: '', isRtl: false, displayOrder: 0, isActive: true };

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      this.languages = await firstValueFrom(this.storeApi.listLanguages());
      this.state = this.languages.length ? 'success' : 'empty';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load languages.';
      this.state = 'error';
    }
  }

  async create(): Promise<void> {
    await firstValueFrom(this.storeApi.createLanguage({ ...this.newLanguage, nativeName: this.newLanguage.name }));
    this.newLanguage = { name: '', languageCode: '', cultureCode: '', isRtl: false };
    await this.load();
  }

  startEdit(language: LanguageSummary): void {
    this.editingId = language.id;
    this.editForm = {
      name: language.name,
      cultureCode: language.cultureCode,
      nativeName: language.nativeName,
      isRtl: language.isRtl,
      displayOrder: language.displayOrder,
      isActive: language.isActive
    };
  }

  async saveEdit(id: number): Promise<void> {
    await firstValueFrom(this.storeApi.updateLanguage(id, this.editForm));
    this.editingId = null;
    await this.load();
  }
}
