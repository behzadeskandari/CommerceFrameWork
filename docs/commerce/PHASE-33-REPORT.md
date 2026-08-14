# PHASE 33 — Coupons / Gift Cards / Store Credit / Affiliate — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## Summary

Phase 33 delivers gift cards, checkout store credit application, affiliate/referral tracking with commission ledger, and strengthens the existing coupon stack. Financial operations use transaction-safe ledgers with idempotency — no direct balance mutation, no double spending, no duplicate redemption.

Coupons continue to use the Pricing module (Phase 14) with promotion coupon usage recording (Phase 26 architecture). Phase 26 promotions were not rebuilt.

---

## Backend Delivered

### Gift Cards (Payments)

| Component | Notes |
|---|---|
| `GiftCard` / `GiftCardTransaction` | Issue, redeem, refund ledger |
| `GiftCardAdminService` | Admin CRUD + transaction history |
| `GiftCardValidationService` / `GiftCardRedemptionService` | Validate + idempotent redeem |
| `EfGiftCardRepository.TryRedeemAsync` | Transaction-safe redemption |

### Store Credit Checkout (Customers + Checkout)

- Checkout session fields: `AppliedStoreCreditAmount`, `GiftCardApplied`, `StoreCreditApplied`
- `CheckoutWalletCalculator` applies gift card + store credit after discounts/tax
- `OrderWalletConsumptionHandler` debits wallets on order create with idempotency keys

### Affiliates (Customers)

| Component | Notes |
|---|---|
| `Affiliate` | Referral code, commission rate, store scope |
| `AffiliateReferral` | Customer attribution |
| `AffiliateCommissionAccount` / `AffiliateCommissionTransaction` | Commission ledger |
| `OrderPaidAffiliateCommissionHandler` | Earn commission on order paid |

### Coupons & Promotions (enhancements)

- `CouponValidationRequest.CustomerGroupId` — customer group restrictions enforced
- `IPromotionUsageService` — records `PromotionUsage` when promotion coupon codes are used at order time

### Order Extensions

- `StoreCreditApplied`, `GiftCardApplied`, `AppliedGiftCardCode`, `ReferralCode`, `AffiliateId`
- Grand total validation includes wallet adjustments

---

## API

### Admin
- `GET/POST/PUT/DELETE /api/admin/gift-cards`
- `GET/POST/PUT/DELETE /api/admin/affiliates`
- Existing `/api/admin/coupons`, `/api/admin/discounts`, `/api/admin/promotions`

### Storefront / Checkout
- `POST/DELETE /api/checkout/{id}/gift-cards`
- `POST/DELETE /api/checkout/{id}/store-credit`
- `POST/DELETE /api/checkout/{id}/referral-code`
- Existing cart coupon endpoints

---

## Frontend

- Admin gift card list page (`/payments/gift-cards`)
- Admin affiliate list page (`/customers/affiliates`)
- Checkout wallet UI (gift card, store credit, referral code)
- API services: `GiftCardsApi`, `AffiliatesApi`, checkout wallet methods

---

## Tests

| File | Coverage |
|---|---|
| `Phase33FinancialDomainTests.cs` | Gift card redeem, idempotency, insufficient balance, expiration; affiliate commission earn, idempotency, negative balance rejection |

---

## Key Files

- `Commerce.Payments.Domain/Entities/GiftCard.cs`
- `Commerce.Payments.Application/GiftCards/GiftCardService.cs`
- `Commerce.Customers.Domain/Entities/Affiliate.cs`
- `Commerce.Checkout.Application/Checkout/CheckoutWalletCalculator.cs`
- `Commerce.Orders.Application/Integration/OrderWalletHandlers.cs`
- `Commerce.Promotions.Application/Usage/PromotionUsageService.cs`

---

**Next:** Phase 34 — not started.
