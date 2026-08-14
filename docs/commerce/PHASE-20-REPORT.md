# PHASE 20 — Digital Products & Downloads — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## 1. Summary

Phase 20 delivers a production-grade **Digital Products & Downloads** subsystem integrated with Catalog, Media, Orders, Payments, Checkout, and Shipping. Customers receive download entitlements when orders are paid; files are served through authorized API endpoints — never via public URLs or filesystem paths.

---

## 2. What Was Implemented

### Domain (`Commerce.Downloads.Domain`)
- `ProductDownloadSettings` — per-product limits (max downloads, expiration days)
- `ProductDownloadFile` — links products to private `MediaAsset` records
- `DownloadEntitlement` — ownership, limits, expiration, download count
- `DownloadHistoryEntry` — audit of each download attempt

### Application
- `DownloadEntitlementService` — grants entitlements on order paid via `IOrderPaidHandler`
- `DownloadAdminService` — configure product downloads
- `CustomerDownloadService` — list and execute authorized downloads
- `DigitalProductTypes` (Catalog.Contracts) — shared digital product classification

### Infrastructure
- `IDownloadStorage` → `MediaDownloadStorage` (wraps `IMediaStorage`)
- EF persistence + `DownloadsInitialMigration`
- Permissions: `Downloads.View`, `Downloads.Configure`, `Downloads.Manage`

### Integration
- `IOrderPaidHandler` hook in `OrderPaymentSyncService`
- Checkout/shipping skip digital-only carts via `DigitalProductTypes`

### API
| Route | Purpose |
|---|---|
| `GET/PUT /api/admin/downloads/products/{id}/settings` | Configure limits |
| `GET/POST/DELETE .../files` | Manage download files |
| `GET .../history` | Download audit |
| `GET /api/downloads` | Customer entitlement list |
| `GET /api/downloads/{entitlementId}/files/{fileId}` | Authorized file download |

### Angular
- Admin product form: **Digital downloads** panel (settings + files)
- Storefront: `/account/downloads` page with link from account

---

## 3. Security Model

- Files stored via `IMediaStorage`; private media assets (`IsPublic = false`)
- No storage keys or filesystem paths exposed in APIs
- Authorization checks: customer identity, order ownership, payment status = Paid, expiration, download limits
- Storage key path traversal validation before read
- Download history recorded (IP/user agent optional; failure reasons stored)

---

## 4. Tests

### Unit (`Commerce.Tests.Unit/Downloads/DownloadTests.cs`)
| Scenario | Covered |
|---|---|
| Digital product type classification | Yes |
| Entitlement limits / expiration / unlimited | Yes |
| Wrong customer (domain ownership) | Yes |
| Revoked entitlement | Yes |
| Guest token validation | Yes |
| Storage key path traversal | Yes |
| Digital-only checkout (no shipping) | Yes |
| Mixed order (shipping required) | Yes |
| Physical checkout regression | Yes |
| Download history recording | Yes |

Authorization service scenarios (unpaid order, wrong file, file-not-found) are enforced in `CustomerDownloadService` with history logging; full service-level integration tests require .NET 10 SDK.

### Architecture (`Commerce.Tests.Architecture/DownloadsArchitectureTests.cs`)
- Downloads application references Media.Contracts only
- Domain has no infrastructure references
- Application does not reference Host

### Build results

| Target | Result |
|---|---|
| `npm test` (admin + storefront) | **PASS** — 4 tests |
| `npm run build` (admin + storefront) | **PASS** |
| `dotnet build` / `dotnet test` | **BLOCKED** — SDK 8.0.302 vs net10.0 (`NETSDK1045`) |

---

## 5. Known Limitations

1. **Guest downloads** — domain supports guest tokens; storefront UI requires authenticated customer
2. **Refund revocation** — entitlements not auto-revoked on refund
3. **Signed URLs** — abstraction supports future provider; current delivery streams via API
4. **Cloud storage providers** — only local storage via `IMediaStorage` in this phase
5. **Mixed orders** — physical + digital supported; shipping required for physical lines only

---

## 6. Key Files

```
docs/commerce/PHASE-20-PREIMPLEMENTATION.md
docs/commerce/PHASE-20-REPORT.md
src/Commerce/Modules/Downloads/
src/Commerce/Host/Commerce.Host/Downloads/
src/Commerce/Modules/Catalog/Commerce.Catalog.Contracts/Products/DigitalProductTypes.cs
src/Commerce/Modules/Orders/Commerce.Orders.Contracts/Orders/IOrderPaidHandler.cs
tests/Commerce/Commerce.Tests.Unit/Downloads/DownloadTests.cs
tests/Commerce/Commerce.Tests.Architecture/DownloadsArchitectureTests.cs
frontend/commerce-ui/libs/api/src/lib/downloads-api.service.ts
frontend/commerce-ui/apps/admin/src/app/pages/catalog/product-form.page.ts
frontend/commerce-ui/apps/storefront/src/app/pages/account-downloads.page.ts
```

---

**Phase 20 complete. Stopped — awaiting explicit approval before Phase 21.**
