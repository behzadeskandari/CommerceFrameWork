# PHASE 21 — Advanced Pricing, Currency & Tax Engine — Pre-Implementation

**Date:** 2026-08-12  
**Status:** Approved for implementation

---

## 1. Objective

Centralize price and tax calculation so monetary logic is not scattered across controllers, cart, checkout, orders, products, or frontend. Extend Phases 14 (Pricing/Discounts) and 16 (Tax) with customer-group pricing, tier/quantity pricing, currency conversion, deterministic rounding, and admin configuration completeness.

**Out of scope:** Coupons beyond existing engine, gift cards, affiliate, advanced promotions (Phase 26).

---

## 2. Current State

| Area | Status |
|---|---|
| Base pricing | `ProductOffer` per store + currency |
| Discounts | `IPriceCalculationService` + `DiscountCalculationEngine` |
| Tax | `ITaxProvider` / `InternalTaxProvider`, checkout integration |
| Currency | `StoreCurrency`, `ICurrencyConverter` (not wired to pricing) |
| Checkout totals | `CheckoutTotalsCalculator` — single pipeline |
| Order snapshots | Header + line + `OrderTaxLine` persisted |
| Customer groups | **Missing** |
| Tier/quantity pricing | **Missing** |
| Tax settings admin UI | **Missing** |
| Order tax lines in API | **Missing** |

---

## 3. Design

### 3.1 Centralized Pricing Pipeline

New `IProductPricingPipeline` in Pricing module:

```
Offer base price (ProductOffer)
  ↓
Tier/quantity price (OfferTierPrice — best matching MinQuantity)
  ↓
Customer group price (CustomerGroupPrice override)
  ↓
Currency conversion (ICurrencyConverter when needed)
  ↓
[Existing] Discount pipeline (IPriceCalculationService)
  ↓
[Existing] Tax pipeline (ITaxCalculator)
```

**Context:** `ProductPricingContext` — store, customer, customer group, product, offer, variant, quantity, currency, date.

### 3.2 New Domain Entities

**Pricing module:**
- `CustomerGroup` — store-scoped group
- `CustomerGroupPrice` — product/variant/group/store/currency price override

**Catalog module:**
- `OfferTierPrice` — offerId, minQuantity, price (quantity breaks)

**Customers module:**
- `Customer.CustomerGroupId` — nullable FK

### 3.3 Monetary Rounding

`MonetaryRounding` in Framework.Domain:
- Calculation scale: 4 dp (banker's, matches `Money`)
- Display scale: per currency `DecimalPlaces`
- Tax scale: away-from-zero at 4 dp (matches `TaxAmountCalculator`)

### 3.4 Tax Enhancements

- `ITaxSettingsAdminService` — read/update store tax settings
- Expose `OrderTaxLineDto` on `OrderDetailDto`

### 3.5 Integration Points

| Consumer | Change |
|---|---|
| `DiscountAwarePricingService` | Use pipeline for base unit price before discounts |
| `CartOfferValidator` | Pass quantity to pricing reader |
| `ICatalogPricingReader` | Add quantity-aware overload |
| `CheckoutTotalsCalculator` | No change — already centralized |
| `OrderMapper` | Include tax lines |

---

## 4. API Additions

| Route | Purpose |
|---|---|
| `GET/POST/PUT/DELETE /api/admin/pricing/customer-groups` | Customer group CRUD |
| `GET/POST/DELETE .../customer-groups/{id}/prices` | Group price overrides |
| `GET/POST/DELETE /api/admin/catalog/offers/{id}/tier-prices` | Tier pricing |
| `GET/PUT /api/admin/tax/settings` | Tax configuration |
| `POST /api/admin/pricing/calculate` | Admin price preview |

---

## 5. Admin UI

- Customer groups list/form + price overrides
- Tier prices section on product offer form
- Tax settings page (`Tax.Enabled`, `PricesIncludeTax`, etc.)
- Currencies page already supports rates

---

## 6. Testing Plan

- Pipeline: base, tier, group, currency conversion
- Rounding consistency
- Tax inclusive/exclusive (existing + settings)
- Store-specific pricing
- Order snapshot immutability
- Checkout digital/physical regression (no promo changes)

---

## 7. Risks / Limitations

- Cross-currency fallback requires exchange rate; fails clearly if unavailable
- Customer group assignment manual in admin (no auto rules)
- No external tax provider in this phase
