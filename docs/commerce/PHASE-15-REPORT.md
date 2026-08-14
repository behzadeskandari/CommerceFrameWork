# PHASE 15 — Commerce Shipping Engine — Report

**Status:** Complete (backend compile/test blocked by .NET 10 SDK in CI environment)  
**Date:** 2026-08-12

---

## 1. Summary

Phase 15 delivers a production-oriented shipping subsystem integrated with Store, Catalog, Cart, Checkout, Orders, Settings, Permissions, and Angular Admin/Storefront. Shipping calculation is centralized; checkout and orders consume contracts only. A fully functional built-in `FlatRateShippingProvider` is included. Payments, tax, and the dynamic plugin engine were intentionally excluded.

---

## 2. Architecture

### Provider model

```text
Checkout (IShippingRateProvider)
  ↑ FlatRateShippingRateProvider (bridge)
  ↑ IShippingCalculationService
  ↑ IShippingProvider (future plugin boundary)
  ↑ FlatRateShippingProvider
```

- **Checkout boundary:** `IShippingRateProvider` from Phase 11 reused; no Checkout → Shipping.Infrastructure reference.
- **Internal plugin boundary:** `IShippingProvider` for future dynamic plugins (`Shipping.FlatRate`, `Shipping.FedEx`, etc.).
- **Security:** Client may send only `methodId` + `providerSystemName`; backend recalculates authoritative price.

### Calculation flow

```text
Cart items (offer, product type, weight, subtotal)
  → Checkout session + shipping address
  → ShippingRateRequest → FlatRateShippingRateProvider
  → ShippingCalculationService
  → ShippingZoneMatcher (postal → state → country → default)
  → ShippingRateCalculator (base + weight + free threshold)
  → CalculatedShippingOption[]
  → Checkout selects method → session stores price snapshot
  → Order snapshots shipping at creation
```

### Totals pipeline

```text
Subtotal - Discount + Shipping + Tax(0) = Grand Total
```

---

## 3. Projects Created

| Project | Purpose |
|---|---|
| `Commerce.Shipping.Domain` | Entities, enums |
| `Commerce.Shipping.Contracts` | Calculation/admin contracts |
| `Commerce.Shipping.Application` | Engine, providers, admin services |
| `Commerce.Shipping.Infrastructure` | EF, permissions, settings, seeder |
| `Commerce.Modules.Shipping` | Module registration |

**Path:** `src/Commerce/Modules/Shipping/`

---

## 4. Database Schema

| Table | Notes |
|---|---|
| `ShippingMethods` | Store-scoped, provider reference, soft delete |
| `ShippingZones` | Geographic zones, default flag |
| `ShippingZoneCountries` | ISO country codes |
| `ShippingZoneStates` | Country + state/province |
| `ShippingZonePostalRules` | Exact/prefix/range |
| `ShippingRates` | Base price, weight surcharge, free threshold, min/max bounds |

**Catalog extension:** `CatalogProduct.WeightGrams` (decimal, default 0) for weight-based rates.

Migration: `ShippingInitialMigration` registered via module runtime.

---

## 5. Module Registration

Registered in `Program.cs` after `PricingModule`, before `OrdersModule`:

```csharp
modules.AddModule<ShippingModule>();
```

Seeder (opt-in): `Commerce:Shipping:SeedDevelopmentData=true`

---

## 6. Checkout Integration

- `CheckoutItemDto` extended with `ProductType`, `WeightGrams`
- `ShippingRateLineItem` extended with product type, weight, line subtotal
- Address change clears shipping selection (`ClearShippingSelection`)
- Stale/invalid selections invalidated during mapping with user warning
- Cart refresh recalculates `RequiresShipping` and clears stale shipping
- Digital/Virtual/Downloadable products skip shipping automatically

---

## 7. Order Integration

Orders already snapshot:

- `RequiresShipping`, `ShippingAddress`, `SelectedShippingMethodId`, `SelectedShippingProviderSystemName`, `ShippingTotal`

Historical orders remain immutable; deleted methods do not affect past orders.

---

## 8. Digital Products

Non-shippable product types: `Digital`, `Virtual`, `Downloadable`.

- Digital-only cart: no shipping address/method/cost required
- Mixed cart: shipping calculated on physical lines only

---

## 9. Free Shipping

Supported via:

- Rate-level `FreeShippingThreshold`
- Weight surcharge optional on flat rates
- Store-level settings (`Shipping.AllowFreeShipping`, etc.)

---

## 10. Zone Matching Precedence

1. Postal code rule (exact → prefix → range)
2. State/province within country
3. Country
4. Default zone (`IsDefault = true`)

---

## 11. Settings

Via `ISettingService`:

| Key | Purpose |
|---|---|
| `Shipping.Enabled` | Master toggle |
| `Shipping.DefaultEstimatedDeliveryDays` | Fallback estimate |
| `Shipping.AllowFreeShipping` | Policy flag |
| `Shipping.RequireShippingAddress` | Policy flag |

---

## 12. Permissions

| Permission | Purpose |
|---|---|
| `Shipping.View` | List/read methods, zones, rates |
| `Shipping.Manage` | CRUD methods, zones, rates |
| `Shipping.Configure` | Provider/settings (future) |

---

## 13. API Routes

### Admin

| Method | Route |
|---|---|
| GET/POST | `/api/admin/shipping/methods` |
| GET/PUT/DELETE | `/api/admin/shipping/methods/{id}` |
| GET/POST | `/api/admin/shipping/zones` |
| GET/PUT/DELETE | `/api/admin/shipping/zones/{id}` |
| GET/POST | `/api/admin/shipping/rates` |
| GET/PUT/DELETE | `/api/admin/shipping/rates/{id}` |

### Storefront (existing checkout routes)

| Method | Route |
|---|---|
| GET | `/api/checkout/{id}` (includes `shippingOptions`) |
| PUT | `/api/checkout/{id}/shipping-address` |
| PUT | `/api/checkout/{id}/shipping-method` |

---

## 14. Angular Changes

### Admin

- `/shipping/methods`, `/shipping/zones`, `/shipping/rates` (+ new/edit)
- `ShippingApi` service, models, EN/FA translations, sidebar nav
- Permissions: `Shipping.View`, `Shipping.Manage`

### Storefront

- Checkout shipping steps already wired; displays options, price, updated grand total
- Digital-only carts skip shipping steps via `requiresShipping`

---

## 15. Tests

### Unit (`Commerce.Tests.Unit`)

- `ShippingZoneMatcherTests` — precedence, default zone
- `ShippingRateCalculatorTests` — free shipping, weight surcharge, currency

### Architecture (`Commerce.Tests.Architecture`)

- `ShippingArchitectureTests` — domain/application boundary enforcement

### Integration (`Commerce.Tests.Integration`)

- `ShippingFlowTests` — admin CRUD, physical shipping options, digital skip, selection/totals, address invalidation
- `CheckoutFlowTests` updated for shipping method selection with seeded data

**Backend execution:** NOT RUN — environment has .NET SDK 8.0.302; solution targets `net10.0`.

---

## 16. Validation Results

| Check | Result |
|---|---|
| `dotnet build Commerce.sln --configuration Release` | **FAILED** — NETSDK1045 (.NET 10 required) |
| `dotnet test Commerce.sln --configuration Release` | **NOT RUN** — blocked by SDK |
| `npm test` (admin + storefront) | **PASSED** — 4/4 tests |
| `npm run build` (admin + storefront) | **PASSED** |

---

## 17. Security Considerations

- Server-side recalculation on every shipping method selection
- Inactive/wrong-store/wrong-zone/wrong-currency methods excluded by engine
- Address change invalidates prior selection
- Cart/price changes clear shipping selection on refresh
- No client-authoritative `shippingPrice` field accepted

---

## 18. Known Limitations

- Only `FlatRateShippingProvider` implemented (no external couriers)
- No tax calculation (Phase future)
- No dynamic plugin loading (prepared via `IShippingProvider`)
- Product weight not yet exposed in admin product form (field exists on entity)
- Category/product-level free-shipping restrictions not implemented
- Backend tests unverified until .NET 10 SDK available

---

## 19. Future Plugin Integration

Future plugins will implement `IShippingProvider` with system names such as:

- `Shipping.FlatRate` (built-in)
- `Shipping.Post`, `Shipping.FedEx`, `Shipping.UPS`, `Shipping.DHL`

Core engine remains provider-agnostic.

---

## 20. Files Changed (high level)

- `src/Commerce/Modules/Shipping/**` — new module
- `src/Commerce/Modules/Checkout/**` — integration fixes
- `src/Commerce/Modules/Catalog/**` — `WeightGrams` on Product
- `src/Commerce/Host/**` — module registration, admin controllers
- `frontend/commerce-ui/**` — admin shipping UI, API, i18n
- `tests/Commerce/**` — unit, architecture, integration tests
- `docs/commerce/**` — this report, architecture updates

---

**Phase 15 complete. Awaiting explicit approval before Phase 16.**
