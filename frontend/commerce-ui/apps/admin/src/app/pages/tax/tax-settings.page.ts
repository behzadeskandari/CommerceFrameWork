import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { AdvancedPricingApi, TaxSettings } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, LoadingStateComponent, ErrorStateComponent],
  template: `
    <h1>{{ 'tax.settings' | translate }}</h1>
    @switch (state) {
      @case ('loading') { <cmr-loading-state /> }
      @case ('error') { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
      @case ('success') {
        <form [formGroup]="form" (ngSubmit)="save()">
          <label><input type="checkbox" formControlName="enabled" /> {{ 'tax.enabled' | translate }}</label>
          <label><input type="checkbox" formControlName="pricesIncludeTax" /> {{ 'tax.pricesIncludeTax' | translate }}</label>
          <label><input type="checkbox" formControlName="shippingTaxableByDefault" /> {{ 'tax.shippingTaxable' | translate }}</label>
          <label>{{ 'tax.defaultCategoryId' | translate }}<input type="number" formControlName="defaultCategoryId" /></label>
          <button type="submit">{{ 'action.save' | translate }}</button>
        </form>
      }
    }
  `,
  styles: [`form { display: grid; gap: 1rem; max-width: 28rem; } label { display: flex; gap: 0.5rem; align-items: center; }`]
})
export class TaxSettingsPageComponent implements OnInit {
  private readonly api = inject(AdvancedPricingApi);
  private readonly fb = inject(FormBuilder);
  state: PageState = 'loading';
  errorMessage = '';
  readonly form = this.fb.group({
    enabled: [true],
    pricesIncludeTax: [false],
    shippingTaxableByDefault: [true],
    defaultCategoryId: [null as number | null]
  });

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.state = 'loading';
    try {
      const settings = await firstValueFrom(this.api.getTaxSettings());
      this.form.patchValue(settings);
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load tax settings.';
      this.state = 'error';
    }
  }

  async save(): Promise<void> {
    try {
      const value = this.form.getRawValue();
      await firstValueFrom(this.api.saveTaxSettings({
        enabled: !!value.enabled,
        pricesIncludeTax: !!value.pricesIncludeTax,
        shippingTaxableByDefault: !!value.shippingTaxableByDefault,
        defaultCategoryId: value.defaultCategoryId
      }));
      await this.load();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to save tax settings.';
      this.state = 'error';
    }
  }
}
