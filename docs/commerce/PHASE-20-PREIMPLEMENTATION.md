# PHASE 20 — Digital Products & Downloads — Pre-Implementation

**Status:** Pre-implementation audit complete  
**Date:** 2026-08-12

---

## 1. Objective

Deliver secure digital product fulfillment: file association, download entitlements on paid orders, authorized download execution, admin configuration, and storefront customer access.

## 2. Existing State (Inspected)

| Area | State |
|---|---|
| `ProductType` | `Digital`, `Downloadable`, `Virtual` enum values exist |
| Checkout shipping skip | `CheckoutRequiresShippingEvaluator` skips shipping for digital types |
| Shipping calculation | `ShippingCalculationService.IsNonShippableProductType` skips digital lines |
| Media storage | `IMediaStorage` + `LocalMediaStorage` + `MediaAsset` with `IsPublic` flag |
| Orders | `Order`, `OrderItem`, `PaymentStatus`, guest `GuestAccessToken` |
| Payment sync | `OrderPaymentSyncService.SyncPaidAsync` marks order paid |
| Downloads | **Not implemented** — no entitlements, no secure download API |

## 3. Design Decisions

### 3.1 Module: `Commerce.Downloads`

Standard Clean Architecture layers: Domain, Contracts, Application, Infrastructure, `Commerce.Modules.Downloads`.

### 3.2 Domain Model

| Entity | Purpose |
|---|---|
| `ProductDownloadSettings` | Per-product limits (max downloads, expiration days) |
| `ProductDownloadFile` | File linked to product via `MediaAssetId` |
| `DownloadEntitlement` | Granted on order paid — ties customer/order/item/product |
| `DownloadHistoryEntry` | Audit each download attempt |

### 3.3 Storage

`IDownloadStorage` abstraction wrapping `IMediaStorage` for private file reads. No filesystem paths exposed in APIs.

Download files use **private** `MediaAsset` records (`IsPublic = false`).

### 3.4 Entitlement Grant

`IOrderPaidHandler` in `Orders.Contracts` — `OrderPaymentSyncService` invokes handlers after `MarkPaymentPaid`. Downloads module grants entitlements for digital order items with configured files.

### 3.5 Authorization Rules

Download allowed when:
- Entitlement exists for customer + file's product
- Order `PaymentStatus` is `Paid` (or `Authorized` if configured — **Paid only** in Phase 20)
- Not expired, not over limit, not revoked
- File exists and storage key is valid

### 3.6 Shared Digital Product Helper

`DigitalProductTypes` in `Catalog.Contracts` — single source for Digital/Downloadable/Virtual checks. Refactor checkout/shipping to use it.

### 3.7 APIs

| Audience | Routes |
|---|---|
| Admin | `/api/admin/downloads/products/{productId}/settings`, `/files`, history |
| Storefront | `/api/downloads` (list), `/api/downloads/{entitlementId}/files/{fileId}` (execute) |

### 3.8 Angular

- Admin: digital downloads section on product edit (when type is digital)
- Storefront: `/account/downloads` page

## 4. Out of Scope

- S3/Azure/GCS storage providers (abstraction only)
- Signed URL delivery (future)
- Entitlement revocation on refund (documented limitation)
- Guest download UI (guest token support in domain; storefront requires auth in Phase 20)

## 5. Dependencies

Downloads module depends on: Core, Store, Catalog, Media, Customers, Orders, Payments (via handler contract only).
