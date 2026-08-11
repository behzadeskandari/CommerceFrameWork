import { Pipe, PipeTransform, inject } from '@angular/core';
import { StoreContextService } from './store-context.service';

@Pipe({ name: 'currencyFormat', standalone: true })
export class CurrencyFormatPipe implements PipeTransform {
  private readonly storeContext = inject(StoreContextService);

  transform(amount: number | null | undefined, currencyCode?: string): string {
    if (amount === null || amount === undefined) {
      return '';
    }

    return this.storeContext.formatAmount(amount, currencyCode);
  }
}
