import { Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CreateStoreRequest, StoreApi, UpdateStoreRequest } from '@commerce/api';
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
      { label: ('nav.stores' | translate), link: '/stores' },
      { label: isEdit ? form.name : ('action.create' | translate) }
    ]" />
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="form" (ngSubmit)="save()">
          <h1>{{ isEdit ? form.name : ('nav.stores' | translate) }}</h1>
          @if (!isEdit) {
            <label>System name<input [(ngModel)]="form.systemName" name="systemName" required /></label>
          }
          <label>Name<input [(ngModel)]="form.name" name="name" required /></label>
          <label>URL<input [(ngModel)]="form.url" name="url" required /></label>
          <label>Default language
            <select [(ngModel)]="form.defaultLanguageId" name="defaultLanguageId" required>
              @for (language of languages; track language.id) {
                <option [ngValue]="language.id">{{ language.name }}</option>
              }
            </select>
          </label>
          <label>Default currency
            <select [(ngModel)]="form.defaultCurrencyId" name="defaultCurrencyId" required>
              @for (currency of currencies; track currency.id) {
                <option [ngValue]="currency.id">{{ currency.code }} — {{ currency.name }}</option>
              }
            </select>
          </label>
          <label><input type="checkbox" [(ngModel)]="form.isActive" name="isActive" /> Active</label>
          @if (!isEdit) {
            <fieldset>
              <legend>Primary domain</legend>
              <label>Host<input [(ngModel)]="primaryHost" name="primaryHost" placeholder="localhost" /></label>
            </fieldset>
          } @else if (domains.length) {
            <section>
              <h2>Domains</h2>
              <ul>@for (domain of domains; track domain.id) { <li>{{ domain.scheme }}://{{ domain.host }}{{ domain.port ? ':' + domain.port : '' }}</li> }</ul>
            </section>
          }
          <div class="actions">
            <button type="submit" class="btn btn--primary">{{ 'action.save' | translate }}</button>
            <a routerLink="/stores">{{ 'action.cancel' | translate }}</a>
          </div>
        </form>
      }
    }
  `,
  styles: [`
    .form { display: grid; gap: 0.75rem; max-width: 36rem; background: #fff; padding: 1rem; border-radius: 0.5rem; }
    label { display: grid; gap: 0.25rem; }
    input, select { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .actions { display: flex; gap: 0.75rem; align-items: center; }
    .btn { padding: 0.5rem 1rem; border-radius: 0.375rem; border: none; cursor: pointer; }
    .btn--primary { background: #2563eb; color: #fff; }
  `]
})
export class StoreFormPageComponent implements OnInit {
  readonly id = input<number | undefined>();

  private readonly storeApi = inject(StoreApi);
  private readonly router = inject(Router);

  state: PageState = 'loading';
  errorMessage = '';
  isEdit = false;
  languages: Array<{ id: number; name: string }> = [];
  currencies: Array<{ id: number; code: string; name: string }> = [];
  domains: Array<{ id: number; host: string; scheme: string; port: number | null }> = [];
  primaryHost = 'localhost';

  form = {
    systemName: '',
    name: '',
    url: 'https://localhost:5100',
    defaultLanguageId: 0,
    defaultCurrencyId: 0,
    displayOrder: 0,
    isActive: true
  };

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const [languages, currencies] = await Promise.all([
        firstValueFrom(this.storeApi.listLanguages()),
        firstValueFrom(this.storeApi.listCurrencies())
      ]);
      this.languages = languages;
      this.currencies = currencies;

      const storeId = this.id();
      if (storeId) {
        this.isEdit = true;
        const store = await firstValueFrom(this.storeApi.getStore(storeId));
        this.form = {
          systemName: store.systemName,
          name: store.name,
          url: store.url,
          defaultLanguageId: store.defaultLanguageId,
          defaultCurrencyId: store.defaultCurrencyId,
          displayOrder: store.displayOrder,
          isActive: store.isActive
        };
        this.domains = store.domains;
      } else if (languages.length && currencies.length) {
        this.form.defaultLanguageId = languages[0].id;
        this.form.defaultCurrencyId = currencies[0].id;
      }

      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load store.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    try {
      if (this.isEdit && this.id()) {
        const request: UpdateStoreRequest = {
          name: this.form.name,
          url: this.form.url,
          defaultLanguageId: this.form.defaultLanguageId,
          defaultCurrencyId: this.form.defaultCurrencyId,
          displayOrder: this.form.displayOrder,
          isActive: this.form.isActive
        };
        await firstValueFrom(this.storeApi.updateStore(this.id()!, request));
      } else {
        const request: CreateStoreRequest = {
          systemName: this.form.systemName,
          name: this.form.name,
          url: this.form.url,
          defaultLanguageId: this.form.defaultLanguageId,
          defaultCurrencyId: this.form.defaultCurrencyId,
          displayOrder: this.form.displayOrder,
          isActive: this.form.isActive,
          domains: this.primaryHost ? [{
            host: this.primaryHost,
            scheme: 'https',
            port: 5100,
            isPrimary: true,
            isSslRequired: true
          }] : undefined
        };
        await firstValueFrom(this.storeApi.createStore(request));
      }
      await this.router.navigateByUrl('/stores');
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
      this.state = 'error';
    }
  }
}
