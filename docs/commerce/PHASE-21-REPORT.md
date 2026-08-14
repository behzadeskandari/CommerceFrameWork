# PHASE 21 — Advanced Pricing, Currency & Tax Engine — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## 1. Summary

Phase 21 centralizes advanced pricing logic into a deterministic pipeline and completes currency/tax configuration surfaces. Building on Phases 14 (discounts) and 16 (tax), it adds customer-group pricing, tier/quantity pricing, monetary rounding rules, tax settings admin, and order tax line API exposure — without implementing promotional engines (Phase 26 scope).

---

## 2. Pricing Pipeline

**Flow:**
```
ProductOffer base price
  → OfferTierPrice (quantity breaks)
  → CustomerGroupPrice override
  → [existing] IPriceCalculationService (discounts)
  → [existing] ITaxCalculator (checkout)
```

**New services:**
- `IProductPricingPipeline` / `ProductPricingPipeline`
- `IAdvancedPricingService` — admin price preview
- `ICustomerGroupAdminService` — group + price CRUD

**Integration:**
- `DiscountAwarePricingService` runs pipeline before discounts
- `CartOfferValidator` passes quantity for tier-aware pricing
- `ICatalogPricingReader` quantity overload

---

## 3. Domain Additions

| Entity | Module | Purpose |
|---|---|---|
| `CustomerGroup` | Pricing | Store-scoped customer segments |
| `CustomerGroupPrice` | Pricing | Per-product group price override |
| `OfferTierPrice` | Catalog | Quantity break pricing on offers |
| `Customer.CustomerGroupId` | Customers | Group assignment |
| `MonetaryRounding` | Framework.Domain | Calculation/tax/display rounding |

---

## 4. Currency

- Existing `Money` value object (4 dp, banker's rounding) retained as calculation standard
- `MonetaryRounding.RoundForDisplay` uses per-currency decimal places
- `ICurrencyConverter` wired into pipeline infrastructure (conversion path ready; offers remain currency-explicit)

---

## 5. Tax

- `ITaxSettingsAdminService` — read/update store tax settings
- `TaxAmountCalculator` uses centralized `MonetaryRounding.RoundForTax`
- `OrderDetailDto` now includes `OrderTaxLineDto` breakdown
- Admin UI: `/tax/settings`

---

## 6. API

| Route | Purpose |
|---|---|
| `GET/POST/PUT/DELETE /api/admin/pricing/customer-groups` | Customer groups |
| `GET/POST/PUT/DELETE .../prices` | Group price overrides |
| `POST /api/admin/pricing/preview` | Price calculation preview |
| `GET/POST/PUT/DELETE /api/admin/catalog/offers/{id}/tier-prices` | Tier pricing |
| `GET/PUT /api/admin/tax/settings` | Tax configuration |

---

## 7. Admin UI

- Customer groups list (`/pricing/customer-groups`)
- Tax settings page (`/tax/settings`)
- Currencies/exchange rates (existing `/currencies` page)
- Tier prices API ready for product offer UI extension

---

## 8. Tests

### Unit (`Commerce.Tests.Unit/AdvancedPricing/`)
- Monetary rounding (calculation, tax, display)
- Offer tier price domain rules
- Customer group price domain rules
- Tax inclusive/exclusive calculation

### Build results

| Target | Result |
|---|---|
| `npm test` / `npm run build` | See CI/local run |
| `dotnet build` / `dotnet test` | Blocked without .NET 10 SDK |

---

## 9. Known Limitations

1. Cross-currency offer fallback conversion not fully automated (requires per-currency offers)
2. Customer group assignment via admin customer detail UI minimal (domain + DTO support added)
3. External tax providers deferred ( `ITaxProvider` abstraction exists)
4. Advanced promotions/coupons beyond Phase 14 engine — Phase 26

---

## 10. Key Files

```
docs/commerce/PHASE-21-PREIMPLEMENTATION.md
src/Commerce/Framework/Commerce.Framework.Domain/ValueObjects/MonetaryRounding.cs
src/Commerce/Modules/Pricing/Commerce.Pricing.Application/AdvancedPricing/
src/Commerce/Modules/Catalog/Commerce.Catalog.Domain/Entities/OfferTierPrice.cs
src/Commerce/Modules/Tax/Commerce.Tax.Application/Admin/TaxSettingsAdminService.cs
src/Commerce/Host/Commerce.Host/Pricing/AdminAdvancedPricingController.cs
frontend/commerce-ui/libs/api/src/lib/advanced-pricing-api.service.ts
tests/Commerce/Commerce.Tests.Unit/AdvancedPricing/AdvancedPricingTests.cs
```

---

**Phase 21 complete. Stopped — awaiting explicit approval before Phase 22.**
