# PHASE 15 — Pre-Implementation Analysis

**Date:** 2026-08-12

## Current Architecture Findings

### Existing shipping abstractions (Phase 11)

| Artifact | Location | Status |
|----------|----------|--------|
| `IShippingRateProvider` | `Checkout.Contracts/CheckoutProviderContracts.cs` | Defined, **no implementations registered** |
| `ShippingRateRequest` | Same | Used by `CheckoutService.GetShippingOptionsAsync` |
| `ShippingOption` | Same | Mapped to `ShippingOptionDto` for API |
| `CheckoutSession` shipping fields | `Checkout.Domain/Entities/CheckoutSession.cs` | Complete (address, method, totals) |
| `Order` shipping snapshot | `Orders.Domain/Entities/Order.cs` | Complete |
| Angular checkout steps | `storefront/pages/checkout.page.ts` | Shipping address + method steps exist |

### Cart

- `CartTotalsDto.ShippingTotal` exists but is always **0** (by design — shipping calculated at checkout).
- No persisted shipping selection on cart entity.

### Checkout gaps to fix

1. `ProductType` passed as `string.Empty` in `GetShippingOptionsAsync` / `ApplyTotalsAsync`.
2. `ClearShippingSelection()` not called when shipping address changes.
3. `CheckoutRequiresShippingEvaluator` only treats `Digital` as non-shippable; must include `Virtual`, `Downloadable`.
4. No `IShippingRateProvider` DI registration → empty options → warning only.

### Product / weight

- `ProductType` enum: Simple, Grouped, Digital, Downloadable, Virtual, Variant, Bundle.
- **No weight field** on Product — Phase 15 adds optional `WeightGrams`.
- Extend `ShippingRateLineItem` with `WeightGrams` and `LineSubtotal`.

### Settings

- Use `ISettingService` + `ISettingDefinitionProvider` pattern (see `CheckoutSettings`).
- Store-scoped keys: `Shipping.Enabled`, `Shipping.DefaultEstimatedDeliveryDays`, etc.

## Required Changes

### New module: `Commerce.Shipping`

```
Commerce.Shipping.Domain
Commerce.Shipping.Contracts
Commerce.Shipping.Application
Commerce.Shipping.Infrastructure
Commerce.Modules.Shipping
```

### Integration points

| Consumer | Integration |
|----------|-------------|
| Checkout | Register `FlatRateShippingRateProvider : IShippingRateProvider` |
| Checkout | Fix ProductType propagation, address change invalidation |
| Catalog | Optional `WeightGrams` on Product |
| Orders | No schema change — snapshots from checkout |
| Admin | `/api/admin/shipping/*` |
| Angular | Admin shipping CRUD + storefront checkout (mostly wired) |

## Dependency Graph

```
Commerce.Shipping.Domain
  └── Framework.Core, Framework.Domain

Commerce.Shipping.Contracts
  └── Framework.Core

Commerce.Shipping.Application
  └── Shipping.Domain, Shipping.Contracts
  └── Checkout.Contracts (IShippingRateProvider bridge)
  └── Catalog.Contracts (product type/weight reader)
  └── Store.Contracts / Framework.Contracts (settings)

Commerce.Shipping.Infrastructure
  └── Shipping.Application, Shipping.Domain
  └── Framework.Data (EF)

Commerce.Modules.Shipping
  └── Shipping.Infrastructure
  └── Framework.Contracts, Framework.Data
```

**Must NOT:** Checkout.Application → Shipping.Infrastructure (architecture test).

## Data Model

- `ShippingMethod` — store-scoped, provider reference, active, display order
- `ShippingZone` — store-scoped, name, is default
- `ShippingZoneCountry` — zone + ISO country
- `ShippingZoneState` — zone + country + state/province
- `ShippingZonePostalRule` — zone + postal prefix/range
- `ShippingRate` — method + zone + currency + pricing rules

## Zone Matching Precedence

1. Postal code exact/prefix match
2. State/province match (within country)
3. Country match
4. Default zone (`IsDefault = true`)

## API Plan

### Admin

- `GET/POST/PUT/DELETE /api/admin/shipping/methods`
- `GET/POST/PUT/DELETE /api/admin/shipping/zones`
- `GET/POST/PUT/DELETE /api/admin/shipping/rates`

### Storefront (existing checkout routes)

- `PUT /api/checkout/{id}/shipping-address`
- `GET /api/checkout/{id}/shipping-options`
- `PUT /api/checkout/{id}/shipping-method`

## Risks

- .NET 10 SDK required for backend build/test (environment may lack it).
- Weight-based rates depend on catalog `WeightGrams` population.
- Mixed digital/physical carts already require shipping (evaluator logic correct after expanding non-shippable types).

## Compatibility

- Backward-compatible extensions to `ShippingRateLineItem` (optional params).
- Backward-compatible `WeightGrams` on Product (nullable, default null).
- No breaking checkout API changes.
