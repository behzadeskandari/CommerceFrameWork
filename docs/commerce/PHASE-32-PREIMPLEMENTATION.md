# PHASE 32 — Customer Account, Loyalty & Segmentation — Pre-Implementation

**Status:** Complete (pre-implementation)  
**Date:** 2026-08-12

---

## Objective

Extend the Customers module with account enrichment: profile preferences, customer groups assignment, dynamic segments, loyalty points, rewards, store credit, purchase history, and activity logging — with multi-store isolation and financial safety.

---

## Domain Additions

| Entity | Purpose |
|---|---|
| `CustomerPreference` | Key-value preferences per customer/store |
| `CustomerSegment` + `CustomerSegmentRule` | Rule-based segments (group, order count, lifetime spend) |
| `CustomerSegmentMembership` | Materialized segment assignments |
| `LoyaltyAccount` + `LoyaltyTransaction` | Points ledger (no direct balance mutation) |
| `LoyaltyReward` + `LoyaltyRewardRedemption` | Redeemable rewards with idempotency |
| `StoreCreditAccount` + `StoreCreditTransaction` | Store-scoped wallet ledger |
| `CustomerActivityLog` | Append-only activity audit |

---

## Financial Safety

- Balances updated only via `PostTransaction` on account aggregates
- Idempotency keys on all earn/spend/credit/debit/redeem operations
- Full transaction history preserved (append-only)
- Store + customer isolation on all account entities

---

## Services

| Service | Responsibility |
|---|---|
| `CustomerPreferenceService` | CRUD preferences |
| `CustomerSegmentAdminService` | Segment CRUD + evaluation |
| `LoyaltyService` | Earn, spend, expire, redeem |
| `LoyaltyRewardAdminService` | Reward catalog admin |
| `StoreCreditService` / `IStoreCreditReader` | Credit, debit, expire, checkout read |
| `CustomerActivityService` | Activity logging and listing |
| `CustomerAccountAdminService` | Group assign, tax, deactivate, purchase history |
| `CustomerAccountStorefrontService` | Account overview |
| `OrderPaidLoyaltyHandler` | Auto-earn on order paid |

---

## Admin API

| Endpoint | Permission |
|---|---|
| `PUT /api/admin/customers/{id}/group` | Customers.Manage |
| `GET /api/admin/customers/{id}/purchase-history` | Customers.View |
| `GET/POST .../loyalty`, `.../store-credit` | Customers.Loyalty.* / StoreCredit.Manage |
| `/api/admin/customer-segments/*` | Customers.Segments.* |
| `/api/admin/loyalty-rewards/*` | Customers.Loyalty.* |

## Storefront API

| Endpoint | Auth |
|---|---|
| `GET /api/customers/me/account/overview` | Customer |
| `GET/PUT .../preferences` | Customer |
| `GET .../loyalty`, `.../rewards`, `POST .../redeem` | Customer |
| `GET .../store-credit`, `.../activity` | Customer |

---

## Tests Planned

- Earn, spend, expiration, duplicate idempotency (loyalty + store credit)
- Insufficient balance rejection
- Segment rule validation
- Authorization via permissions
