import { Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  CreateTaxZoneRequest,
  TaxPostalRuleType,
  TaxApi,
  TaxZonePostalRule,
  TaxZoneState,
  UpdateTaxZoneRequest
} from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

interface PostalRuleRow {
  countryCode: string;
  ruleType: TaxPostalRuleType;
  postalFrom: string;
  postalTo: string;
}

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe, BreadcrumbsComponent, LoadingStateComponent, ErrorStateComponent],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: ('tax.zones.title' | translate), link: '/tax/zones' },
      { label: isEdit ? form.name : ('action.create' | translate) }
    ]" />
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @default {
        <form class="form" (ngSubmit)="save()">
          <h1>{{ isEdit ? form.name : ('tax.zones.create' | translate) }}</h1>

          @if (!isEdit) {
            <label>{{ 'tax.zones.systemName' | translate }}
              <input [(ngModel)]="form.systemName" name="systemName" required />
            </label>
            <label>{{ 'tax.storeId' | translate }}
              <input type="number" [(ngModel)]="form.storeId" name="storeId" required />
            </label>
          }
          <label>{{ 'tax.zones.name' | translate }}
            <input [(ngModel)]="form.name" name="name" required />
          </label>
          <label>{{ 'tax.displayOrder' | translate }}
            <input type="number" [(ngModel)]="form.displayOrder" name="displayOrder" required />
          </label>
          <label class="checkbox">
            <input type="checkbox" [(ngModel)]="form.isDefault" name="isDefault" />
            {{ 'tax.zones.isDefault' | translate }}
          </label>
          <label class="checkbox">
            <input type="checkbox" [(ngModel)]="form.isActive" name="isActive" />
            {{ 'tax.active' | translate }}
          </label>
          <label>{{ 'tax.zones.countries' | translate }}
            <input [(ngModel)]="countriesCsv" name="countriesCsv" [placeholder]="'tax.zones.countriesHint' | translate" />
          </label>
          <label>{{ 'tax.zones.states' | translate }}
            <input [(ngModel)]="statesCsv" name="statesCsv" [placeholder]="'tax.zones.statesHint' | translate" />
          </label>

          <section class="postal-rules">
            <h2>{{ 'tax.zones.postalRules' | translate }}</h2>
            @for (rule of postalRules; track $index; let i = $index) {
              <div class="rule-row">
                <input [(ngModel)]="rule.countryCode" [name]="'ruleCountry-' + i" [placeholder]="'tax.zones.countryCode' | translate" />
                <select [(ngModel)]="rule.ruleType" [name]="'ruleType-' + i">
                  @for (type of ruleTypes; track type) {
                    <option [value]="type">{{ postalRuleLabel(type) | translate }}</option>
                  }
                </select>
                <input [(ngModel)]="rule.postalFrom" [name]="'ruleFrom-' + i" [placeholder]="'tax.zones.postalFrom' | translate" />
                <input [(ngModel)]="rule.postalTo" [name]="'ruleTo-' + i" [placeholder]="'tax.zones.postalTo' | translate" />
                <button type="button" (click)="removePostalRule(i)">{{ 'action.delete' | translate }}</button>
              </div>
            }
            <button type="button" class="secondary" (click)="addPostalRule()">{{ 'tax.zones.addPostalRule' | translate }}</button>
          </section>

          <div class="actions">
            <button type="submit" class="btn btn--primary">{{ 'action.save' | translate }}</button>
            <a routerLink="/tax/zones">{{ 'action.cancel' | translate }}</a>
          </div>
        </form>
      }
    }
  `,
  styles: [`
    .form { display: grid; gap: 0.75rem; max-width: 48rem; background: #fff; padding: 1rem; border-radius: 0.5rem; }
    label { display: grid; gap: 0.25rem; }
    label.checkbox { display: flex; align-items: center; gap: 0.5rem; }
    input, select, textarea { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .postal-rules { display: grid; gap: 0.5rem; margin-top: 0.5rem; }
    .rule-row { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: center; }
    .rule-row input, .rule-row select { flex: 1 1 120px; min-width: 0; }
    .rule-row button { background: none; border: none; color: #dc2626; cursor: pointer; }
    .actions { display: flex; gap: 0.75rem; align-items: center; margin-top: 0.5rem; }
    .btn { padding: 0.5rem 1rem; border-radius: 0.375rem; border: none; cursor: pointer; }
    .btn--primary { background: #2563eb; color: #fff; }
    button.secondary { width: fit-content; padding: 0.375rem 0.75rem; border: 1px solid #d1d5db; background: #fff; border-radius: 0.375rem; cursor: pointer; }
  `]
})
export class ZoneFormPageComponent implements OnInit {
  readonly id = input<number | undefined>();

  private readonly taxApi = inject(TaxApi);
  private readonly router = inject(Router);

  state: PageState = 'loading';
  errorMessage = '';
  isEdit = false;
  countriesCsv = '';
  statesCsv = '';
  readonly ruleTypes: TaxPostalRuleType[] = ['Exact', 'Prefix', 'Range'];
  postalRules: PostalRuleRow[] = [];

  form = {
    storeId: 1,
    systemName: '',
    name: '',
    isDefault: false,
    isActive: true,
    displayOrder: 0
  };

  ngOnInit(): void {
    void this.load();
  }

  postalRuleLabel(type: TaxPostalRuleType): string {
    const map: Record<TaxPostalRuleType, string> = {
      Exact: 'tax.zones.ruleExact',
      Prefix: 'tax.zones.rulePrefix',
      Range: 'tax.zones.ruleRange'
    };
    return map[type];
  }

  addPostalRule(): void {
    this.postalRules.push({ countryCode: '', ruleType: 'Exact', postalFrom: '', postalTo: '' });
  }

  removePostalRule(index: number): void {
    this.postalRules.splice(index, 1);
  }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const zoneId = this.id();
      if (zoneId) {
        this.isEdit = true;
        const detail = await firstValueFrom(this.taxApi.getZone(zoneId));
        this.form = {
          storeId: detail.storeId,
          systemName: detail.systemName,
          name: detail.name,
          isDefault: detail.isDefault,
          isActive: detail.isActive,
          displayOrder: detail.displayOrder
        };
        this.countriesCsv = detail.countries.map(c => c.countryCode).join(', ');
        this.statesCsv = detail.states.map(s => `${s.countryCode}:${s.stateProvince}`).join(', ');
        this.postalRules = detail.postalRules.map(r => ({
          countryCode: r.countryCode,
          ruleType: r.ruleType,
          postalFrom: r.postalFrom,
          postalTo: r.postalTo ?? ''
        }));
      }
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load tax zone.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    try {
      const countries = this.parseCountries(this.countriesCsv);
      const states = this.parseStates(this.statesCsv);
      const postalRules = this.buildPostalRules();

      if (this.isEdit && this.id()) {
        const request: UpdateTaxZoneRequest = {
          name: this.form.name,
          isDefault: this.form.isDefault,
          isActive: this.form.isActive,
          displayOrder: this.form.displayOrder,
          countries,
          states,
          postalRules
        };
        await firstValueFrom(this.taxApi.updateZone(this.id()!, request));
      } else {
        const request: CreateTaxZoneRequest = {
          storeId: this.form.storeId,
          name: this.form.name,
          systemName: this.form.systemName,
          isDefault: this.form.isDefault,
          isActive: this.form.isActive,
          displayOrder: this.form.displayOrder,
          countries,
          states,
          postalRules
        };
        await firstValueFrom(this.taxApi.createZone(request));
      }
      await this.router.navigateByUrl('/tax/zones');
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
      this.state = 'error';
    }
  }

  private parseCountries(csv: string) {
    return csv
      .split(',')
      .map(code => code.trim().toUpperCase())
      .filter(Boolean)
      .map(countryCode => ({ countryCode }));
  }

  private parseStates(csv: string): TaxZoneState[] {
    if (!csv.trim()) return [];
    return csv
      .split(',')
      .map(entry => entry.trim())
      .filter(Boolean)
      .map(entry => {
        const [countryCode, stateProvince] = entry.split(':').map(part => part.trim());
        return { countryCode: countryCode.toUpperCase(), stateProvince };
      })
      .filter(entry => entry.countryCode && entry.stateProvince);
  }

  private buildPostalRules(): TaxZonePostalRule[] {
    return this.postalRules
      .filter(rule => rule.countryCode.trim() && rule.postalFrom.trim())
      .map(rule => ({
        countryCode: rule.countryCode.trim().toUpperCase(),
        ruleType: rule.ruleType,
        postalFrom: rule.postalFrom.trim(),
        postalTo: rule.postalTo.trim() || null
      }));
  }
}
