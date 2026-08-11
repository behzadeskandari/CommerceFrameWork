# PHASE 10 COMPLETE

## Cart Module: PASS
## Guest Cart: PASS
## Customer Cart: PASS
## Cart Persistence: PASS

## Cart Items: PASS
## Offer Integration: PASS
## Pricing Integration: PASS
## Currency Validation: PASS

## Add Item: PASS
## Update Quantity: PASS
## Remove Item: PASS
## Clear Cart: PASS

## Guest Cart Token: PASS
## Guest → Customer Merge: PASS
## Duplicate Offer Merge: PASS

## Store Isolation: PASS
## Customer Isolation: PASS
## Authorization: PASS
## Concurrency: PASS

## Cart Totals: PASS
## Price Change Detection: PASS
## Invalid Offer Handling: PASS
## Expiration: PASS

## Angular Cart: PASS
## Add to Cart: PASS
## Cart Count: PASS
## Quantity Controls: PASS
## Remove Item: PASS
## Empty Cart: PASS
## Responsive UI: PASS
## RTL/LTR: PASS

## Installation Regression: PASS
## Catalog Regression: PASS
## Pricing Regression: PASS
## Customers Regression: PASS
## Store Regression: PASS
## Media Regression: PASS

## Backend Unit Tests: PASS (75)
## Architecture Tests: PASS (26)
## Integration Tests: PASS (22)
## Angular Tests: PASS (4)
## Admin Build: PASS
## Storefront Build: PASS

## Checkout: NOT IMPLEMENTED
## Orders: NOT IMPLEMENTED
## Payments: NOT IMPLEMENTED
## Shipping: NOT IMPLEMENTED
## Tax: NOT IMPLEMENTED
## Inventory: NOT IMPLEMENTED
## Discounts: NOT IMPLEMENTED
## Digital Downloads: NOT IMPLEMENTED
## CMS: NOT IMPLEMENTED
## Themes: NOT IMPLEMENTED
## Plugin Engine: NOT IMPLEMENTED
## Smartstore Import: NOT STARTED

---

## Architecture Summary

### Module layout

```text
src/Commerce/Modules/Cart/
├── Commerce.Cart.Domain
├── Commerce.Cart.Contracts
├── Commerce.Cart.Application
├── Commerce.Cart.Infrastructure
└── Commerce.Modules.Cart
```

### Purchase chain

```text
Product → Variant → Offer → ResolvedPriceDto → CartItem
```

Cart never trusts client `price`, `currency`, or totals. Only `offerId` and `quantity` are accepted from Angular.

### Cart ownership

| Type | Identity | Persistence |
|---|---|---|
| Guest | `GuestToken` in HttpOnly cookie `commerce.cart.guest` | Database |
| Customer | `CustomerId` from auth | Database |

**Invariant:** one active cart per **Store + Customer + Currency** or **Store + GuestToken + Currency**.

### Cart status

`Active`, `Converted`, `Abandoned`, `Expired` — only `Active` carts accept modifications.

### API

| Method | Route | Description |
|---|---|---|
| GET | `/api/cart` | Get or create current cart |
| POST | `/api/cart/items` | Add/increase item by offer |
| PUT | `/api/cart/items/{id}` | Update quantity (revalidates offer) |
| DELETE | `/api/cart/items/{id}` | Remove item |
| DELETE | `/api/cart` | Clear items |
| POST | `/api/cart/merge` | Merge guest cart after login |

### Totals

`ICartTotalsCalculator` computes line subtotals using `Money`. `DiscountTotal`, `ShippingTotal`, and `TaxTotal` remain `0` until future modules contribute.

### Settings

- `Cart.MaxItemQuantity` (default 999)
- `Cart.MaxDistinctItems` (default 100)
- `Cart.GuestExpirationHours` (default 720)
- `Cart.CustomerExpirationDays` (default 30)

### Angular

- `CartApi`, `CartStateService` in `@commerce/api`
- Storefront `/cart` page with responsive layout
- Header cart count from backend state
- Functional Add to Cart on product detail (uses `price.offerId`)
- Checkout button disabled (not implemented)
- Login/register triggers `POST /api/cart/merge`

### Security

- Store from `IStoreContext` only (never request body)
- Currency from store context; mismatched offers rejected
- Guest token is opaque random value — no PII in cookie
- Cart resolved from auth/cookie — no cart ID in URLs for ownership

### Future Checkout boundary

Cart stops before checkout. Phase 11 will consume the active cart and create authoritative order price snapshots.

---

## Next Phase: PHASE 11

STOP. Do not begin Phase 11 without explicit approval.
