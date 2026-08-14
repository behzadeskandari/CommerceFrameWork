# PHASE 33 — Coupons / Gift Cards / Store Credit / Affiliate — Pre-Implementation

**Status:** Pre-implementation  
**Date:** 2026-08-12

---

## Scope

Phase 33 completes the financial redemption layer for commerce:

| Area | Approach |
|---|---|
| **Coupons** | Extend existing Pricing coupons (Phase 14) + promotion coupon usage (Phase 26) |
| **Gift cards** | New Payments module ledger (`GiftCard` / `GiftCardTransaction`) |
| **Store credit** | Wire Phase 32 wallet into checkout and order consumption |
| **Affiliates** | New Customers module (`Affiliate`, referral tracking, commission ledger) |

## Design Principles

- All balances updated via transaction ledgers with idempotency keys
- No direct balance mutation on aggregates
- No negative balances unless explicitly supported (payout debits on commission account)
- Race-safe redemption via DB transactions + unique idempotency indexes
- Multi-store isolation via `StoreId`

## Integration Points

- **Cart/Checkout:** Coupons on cart; gift card, store credit, referral code on checkout
- **Pricing:** `CouponValidationService` validates customer group eligibility
- **Promotions:** `IPromotionUsageService` records usage when promotion coupon codes are consumed
- **Orders:** Wallet debit on order created; affiliate commission on order paid

## Out of Scope

- Rebuilding Phase 26 promotion rule engine
- Loyalty point redemption at checkout (Phase 32 catalog only)
- Payment gateway gift card issuance plugins

---

**Next:** Implementation per this plan.
