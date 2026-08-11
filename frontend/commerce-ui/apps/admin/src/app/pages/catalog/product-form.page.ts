import { Component, OnInit, inject, input } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  CatalogApi,
  CurrencySummary,
  MediaApiService,
  MediaSummary,
  OfferSummary,
  ProductMediaSummary,
  ProductType,
  StoreApi,
  StoreSummary,
  VariantSummary
} from '@commerce/api';
import { PermissionService } from '@commerce/auth';
import { ApiClientError } from '@commerce/core';
import { BreadcrumbsComponent } from '@commerce/layout';
import { TranslatePipe } from '@commerce/localization';
import { ErrorStateComponent, LoadingStateComponent, MediaPickerComponent, PageState } from '@commerce/shared';
import { firstValueFrom } from 'rxjs';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, BreadcrumbsComponent, LoadingStateComponent, ErrorStateComponent, TranslatePipe, MediaPickerComponent],
  template: `
    <cmr-breadcrumbs [items]="[
      { label: 'Dashboard', link: '/dashboard' },
      { label: 'Products', link: '/catalog/products' },
      { label: isEdit() ? 'Edit' : 'New' }
    ]" />
    @if (state === 'loading') { <cmr-loading-state /> } @else {
      @if (errorMessage) { <cmr-error-state [message]="errorMessage" [retryLabel]="''" /> }
      <form [formGroup]="form" (ngSubmit)="save()">
        <h1>{{ isEdit() ? 'Edit product' : 'Create product' }}</h1>
        <label>Name<input formControlName="name" required /></label>
        <label>SKU<input formControlName="sku" [readonly]="isEdit()" required /></label>
        <label>Slug<input formControlName="slug" /></label>
        <label>Type
          <select formControlName="productType">
            @for (type of productTypes; track type) { <option [value]="type">{{ type }}</option> }
          </select>
        </label>
        <label>Short description<textarea formControlName="shortDescription"></textarea></label>
        <label>Description<textarea formControlName="description"></textarea></label>
        <label><input type="checkbox" formControlName="published" /> Published</label>
        <label><input type="checkbox" formControlName="isVisible" /> Visible</label>
        <label><input type="checkbox" formControlName="isAvailable" /> Available</label>
        <label>Display order<input type="number" formControlName="displayOrder" /></label>
        <div class="actions">
          <button type="submit" [disabled]="form.invalid || saving">{{ 'action.save' | translate }}</button>
          <a routerLink="/catalog/products">Cancel</a>
        </div>
      </form>

      @if (isEdit() && isVariantProduct()) {
        <section class="panel">
          <h2>Variants</h2>
          @if (permissions.hasPermission('Catalog.Variants.Create')) {
            <form [formGroup]="generateForm" (ngSubmit)="generateVariants()" class="inline-form">
              <label>SKU prefix<input formControlName="skuPrefix" required /></label>
              <label><input type="checkbox" formControlName="skipExisting" /> Skip existing</label>
              <button type="submit" [disabled]="generateForm.invalid || variantsLoading">Generate</button>
            </form>
          }
          @if (variantsLoading) { <p>Loading variants…</p> }
          @else if (variants.length) {
            <table>
              <thead>
                <tr><th>SKU</th><th>Name</th><th>Default</th><th>Active</th><th>Actions</th></tr>
              </thead>
              <tbody>
                @for (variant of variants; track variant.id) {
                  <tr>
                    @if (editingVariantId === variant.id) {
                      <td>{{ variant.sku }}</td>
                      <td colspan="4">
                        <form [formGroup]="variantEditForm" (ngSubmit)="saveVariant(variant)" class="inline-form">
                          <input formControlName="name" required />
                          <button type="submit" [disabled]="variantEditForm.invalid">Save</button>
                          <button type="button" (click)="editingVariantId = null">Cancel</button>
                        </form>
                      </td>
                    } @else {
                      <td>{{ variant.sku }}</td>
                      <td>{{ variant.name }}</td>
                      <td>{{ variant.isDefault ? 'Yes' : 'No' }}</td>
                      <td>{{ variant.isActive ? 'Yes' : 'No' }}</td>
                      <td>
                        @if (permissions.hasPermission('Catalog.Variants.Update')) {
                          <button type="button" (click)="editVariant(variant)">Edit</button>
                        }
                      </td>
                    }
                  </tr>
                }
              </tbody>
            </table>
          } @else {
            <p>No variants yet. Assign option attributes and generate variants.</p>
          }
        </section>
      }

      @if (isEdit()) {
        <section class="panel">
          <h2>Images</h2>
          @if (permissions.hasPermission('Catalog.Products.Update')) {
            <cmr-media-picker (picked)="assignImage($event, 'Gallery')" />
          }
          @if (productMedia.length) {
            <ul class="media-list">
              @for (media of productMedia; track media.mediaAssetId) {
                <li>
                  <img [src]="media.thumbnailUrl || media.url" [alt]="media.altText || ''" />
                  <span>{{ media.role }}</span>
                  @if (permissions.hasPermission('Catalog.Products.Update')) {
                    <button type="button" (click)="removeImage(media.mediaAssetId)">Remove</button>
                  }
                </li>
              }
            </ul>
          }
        </section>
      }

      @if (isEdit()) {
        <section class="panel">
          <h2>Offers</h2>
          @if (permissions.hasPermission('Catalog.Offers.Create')) {
            <form [formGroup]="offerForm" (ngSubmit)="createOffer()" class="offer-form">
              <label>Store
                <select formControlName="storeId">
                  @for (store of stores; track store.id) {
                    <option [value]="store.id">{{ store.name }}</option>
                  }
                </select>
              </label>
              <label>Currency
                <select formControlName="currencyId" (change)="onCurrencyChange()">
                  @for (currency of currencies; track currency.id) {
                    <option [value]="currency.id">{{ currency.code }} — {{ currency.name }}</option>
                  }
                </select>
              </label>
              <label>Price<input type="number" step="0.01" formControlName="price" required /></label>
              <label>Compare at<input type="number" step="0.01" formControlName="compareAtPrice" /></label>
              <button type="submit" [disabled]="offerForm.invalid || offersLoading">Create offer</button>
            </form>
          }
          @if (offersLoading) { <p>Loading offers…</p> }
          @else if (offers.length) {
            <table>
              <thead>
                <tr><th>Store</th><th>Currency</th><th>Price</th><th>Compare at</th><th>Active</th></tr>
              </thead>
              <tbody>
                @for (offer of offers; track offer.id) {
                  <tr>
                    <td>{{ storeName(offer.storeId) }}</td>
                    <td>{{ offer.currencyCode }}</td>
                    <td>{{ offer.price }}</td>
                    <td>{{ offer.compareAtPrice ?? '—' }}</td>
                    <td>{{ offer.isActive ? 'Yes' : 'No' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          } @else {
            <p>No offers yet.</p>
          }
        </section>
      }
    }
  `,
  styles: [`
    form { display: grid; gap: 1rem; max-width: 40rem; background: #fff; padding: 1.5rem; border-radius: 0.5rem; margin-bottom: 1rem; }
    .panel { background: #fff; padding: 1.5rem; border-radius: 0.5rem; margin-bottom: 1rem; border: 1px solid #e5e7eb; }
    label { display: grid; gap: 0.375rem; }
    input, textarea, select { padding: 0.5rem 0.75rem; border: 1px solid #d1d5db; border-radius: 0.375rem; }
    .actions, .inline-form { display: flex; gap: 0.75rem; align-items: center; flex-wrap: wrap; }
    .offer-form { max-width: none; background: transparent; padding: 0; margin-bottom: 1rem; }
    button { padding: 0.625rem 1rem; border: none; background: #2563eb; color: #fff; border-radius: 0.375rem; cursor: pointer; }
    button[type="button"] { background: #fff; color: inherit; border: 1px solid #d1d5db; }
    table { width: 100%; border-collapse: collapse; margin-top: 0.75rem; }
    th, td { padding: 0.5rem; border-bottom: 1px solid #e5e7eb; text-align: start; }
    .media-list { list-style: none; padding: 0; display: flex; gap: 0.75rem; flex-wrap: wrap; margin-top: 1rem; }
    .media-list li { display: grid; gap: 0.25rem; width: 96px; }
    .media-list img { width: 96px; height: 72px; object-fit: cover; border-radius: 0.375rem; }
  `]
})
export class ProductFormPageComponent implements OnInit {
  readonly id = input<string | undefined>();
  private readonly catalogApi = inject(CatalogApi);
  private readonly mediaApi = inject(MediaApiService);
  private readonly storeApi = inject(StoreApi);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  readonly permissions = inject(PermissionService);

  state: PageState = 'success';
  errorMessage = '';
  saving = false;
  productTypes: ProductType[] = ['Simple', 'Grouped', 'Digital', 'Downloadable', 'Virtual', 'Variant', 'Bundle'];

  variants: VariantSummary[] = [];
  variantsLoading = false;
  editingVariantId: number | null = null;

  offers: OfferSummary[] = [];
  offersLoading = false;
  productMedia: ProductMediaSummary[] = [];
  stores: StoreSummary[] = [];
  currencies: CurrencySummary[] = [];

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    sku: ['', Validators.required],
    slug: [''],
    productType: ['Simple' as ProductType, Validators.required],
    shortDescription: [''],
    description: [''],
    published: [false],
    isVisible: [true],
    isAvailable: [true],
    displayOrder: [0]
  });

  readonly generateForm = this.fb.nonNullable.group({
    skuPrefix: ['', Validators.required],
    skipExisting: [true]
  });

  readonly variantEditForm = this.fb.nonNullable.group({
    name: ['', Validators.required]
  });

  readonly offerForm = this.fb.nonNullable.group({
    storeId: [0, Validators.required],
    currencyId: [0, Validators.required],
    currencyCode: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0)]],
    compareAtPrice: [null as number | null]
  });

  isEdit(): boolean {
    return !!this.id() && this.id() !== 'new';
  }

  isVariantProduct(): boolean {
    return this.form.controls.productType.value === 'Variant';
  }

  ngOnInit(): void {
    if (this.isEdit()) {
      void this.load(Number(this.id()));
    }
  }

  async load(id: number): Promise<void> {
    this.state = 'loading';
    try {
      const product = await firstValueFrom(this.catalogApi.getProduct(id));
      this.form.patchValue({
        name: product.name,
        sku: product.sku,
        slug: product.slug ?? '',
        productType: product.productType,
        shortDescription: product.shortDescription ?? '',
        description: product.description ?? '',
        published: product.published,
        isVisible: product.isVisible,
        isAvailable: product.isAvailable,
        displayOrder: product.displayOrder
      });
      this.state = 'success';
      if (product.productType === 'Variant') {
        void this.loadVariants(id);
      }
      void this.loadOffers(id);
      void this.loadProductMedia(id);
      void this.loadOfferFormData();
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load product.';
      this.state = 'error';
    }
  }

  async loadVariants(productId: number): Promise<void> {
    this.variantsLoading = true;
    try {
      this.variants = await firstValueFrom(this.catalogApi.listProductVariants(productId, true));
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load variants.';
    } finally {
      this.variantsLoading = false;
    }
  }

  async loadOffers(productId: number): Promise<void> {
    this.offersLoading = true;
    try {
      this.offers = await firstValueFrom(this.catalogApi.listOffersForProduct(productId));
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load offers.';
    } finally {
      this.offersLoading = false;
    }
  }

  async loadOfferFormData(): Promise<void> {
    try {
      [this.stores, this.currencies] = await Promise.all([
        firstValueFrom(this.storeApi.listStores()),
        firstValueFrom(this.storeApi.listCurrencies())
      ]);
      if (this.stores.length) {
        this.offerForm.patchValue({ storeId: this.stores[0].id });
      }
      if (this.currencies.length) {
        this.offerForm.patchValue({
          currencyId: this.currencies[0].id,
          currencyCode: this.currencies[0].code
        });
      }
    } catch {
      // Offer form defaults are optional for product save
    }
  }

  onCurrencyChange(): void {
    const currencyId = Number(this.offerForm.controls.currencyId.value);
    const currency = this.currencies.find(item => item.id === currencyId);
    if (currency) {
      this.offerForm.patchValue({ currencyCode: currency.code });
    }
  }

  storeName(storeId: number): string {
    return this.stores.find(store => store.id === storeId)?.name ?? String(storeId);
  }

  editVariant(variant: VariantSummary): void {
    this.editingVariantId = variant.id;
    this.variantEditForm.patchValue({ name: variant.name });
  }

  async saveVariant(variant: VariantSummary): Promise<void> {
    if (this.variantEditForm.invalid) return;
    const name = this.variantEditForm.controls.name.value;
    try {
      const detail = await firstValueFrom(this.catalogApi.getVariant(variant.id));
      await firstValueFrom(this.catalogApi.updateVariant(variant.id, {
        name,
        attributeOptionIds: detail.attributes.map(attribute => attribute.attributeOptionId),
        isActive: variant.isActive,
        isDefault: variant.isDefault,
        displayOrder: variant.displayOrder
      }));
      this.editingVariantId = null;
      if (this.isEdit()) {
        await this.loadVariants(Number(this.id()));
      }
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to update variant.';
    }
  }

  async generateVariants(): Promise<void> {
    if (!this.isEdit() || this.generateForm.invalid) return;
    const value = this.generateForm.getRawValue();
    try {
      await firstValueFrom(this.catalogApi.generateVariants(Number(this.id()), value));
      await this.loadVariants(Number(this.id()));
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to generate variants.';
    }
  }

  async createOffer(): Promise<void> {
    if (!this.isEdit() || this.offerForm.invalid) return;
    const value = this.offerForm.getRawValue();
    try {
      await firstValueFrom(this.catalogApi.createOffer({
        productId: Number(this.id()),
        variantId: null,
        storeId: value.storeId,
        currencyId: value.currencyId,
        currencyCode: value.currencyCode,
        price: value.price,
        compareAtPrice: value.compareAtPrice,
        isActive: true
      }));
      this.offerForm.patchValue({ price: 0, compareAtPrice: null });
      await this.loadOffers(Number(this.id()));
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to create offer.';
    }
  }

  async save(): Promise<void> {
    if (this.form.invalid) return;
    this.saving = true;
    this.errorMessage = '';
    const value = this.form.getRawValue();
    try {
      if (this.isEdit()) {
        await firstValueFrom(this.catalogApi.updateProduct(Number(this.id()), {
          name: value.name,
          productType: value.productType,
          shortDescription: value.shortDescription || null,
          description: value.description || null,
          slug: value.slug || null,
          published: value.published,
          isVisible: value.isVisible,
          isAvailable: value.isAvailable,
          displayOrder: value.displayOrder
        }));
        await this.router.navigateByUrl('/catalog/products');
      } else {
        await firstValueFrom(this.catalogApi.createProduct({
          name: value.name,
          sku: value.sku,
          productType: value.productType,
          shortDescription: value.shortDescription || null,
          description: value.description || null,
          slug: value.slug || null,
          published: value.published,
          isVisible: value.isVisible,
          isAvailable: value.isAvailable,
          displayOrder: value.displayOrder
        }));
        await this.router.navigateByUrl('/catalog/products');
      }
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Save failed.';
    } finally {
      this.saving = false;
    }
  }

  async loadProductMedia(productId: number): Promise<void> {
    try {
      this.productMedia = await firstValueFrom(this.mediaApi.getProductMedia(productId));
    } catch (error) {
      this.errorMessage = error instanceof ApiClientError ? error.message : 'Failed to load product media.';
    }
  }

  async assignImage(item: MediaSummary, role: string): Promise<void> {
    if (!this.isEdit()) return;
    await firstValueFrom(this.mediaApi.assignProductMedia(Number(this.id()), item.id, role));
    await this.loadProductMedia(Number(this.id()));
  }

  async removeImage(mediaAssetId: number): Promise<void> {
    if (!this.isEdit()) return;
    await firstValueFrom(this.mediaApi.removeProductMedia(Number(this.id()), mediaAssetId));
    await this.loadProductMedia(Number(this.id()));
  }
}
