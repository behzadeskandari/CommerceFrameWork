import { Component, OnInit, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ResolvedPrice, StorefrontProductDetail, StorefrontVariant, CartStateService } from '@commerce/api';
import { ApiClientError } from '@commerce/core';
import { CurrencyFormatPipe, TranslatePipe } from '@commerce/localization';
import { EmptyStateComponent, ErrorStateComponent, LoadingStateComponent, PageState } from '@commerce/shared';
import { StorefrontCatalogFacade } from '../services/storefront-catalog.facade';

@Component({
  standalone: true,
  imports: [FormsModule, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent, CurrencyFormatPipe, TranslatePipe],
  template: `
    @if (state === 'loading') { <cmr-loading-state /> }
    @else if (!product) { <cmr-empty-state messageKey="state.empty" /> }
    @else {
      <article class="product-detail">
        @if (displayImage()) {
          <img class="hero" [src]="displayImage()!.thumbnailUrl || displayImage()!.url" [alt]="displayImage()!.altText || product.name" />
        }
        <h1>{{ product.name }}</h1>
        <p class="sku">SKU: {{ selectedVariant()?.sku ?? product.sku }}</p>

        @if (price) {
          <p class="price">
            <strong>{{ price.unitPrice | currencyFormat: price.currencyCode }}</strong>
            @if (price.compareAtPrice) {
              <span class="compare-at">{{ price.compareAtPrice | currencyFormat: price.currencyCode }}</span>
            }
          </p>
          @if (availabilityLabel()) {
            <p class="availability" role="status">{{ availabilityLabel() | translate }}</p>
          }
        }

        @if (isVariantProduct()) {
          @for (attribute of product.configurableAttributes; track attribute.attributeDefinitionId) {
            <label class="selector">
              {{ attribute.name }}
              <select
                [ngModel]="selectedOptions[attribute.attributeDefinitionId]"
                (ngModelChange)="onOptionChange(attribute.attributeDefinitionId, $event)">
                @for (option of attribute.options; track option.id) {
                  <option [value]="option.id">{{ option.value }}</option>
                }
              </select>
            </label>
          }
        }

        @if (product.shortDescription) { <p>{{ product.shortDescription }}</p> }
        @if (product.description) { <div [innerHTML]="product.description"></div> }

        <button
          type="button"
          class="add-to-cart"
          [disabled]="!canAddToCart() || adding"
          (click)="addToCart()">
          {{ addSuccess ? ('cart.added' | translate) : ('cart.addToCart' | translate) }}
        </button>
        @if (!canAddToCart() && product) {
          <p class="unavailable" role="status">{{ 'cart.unavailable' | translate }}</p>
        }
      </article>
    }
    @if (errorMessage) { <cmr-error-state [message]="errorMessage" (retry)="load()" /> }
  `,
  styles: [`
    .product-detail { display: grid; gap: 1rem; max-width: 40rem; }
    .hero { width: 100%; max-height: 360px; object-fit: contain; border-radius: 0.5rem; background: #f9fafb; }
    .sku { color: #6b7280; }
    .price { font-size: 1.25rem; display: flex; gap: 0.75rem; align-items: baseline; }
    .compare-at { color: #9ca3af; text-decoration: line-through; font-size: 1rem; }
    .availability { font-weight: 600; margin: 0; }
    .availability.in-stock { color: #047857; }
    .availability.out-of-stock { color: #b91c1c; }
    .availability.backorder { color: #b45309; }
    .selector { display: grid; gap: 0.375rem; max-width: 16rem; }
    select { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .add-to-cart {
      padding: 0.75rem 1.25rem; border: none; border-radius: 0.375rem;
      background: var(--primary, #0f766e); color: #fff; cursor: pointer; width: fit-content;
    }
    .add-to-cart:disabled { background: #9ca3af; cursor: not-allowed; }
    .unavailable { color: #b91c1c; margin: 0; }
  `]
})
export class ProductDetailPageComponent implements OnInit {
  readonly slug = input.required<string>();
  private readonly catalog = inject(StorefrontCatalogFacade);
  private readonly cart = inject(CartStateService);

  state: PageState = 'loading';
  errorMessage = '';
  product: StorefrontProductDetail | null = null;
  price: ResolvedPrice | null = null;
  selectedOptions: Record<number, number> = {};
  adding = false;
  addSuccess = false;

  ngOnInit(): void {
    void this.load();
  }

  isVariantProduct(): boolean {
    return this.product?.productType === 'Variant';
  }

  selectedVariant(): StorefrontVariant | null {
    if (!this.product?.variants.length) {
      return null;
    }

    if (!this.isVariantProduct()) {
      return this.product.variants.find(variant => variant.isDefault) ?? this.product.variants[0] ?? null;
    }

    const selectedOptionIds = Object.values(this.selectedOptions).sort((a, b) => a - b);
    return this.product.variants.find(variant => {
      const variantOptionIds = variant.options.map(option => option.id).sort((a, b) => a - b);
      return variantOptionIds.length === selectedOptionIds.length &&
        variantOptionIds.every((id, index) => id === selectedOptionIds[index]);
    }) ?? null;
  }

  async load(): Promise<void> {
    this.state = 'loading';
    this.errorMessage = '';
    try {
      this.product = await this.catalog.findProductBySlug(this.slug());
      if (!this.product) {
        this.state = 'empty';
        return;
      }

      this.initializeSelections();
      this.price = this.product.price;
      if (this.isVariantProduct() && this.selectedVariant()) {
        await this.resolveSelectedPrice();
      }
      this.state = 'success';
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load product.';
      this.state = 'error';
    }
  }

  initializeSelections(): void {
    if (!this.product) return;

    this.selectedOptions = {};
    for (const attribute of this.product.configurableAttributes) {
      const defaultVariant = this.product.variants.find(variant =>
        variant.id === this.product!.defaultVariantId
      ) ?? this.product.variants[0];

      const defaultOption = defaultVariant?.options.find(option =>
        this.product!.configurableAttributes.some(item =>
          item.attributeDefinitionId === attribute.attributeDefinitionId &&
          item.options.some(candidate => candidate.id === option.id)
        )
      ) ?? attribute.options[0];

      if (defaultOption) {
        this.selectedOptions[attribute.attributeDefinitionId] = defaultOption.id;
      }
    }
  }

  async onOptionChange(attributeDefinitionId: number, optionId: number): Promise<void> {
    this.selectedOptions[attributeDefinitionId] = Number(optionId);
    await this.resolveSelectedPrice();
  }

  async resolveSelectedPrice(): Promise<void> {
    const variant = this.selectedVariant();
    if (!this.product) return;

    try {
      if (variant) {
        this.price = await this.catalog.resolveVariantPrice(variant.id);
      } else {
        this.price = await this.catalog.resolveProductPrice(this.product.id);
      }
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to resolve price.';
    }
  }

  displayImage() {
    const variant = this.selectedVariant();
    if (variant?.image) {
      return variant.image;
    }

    return this.product?.primaryImage ?? this.product?.gallery?.[0] ?? null;
  }

  canAddToCart(): boolean {
    if (!(this.price?.offerId ?? 0)) return false;
    return this.price?.availability?.canPurchase ?? true;
  }

  availabilityLabel(): string | null {
    const status = this.price?.availability?.status;
    if (!status || status === 'NotTracked') return null;
    switch (status) {
      case 'InStock': return 'product.inStock';
      case 'OutOfStock': return 'product.outOfStock';
      case 'Backorder': return 'product.backorder';
      default: return null;
    }
  }

  async addToCart(): Promise<void> {
    if (!this.price?.offerId) return;
    this.adding = true;
    this.addSuccess = false;
    this.errorMessage = '';
    try {
      await this.cart.addItem(this.price.offerId, 1);
      this.addSuccess = true;
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to add to cart.';
    } finally {
      this.adding = false;
    }
  }
}
