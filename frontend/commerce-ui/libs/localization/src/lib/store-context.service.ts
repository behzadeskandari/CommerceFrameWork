import { Injectable, computed, inject, signal } from '@angular/core';
import { CurrencySummary, StoreApi, StoreContext } from '@commerce/api';
import { LocalizationService, SupportedLocale, TextDirection } from './localization.service';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class StoreContextService {
  private readonly storeApi = inject(StoreApi);
  private readonly localization = inject(LocalizationService);

  private readonly contextSignal = signal<StoreContext | null>(null);
  private readonly currenciesSignal = signal<CurrencySummary[]>([]);

  readonly context = this.contextSignal.asReadonly();
  readonly currencies = this.currenciesSignal.asReadonly();

  readonly currentStore = computed(() => this.contextSignal());
  readonly currentLanguageCode = computed(() => this.contextSignal()?.languageCode ?? 'en');
  readonly currentCurrencyCode = computed(() => this.contextSignal()?.currencyCode ?? 'USD');
  readonly direction = computed<TextDirection>(() =>
    this.contextSignal()?.isRtl ? 'rtl' : 'ltr'
  );

  readonly activeCurrency = computed(() => {
    const code = this.currentCurrencyCode();
    return this.currenciesSignal().find(currency => currency.code === code) ?? null;
  });

  async initialize(): Promise<void> {
    const [context, currencies] = await Promise.all([
      firstValueFrom(this.storeApi.getContext()),
      firstValueFrom(this.storeApi.listCurrencies())
    ]);

    this.contextSignal.set(context);
    this.currenciesSignal.set(currencies);
    this.applyLanguage(context);
  }

  async selectLanguage(languageCode: string): Promise<void> {
    await firstValueFrom(this.storeApi.selectLanguage(languageCode));
    await this.initialize();
  }

  formatAmount(amount: number, currencyCode?: string): string {
    const currency = this.currenciesSignal().find(item =>
      item.code === (currencyCode ?? this.currentCurrencyCode())
    );

    if (!currency) {
      return amount.toFixed(2);
    }

    const formatted = new Intl.NumberFormat(this.contextSignal()?.cultureCode ?? 'en-US', {
      minimumFractionDigits: currency.decimalPlaces,
      maximumFractionDigits: currency.decimalPlaces
    }).format(amount);

    if (currency.symbol && currency.symbol !== currency.code) {
      return `${formatted} ${currency.symbol}`.trim();
    }

    return `${formatted} ${currency.code}`.trim();
  }

  private applyLanguage(context: StoreContext): void {
    const locale = (context.languageCode === 'fa' ? 'fa' : 'en') as SupportedLocale;
    this.localization.setLocale(locale);
  }
}
