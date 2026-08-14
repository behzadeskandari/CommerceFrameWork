# PHASE 32 — Customer Account, Loyalty & Segmentation — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## Summary

Phase 32 extends the Customers module into a full customer account platform: preferences, dynamic segments, loyalty points with rewards, store credit wallet, purchase history, activity logging, and admin tooling. Customer groups (Pricing module) are wired to admin assignment. All balances use transaction-ledgers with idempotency — no direct balance mutation.

Multi-store isolation is enforced via `StoreId` on loyalty, store credit, segments, and store-scoped preferences/activity.

---

## Backend Delivered

### Domain

| Entity | Notes |
|---|---|
| `CustomerPreference` | Per customer, optional store scope |
| `CustomerSegment` / `CustomerSegmentRule` | CustomerGroup, MinOrderCount, MinLifetimeSpend rules |
| `CustomerSegmentMembership` | Evaluated assignments |
| `LoyaltyAccount` / `LoyaltyTransaction` | Points ledger with idempotent `PostTransaction` |
| `LoyaltyReward` / `LoyaltyRewardRedemption` | Catalog + idempotent redemption |
| `StoreCreditAccount` / `StoreCreditTransaction` | Decimal wallet with expiration support |
| `CustomerActivityLog` | Append-only activity trail |

### Services

- Preference, segment, loyalty, store credit, activity services
- Admin account service (group assign, tax profile, deactivate, purchase history via Orders)
- Storefront account overview
- `OrderPaidLoyaltyHandler` implements `IOrderPaidHandler` — earns 1 point per currency unit on paid orders

### Permissions Added

- `Customers.Manage`, `Customers.Loyalty.View/Manage`, `Customers.Segments.View/Manage`, `Customers.StoreCredit.Manage`

### Integration

| Module | Integration |
|---|---|
| Pricing | Customer group assignment (existing `CustomerGroupId` on Customer) |
| Orders | Purchase history, order-paid loyalty earn |
| Checkout/Payments | `IStoreCreditReader` for future checkout application |

---

## Admin API

- Extended `AdminCustomerAccountController` — group, tax, deactivate, preferences, loyalty, store credit, activity, purchase history
- `AdminCustomerSegmentsController` — segment CRUD + evaluate
- `AdminLoyaltyRewardsController` — reward catalog CRUD

## Storefront API

- `CustomerAccountController` at `/api/customers/me/account/*` — overview, preferences, loyalty, rewards, redeem, store credit, activity

---

## Frontend

### Storefront
- `/account/preferences`, `/account/loyalty`, `/account/activity`
- Extended account hub with navigation links
- `CustomerAccountApi` + models

### Admin
- Extended customer detail — group assignment, purchase history, loyalty, activity
- `/customers/segments`, `/customers/loyalty-rewards` admin pages

---

## Tests

| File | Coverage |
|---|---|
| `CustomerAccountDomainTests.cs` | Earn, spend, expire, idempotent duplicate, insufficient balance (loyalty + store credit), segment/reward validation |

`Commerce.Customers.Application` and `Commerce.Customers.Infrastructure` build cleanly.

---

## Key Files

- `Commerce.Customers.Domain/Entities/LoyaltyAccount.cs`
- `Commerce.Customers.Domain/Entities/StoreCreditAccount.cs`
- `Commerce.Customers.Domain/Entities/CustomerSegment.cs`
- `Commerce.Customers.Application/CustomerAccount/LoyaltyService.cs`
- `Commerce.Customers.Application/CustomerAccount/StoreCreditService.cs`
- `Commerce.Host/Customers/AdminCustomerAccountController.cs`

---

**Next:** Phase 33 — not started.
