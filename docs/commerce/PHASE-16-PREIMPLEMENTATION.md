# PHASE 16 — Tax Engine — Pre-Implementation

**Date:** 2026-08-12

---

## 1. Current Architecture Findings

### Existing tax placeholder (Phase 11)

- `ITaxCalculator` / `TaxCalculationRequest` / `TaxCalculationResult` in `Checkout.Contracts`
- `NoOpTaxCalculator` registered in Checkout.Application (returns `TaxTotal = 0`)
- `CheckoutTotalsCalculator` already invokes `ITaxCalculator` but:
  - Uses **pre-discount** subtotal for tax input
  - Calculates tax **before** discount is applied to grand total (order: discount → tax → grand)
  - Does **not** pass shipping amount to tax calculator
  - Does **not** expose tax breakdown in checkout DTO

### Order model

- `Order.TaxTotal` and `OrderItem.TaxTotal` exist
- `OrderService.CreateFromCheckoutAsync` hardcodes item `DiscountTotal = 0`, `TaxTotal = 0` — **must fix**
- No order-level tax line snapshots — **will add `OrderTaxLine`**

### Product / Customer

- `Product` has no `TaxCategoryId` — **will add**
- `Customer` has no tax exemption fields — **will add** `IsTaxExempt`, `TaxRegistrationNumber`

### Pricing (Phase 14)

- `IPriceCalculationService.CalculateCartAsync` returns per-line `LineDiscountTotal`
- Tax must consume post-discount taxable amounts; must **not** recalculate discounts

### Shipping (Phase 15)

- `CheckoutTotalContext.ShippingTotal` available
- Digital/non-shippable lines identified via `ProductType`
- Shipping tax applies only when shipping amount > 0

---

## 2. Design Decisions

| Topic | Decision |
|-------|----------|
| Checkout boundary | Reuse `ITaxCalculator`; `CheckoutTaxCalculator` bridges to internal engine |
| Internal plugins | `ITaxProvider` + `InternalTaxProvider` (built-in) |
| Zone matching | Same precedence as Shipping: postal → state → country → default |
| Shared geo abstractions | **No** — duplicate zone matcher in Tax.Application (minimal scope) |
| Tax on | Post-discount line amounts + shipping (when taxable) |
| Inclusive pricing | Store setting `Tax.PricesIncludeTax`; extract vs add tax portion |
| Rounding | Per-line, 4 decimal places, `MidpointRounding.AwayFromZero`, sum lines |
| Product classification | `Product.TaxCategoryId` nullable FK |
| Customer exemption | `Customer.IsTaxExempt`, `TaxRegistrationNumber` |
| Order snapshot | `OrderTaxLine` entity + item-level `TaxTotal`/`DiscountTotal` |

---

## 3. Domain Model

### Entities

- **TaxCategory** — store-scoped classification (Standard, Reduced, Zero, Exempt, Digital, etc.)
- **TaxRate** — percentage (primary), optional fixed, effective dates, priority, zone, category, `TaxShipping` flag
- **TaxZone** + **TaxZoneCountry/State/PostalRule** — geographic scope
- **OrderTaxLine** (Orders module) — immutable tax snapshot on order

### Not creating

- Separate `TaxRule` aggregate (rates + zones sufficient for generic engine)
- `TaxExemption` entity (customer flag + exempt category sufficient)

---

## 4. Tax Calculation Pipeline

```text
CheckoutTotalContext
  → IDiscountCalculator (Pricing) — authoritative discounts
  → ITaxCalculator (Tax.CheckoutTaxCalculator)
      → IPriceCalculationService (line discounts)
      → ITaxCalculationService
          → ITaxProvider (InternalTaxProvider)
              → TaxZoneMatcher + TaxRateResolver + TaxAmountCalculator
  → CheckoutTotalResult (subtotal, discount, shipping, tax, grand)
```

Formula (exclusive):

```text
LineTaxable = LineSubtotal - LineDiscount
LineTax = round(LineTaxable * Rate)
ShippingTax = round(ShippingTotal * ShippingRate) when applicable
TaxTotal = sum(LineTax) + ShippingTax
GrandTotal = Subtotal - DiscountTotal + ShippingTotal + TaxTotal
```

---

## 5. Module Dependencies

```text
Commerce.Tax
 ├── Framework (Core, Domain, Contracts)
 ├── Store (settings)
 ├── Catalog.Contracts (product tax category)
 ├── Customers.Contracts (exemption)
 ├── Pricing.Contracts (line discounts)
 ├── Checkout.Contracts (ITaxCalculator bridge)
 └── Orders.Contracts (optional, snapshot types in Orders.Domain)
```

Tax module does **not** reference Shipping.Infrastructure or Pricing.Infrastructure.

---

## 6. API Plan

| Route | Purpose |
|-------|---------|
| `/api/admin/tax/categories` | CRUD tax categories |
| `/api/admin/tax/rates` | CRUD tax rates |
| `/api/admin/tax/zones` | CRUD tax zones |

Permissions: `Tax.View`, `Tax.Manage`, `Tax.Configure`

---

## 7. Checkout / Order Changes

- Extend `TaxCalculationRequest` with shipping, cart context, coupons
- Extend `TaxCalculationResult` with line breakdown, shipping tax
- Extend `CheckoutTotalsDto` with `TaxLines`, `PricesIncludeTax`
- Extend `OrderPreparationLineDto` with discount/tax per line
- Fix `OrderService` to persist line discount/tax and order tax lines

---

## 8. Angular Plan

- Admin: categories, rates, zones, settings display
- Storefront: tax row + breakdown in checkout review
- EN/FA localization

---

## 9. Migration Plan

- `TaxInitialMigration` via `ICommerceMigration`
- `CatalogProduct.TaxCategoryId` column
- `Customer` tax fields
- `OrderTaxLine` table

---

## 10. Risks

- Fixing totals pipeline may affect existing checkout tests — update integration tests
- Tax-inclusive display requires careful storefront labeling
- Backend validation blocked until .NET 10 SDK available

---

## 11. Compatibility

- `ITaxCalculator` signature extended with optional parameters (backward compatible for NoOp)
- Existing checkout/order APIs extended, not replaced
- NoOpTaxCalculator removed when Tax module registers real calculator
