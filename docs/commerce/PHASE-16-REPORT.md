# PHASE 16 — Commerce Tax Engine — Report

**Status:** Complete (backend compile/test blocked by .NET 10 SDK in this environment)  
**Date:** 2026-08-12

---

## 1. Summary

Phase 16 delivers a production-oriented tax subsystem as the **authoritative** source for checkout tax calculation. Tax is computed server-side through a centralized engine integrated with Pricing (post-discount amounts), Shipping, Catalog (product classification), Customers (exemptions), Checkout, and Orders (immutable snapshots). Payments and external tax providers were intentionally excluded.

---

## 2. Architecture

### Provider model

```text
Checkout (ITaxCalculator)
  ↑ CheckoutTaxCalculator (bridge)
  ↑ ITaxCalculationService
  ↑ ITaxProvider (future plugin boundary)
  ↑ InternalTaxProvider (built-in)
      → TaxZoneMatcher + rate resolution + TaxAmountCalculator
```

- **Checkout boundary:** Existing `ITaxCalculator` from Phase 11 reused; Tax module registers `CheckoutTaxCalculator` after Checkout's `NoOpTaxCalculator`.
- **Internal plugin boundary:** `ITaxProvider` for future dynamic plugins (`Tax.Internal`, country-specific, external APIs).
- **Security:** Client may display tax totals but cannot submit trusted tax amounts, rates, or grand totals.

### Calculation flow

```text
Cart / checkout lines (base offer prices)
  → Pricing (IDiscountCalculator) — post-discount taxable amounts
  → Shipping total (when RequiresShipping)
  → ITaxCalculator → InternalTaxProvider
  → Zone match (postal → state → country → default)
  → Rate match (category + zone + priority + effective dates)
  → Per-line tax + shipping tax
  → CheckoutTotalsCalculator → grand total
  → Order snapshot (OrderTaxLine + item TaxTotal)
```

### Totals pipeline (single authoritative path)

```text
Subtotal - DiscountTotal + ShippingTotal + TaxTotal = GrandTotal
```

Tax is calculated on **post-discount** line amounts. Pricing remains responsible for discounts; Tax consumes final taxable amounts only.

---

## 3. Projects Created

| Project | Purpose |
|---|---|
| `Commerce.Tax.Domain` | `TaxCategory`, `TaxRate`, `TaxZone`, zone rules, enums |
| `Commerce.Tax.Contracts` | `ITaxCalculationService`, `ITaxProvider`, calculation DTOs, admin DTOs |
| `Commerce.Tax.Application` | Engine, `InternalTaxProvider`, `CheckoutTaxCalculator`, admin services |
| `Commerce.Tax.Infrastructure` | EF, permissions, settings, seeder, migration |
| `Commerce.Modules.Tax` | Module registration |

**Path:** `src/Commerce/Modules/Tax/`

---

## 4. Domain Model

| Entity | Purpose |
|---|---|
| `TaxCategory` | Store-scoped classification (Standard, Reduced, Zero, Exempt, Digital, etc.); `IsExempt` flag |
| `TaxRate` | Percentage or fixed amount, zone, category, priority, effective dates, `TaxShipping` flag |
| `TaxZone` | Geographic scope with default-zone fallback |
| `TaxZoneCountry` | ISO country match |
| `TaxZoneState` | Country + state/province match |
| `TaxZonePostalRule` | Exact, prefix, or range postal match |
| `OrderTaxLine` (Orders) | Immutable tax breakdown snapshot on orders |

**Not created (justified):** separate `TaxRule`, `TaxExemption`, `TaxConfiguration` aggregates — rates + zones + customer/category flags suffice for a generic engine.

**Catalog extension:** `Product.TaxCategoryId` (nullable FK reference, no duplication)

**Customers extension:** `Customer.IsTaxExempt`, `Customer.TaxRegistrationNumber`

---

## 5. Tax-Inclusive vs Tax-Exclusive

Store setting: `Tax.PricesIncludeTax` (via `TaxSettingDefinitionProvider`)

| Mode | Behavior |
|---|---|
| Exclusive | Tax added on top: Net 100 + Tax 10 = Total 109 |
| Inclusive | Tax extracted from gross: Gross 110 → Tax portion 10, Net 100 |

Implemented in `TaxAmountCalculator` using decimal arithmetic (4 decimal places, `MidpointRounding.AwayFromZero`).

---

## 6. Rounding

- **Precision:** 4 decimal places for tax amounts
- **Strategy:** Per-line rounding, then aggregate
- **Currency:** Uses existing checkout/cart `Money` conventions for display totals

---

## 7. Database Schema

| Table | Notes |
|---|---|
| `TaxCategories` | Store-scoped, soft delete, exempt flag |
| `TaxZones` | Default zone, display order |
| `TaxZoneCountries` | Country codes |
| `TaxZoneStates` | Country + state |
| `TaxZonePostalRules` | Exact/prefix/range |
| `TaxRates` | Percentage/fixed, priority, effective dates, tax-shipping flag |
| `OrderTaxLine` | Order snapshot: name, taxable, tax, rate, category |
| `CatalogProduct.TaxCategoryId` | Product classification column |
| `Customer.IsTaxExempt`, `Customer.TaxRegistrationNumber` | Customer tax fields |

Migration: `TaxInitialMigration` registered via module runtime (`ICommerceMigration`).

---

## 8. Module Registration

Registered in `Program.cs` after `ShippingModule`, before `OrdersModule`:

```csharp
modules.AddModule<TaxModule>();
```

Seeder (opt-in): `Commerce:Tax:SeedDevelopmentData=true`

---

## 9. Checkout Integration

Extended contracts and services:

- `TaxCalculationRequest` — cart id, shipping total, coupons, guest flag, requires-shipping
- `TaxCalculationResult` — product/shipping tax split, line breakdown, prices-include-tax flag
- `CheckoutTotalsDto` — `ProductTaxTotal`, `ShippingTaxTotal`, `PricesIncludeTax`, `TaxLines`, `TaxLineItems`
- `CheckoutTotalsCalculator` — discount → tax → grand total
- `CheckoutService` — authoritative recalculation on map/refresh; ignores client totals

Tax address resolution: shipping address when `RequiresShipping`, otherwise billing address.

Digital-only carts: no shipping tax (`RequiresShipping = false`); product tax still applies.

---

## 10. Order Integration

- `OrderTaxLine` entity with EF configuration
- `OrderService.CreateFromCheckoutAsync` snapshots tax lines and per-item `TaxTotal` / `DiscountTotal`
- Historical orders immutable when rates, zones, or customer status change later

---

## 11. Admin API

| Endpoint | Permission |
|---|---|
| `GET/POST/PUT/DELETE /api/admin/tax/categories` | `Tax.View` / `Tax.Manage` |
| `GET/POST/PUT/DELETE /api/admin/tax/zones` | `Tax.View` / `Tax.Manage` |
| `GET/POST/PUT/DELETE /api/admin/tax/rates` | `Tax.View` / `Tax.Manage` |

Settings (`Tax.Enabled`, `Tax.PricesIncludeTax`, `Tax.DefaultCategoryId`) via existing store settings infrastructure with `Tax.Configure` permission.

---

## 12. Permissions

| Permission | Description |
|---|---|
| `Tax.View` | View tax configuration |
| `Tax.Manage` | CRUD categories, zones, rates |
| `Tax.Configure` | Configure tax providers and store settings |

---

## 13. Angular Changes

### Admin

- Routes: `/tax/categories`, `/tax/zones`, `/tax/rates` (list + form pages)
- `TaxApiService`, `tax.models.ts`
- Navigation entries in admin layout
- EN + FA localization keys

### Storefront

- Checkout totals display: subtotal, discount, shipping, product tax, shipping tax, tax total, prices-include-tax note, tax line breakdown
- Informational only — no client-side tax editing

---

## 14. Tests

| Suite | Coverage |
|---|---|
| `TaxCalculationTests` | Zone matching, inclusive/exclusive amounts, zero inputs |
| `TaxArchitectureTests` | Domain isolation, no infrastructure leakage, checkout boundary |
| `TaxFlowTests` | Admin CRUD, checkout with billing address returns tax totals |

**Frontend:** `npm test -- --watch=false --browsers=ChromeHeadless` — **PASS** (admin 1/1, storefront 3/3)  
**Frontend build:** `npm run build` — **PASS** (admin + storefront)

**Backend:** **BLOCKED** — SDK 8.0.302; projects target `net10.0`

---

## 15. Build Results

| Command | Result |
|---|---|
| `dotnet --version` | 8.0.302 |
| `dotnet build Commerce.sln -c Release` | **BLOCKED** (NETSDK1045) |
| `dotnet test Commerce.sln -c Release` | **BLOCKED** |
| `npm test` | **PASS** |
| `npm run build` | **PASS** |

---

## 16. Known Limitations

1. **No external tax providers** — only `InternalTaxProvider`; abstraction ready for Phase 17+ plugins
2. **No dedicated tax settings admin UI** — settings via store settings API / future admin screen
3. **No order detail API tax line exposure yet** — snapshots persisted; storefront order detail may not show breakdown
4. **Fixed-amount rates** — domain supports `TaxRateType.Fixed` but engine primarily exercises percentage rates in tests
5. **No compound/multi-jurisdiction stacking** — highest-priority matching rate per category
6. **Backend validation blocked** until .NET 10 SDK available in CI/dev

---

## 17. Architectural Changes

1. Extended `ITaxCalculator` contract with richer request/result (backward compatible for `NoOpTaxCalculator`)
2. Fixed `OrderService` to use real line discount/tax from checkout preparation (was hardcoded zero)
3. Added `OrderTaxLine` for immutable tax snapshots
4. Added `Product.TaxCategoryId` and customer exemption fields
5. Tax module overrides `NoOpTaxCalculator` via later DI registration (Tax after Checkout in `Program.cs`)

---

## 18. Before Phase 17

Recommended follow-ups (not in scope for Phase 16):

- Install .NET 10 SDK and run full backend test suite
- Expose `OrderTaxLine` in order detail DTOs/API
- Admin UI for `Tax.Enabled` / `Tax.PricesIncludeTax` store settings
- Product form: assign `TaxCategoryId` in catalog admin UI
- Customer form: tax exemption fields in customer admin UI
- Additional unit tests: fixed rates, expired rates, exempt customer/category, coupon+tax, mixed carts
- Consider shared geographic matching abstraction with Shipping if duplication becomes maintenance burden

---

**Phase 16 complete. Awaiting explicit approval before Phase 17.**
