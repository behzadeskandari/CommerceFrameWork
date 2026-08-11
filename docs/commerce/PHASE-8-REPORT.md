# PHASE 8 REPORT — Catalog 2.0, Attributes, Variants, Offers & Pricing

## PHASE 8 COMPLETE

Catalog 2.0: PASS  
Product Types: PASS  
Simple Products: PASS  
Variant Products: PASS  
Digital Product Foundation: PASS  

Attributes: PASS  
Attribute Values: PASS  
Variants: PASS  
SKU: PASS  

Offers: PASS  
Store Pricing: PASS  
Currency Pricing: PASS  
Pricing Resolution: PASS  

Localization: PASS  
Store Isolation: PASS  

Admin:  
Products: PASS  
Categories: PASS  
Attributes: PASS  
Variants: PASS  
Offers: PASS  

Storefront:  
Products: PASS  
Product Details: PASS  
Variant Selection: PASS  
Pricing: PASS  
Localization: PASS  

Installation Regression: PASS  
Customers Regression: PASS  
Store Regression: PASS  
Authentication: PASS  
Authorization: PASS  

Backend Unit Tests: PASS (65)  
Architecture Tests: PASS (20)  
Integration Tests: PASS (14)  
Angular Tests: PASS (4)  
Admin Build: PASS  
Storefront Build: PASS    
Checkout: NOT IMPLEMENTED  
Orders: NOT IMPLEMENTED  
Payments: NOT IMPLEMENTED  
Shipping: NOT IMPLEMENTED  
Tax: NOT IMPLEMENTED  
Inventory: NOT IMPLEMENTED  
Discounts: NOT IMPLEMENTED  
CMS: NOT IMPLEMENTED  
Media: NOT IMPLEMENTED  
Downloads: NOT IMPLEMENTED  
Plugin Engine: NOT IMPLEMENTED  
Smartstore Import: NOT STARTED  

Next Phase: PHASE 9

---

## Catalog 2.0 Architecture

The Phase 4 Catalog module was extended in place under `src/Commerce/Modules/Catalog/`. Existing product/category APIs, permissions, migrations, and Angular UI were preserved and expanded.

```text
Category
   │
   └──── Product
             │
             ├── ProductType (Simple | Variant | Digital | Grouped | Bundle)
             │
             ├── Attributes (definitions + options + assignments)
             │
             ├── Variants (purchasable combinations)
             │      │
             │      ├── SKU
             │      ├── Attribute values
             │      └── Offer
             │
             └── Offers (store + currency scoped)
                    │
                    └── Price (Money)
```

**Purchase flow contract (future Cart):**

```text
CartItem → OfferId → Offer → ResolvedPriceDto
```

Cart must never read `Product.Price` directly. Pricing is resolved through offers.

## Product Types

| Type | Phase 8 support |
|---|---|
| Simple | Fully supported — product-level offer |
| Variant | Fully supported — variant-level offers |
| Digital | Catalog-level only (no downloads) |
| Grouped | Foundation enum value only |
| Bundle | Foundation enum value only |

Legacy enum values `Downloadable` and `Virtual` remain for backward compatibility.

## Product Aggregate

Extended fields on `Product`:

- `IsVisible`, `IsAvailable` — catalog visibility separate from `Published`
- `IsPubliclyVisible()` — `Published && IsVisible && IsAvailable && !Deleted`
- Existing slug, descriptions, SKU, categories, and soft-delete preserved

Products remain **global** (not duplicated per store). Store-specific differences are expressed through **offers**.

## SKU

The existing `Sku` value object remains canonical:

- Normalized to upper invariant
- Validated for empty/invalid characters/max length
- **Uniqueness: global** across products and variants (documented in `VariantService`)

Global SKU uniqueness supports multi-store ecommerce where the same SKU must not collide across the catalog.

## Attribute System

| Entity | Purpose |
|---|---|
| `ProductAttributeDefinition` | Reusable attribute (Color, Size, …) with `AttributeType` |
| `ProductAttributeOption` | Selectable values for Option-type attributes |
| `ProductAttributeAssignment` | Links attribute to product |
| `ProductAttributeValue` | Text/boolean/number values on products |

**Attribute types:** Text, Option, Boolean, Number (MultiSelect/Date prepared for later).

Attribute names and option labels use Phase 7 `EntityTranslation` — no duplicate Persian tables.

## Variants

`ProductVariant` has stable identity (not product + attribute text):

- SKU, name, active/default flags, display order
- `ProductVariantAttribute` links variant to attribute option IDs
- Deterministic attribute combination key prevents duplicate active variants
- Validation: duplicate SKU, duplicate combination, inactive options, wrong product

Variant generation (Cartesian product of assigned option attributes) available via admin API.

## Offers & Pricing

`ProductOffer` represents what can be purchased:

| Field | Notes |
|---|---|
| `ProductId`, `VariantId?` | Simple → product offer; Variant → variant offer |
| `StoreId` | Store-scoped pricing |
| `CurrencyId`, `CurrencyCode` | Explicit currency — never inferred from frontend |
| `Price`, `CompareAtPrice` | `Money` / decimal — no floating point |
| `IsActive`, `ValidFromUtc`, `ValidToUtc` | Temporal validity |

**Price resolution (`IPricingService`):**

1. Resolve store from `IStoreContext`
2. Resolve currency from query param or store context
3. Find active offer (variant offer first, then product fallback)
4. Return `ResolvedPriceDto` snapshot

**Future Cart contract:** `ICatalogPricingReader.GetOfferPriceAsync(offerId)` — read-only offer price for checkout modules.

## Store & Currency Pricing

Same product, different stores:

```text
Store A → Offer USD 10
Store B → Offer USD 12
```

Same product, multiple currencies (explicit offers, no silent conversion):

```text
Product X → Offer IRR 4,000,000
Product X → Offer USD 10
```

Phase 7 currency converter may be used for **display** conversion; authoritative checkout price comes from the matching offer currency.

## Public Storefront API

`CatalogStorefrontController` at `/api/catalog/storefront/*`:

- Anonymous read-only
- Filters: `Published && IsVisible && IsAvailable && !Deleted`
- Active offers only for pricing
- Product detail includes attributes, variants, resolved price

## Admin API

| Area | Route prefix |
|---|---|
| Products | `/api/catalog/products` |
| Categories | `/api/catalog/categories` |
| Attributes | `/api/catalog/attributes` |
| Variants | `/api/catalog/products/{id}/variants`, `/api/catalog/variants/{id}` |
| Offers | `/api/catalog/offers` |
| Pricing | `/api/catalog/pricing/products/{id}`, `/api/catalog/pricing/variants/{id}` |

New permissions: `Catalog.Attributes.*`, `Catalog.Variants.*`, `Catalog.Offers.*`

## Angular Admin

- `/admin/catalog/products` — product editor with visibility, variants, offers
- `/admin/catalog/attributes` — attribute CRUD, options, localization
- Product form: variant generation, SKU/active/default, offer management

## Angular Storefront

- Product detail uses `/api/catalog/storefront/products/{id}`
- Variant attribute selectors resolve valid variant
- Price fetched from pricing API (no client-side price calculation)
- Add-to-cart placeholder only (Cart not implemented)

## Database

New tables via `CatalogModelContributor` / EF configurations:

- `ProductAttributeOption`
- `ProductAttributeAssignment`
- `ProductVariant`
- `ProductVariantAttribute`
- `ProductOffer`

Indexes on ProductId, VariantId, Sku, StoreId, CurrencyId, IsActive, Slug. Unique constraints for SKU and variant attribute combinations.

## Development Seed Data

When `Commerce:Catalog:SeedDevelopmentData=true`:

- **C# Course** — Digital, simple offers (IRR + USD)
- **T-Shirt** — Variant with Color/Size attributes, multiple variants, multi-currency offers

## Security

- All mutation endpoints require catalog permissions
- Public APIs hide draft/deleted/inactive items
- Store context enforced for pricing resolution
- Client-supplied store/currency/offer IDs validated server-side

## Tests

| Suite | Count | Status |
|---|---|---|
| Unit | 65 | PASS |
| Architecture | 20 | PASS |
| Integration | 14 | PASS |
| Angular | 4 | PASS |
| **Total backend** | **99** | PASS |

Notable integration test: `Catalog20FlowTests` — attribute → variant → offer → price → storefront visibility.

Architecture tests verify Catalog layers do not reference Orders, Checkout, Payments, Shipping, Inventory, ShoppingCart, or Discounts assemblies.

## Validation Commands

```bash
dotnet restore Commerce.sln
dotnet build Commerce.sln --configuration Release
dotnet test Commerce.sln --configuration Release

cd frontend/commerce-ui
npm install
npm run build
npm test
```

All executed successfully with 0 build errors and 0 warnings.
