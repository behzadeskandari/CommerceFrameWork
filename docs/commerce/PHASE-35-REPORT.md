# PHASE 35 — Real Payment Providers — Report

**Status:** Complete  
**Date:** 2026-08-13

---

## Summary

Phase 35 implements ZarinPal and Stripe as payment provider plugins using the existing `IPaymentProvider` contract and plugin runtime. Browser callbacks are never treated as proof of payment — both providers verify transactions server-side. Credentials are stored as secret plugin settings per store.

---

## Contract Verification (Phase 17)

Confirmed unchanged core lifecycle via `IPaymentProvider`:

- `CreatePaymentAsync` → initiation / redirect
- `VerifyPaymentAsync` / `GetPaymentStatusAsync` → server verification / reconciliation hints
- `CaptureAsync` / `VoidAsync` / `RefundAsync` → post-auth operations

Extended minimally:

- `PaymentRequest.ProviderPaymentId` / `RefundRequest.ProviderPaymentId` for capture/void/refund
- `IPaymentProviderSettingsReader` for store-scoped plugin settings
- `IPaymentCallbackDispatcher` with provider `IPaymentCallbackHandler` hooks
- `PaymentCallbackContext.Headers` for webhook signature verification

---

## Plugins Delivered

### Payment.ZarinPal

| Component | Purpose |
|---|---|
| `ZarinPalPaymentProvider` | Redirect initiation, server-side verify API |
| `ZarinPalCallbackHandler` | Enriches callback with order amount/currency from payment record |
| `ZarinPalApiClient` | HTTP client for v4 request/verify endpoints |
| `ZarinPalSettingDefinitionProvider` | MerchantId, Sandbox, CallbackBaseUrl |

**Security:** `Status=OK` from browser is ignored unless verify API returns code 100/101. `Status=NOK` → cancelled without verify call.

### Payment.Stripe

| Component | Purpose |
|---|---|
| `StripePaymentProvider` | Checkout Session creation, session retrieve verification |
| `StripeCallbackHandler` | Browser return + webhook (`Stripe-Signature` HMAC, 5-minute tolerance) |
| `StripeApiClient` | REST API for sessions, refunds, capture, void |
| `StripeSettingDefinitionProvider` | SecretKey, WebhookSecret (secret), Sandbox flag |

**Metadata:** `commerce_payment_id`, `commerce_order_id`, `commerce_store_id` on Checkout Session for correlation.

---

## Payment Lifecycle

```
POST /api/payments (Idempotency-Key)
  → IPaymentProvider.CreatePaymentAsync
  → RedirectRequired + redirectUrl

Customer completes at gateway
  → GET/POST /api/payments/callback/{provider}
  → IPaymentCallbackDispatcher
  → IPaymentCallbackHandler (optional)
  → IPaymentProvider.VerifyPaymentAsync (server-side)
  → PaymentCallbackRecord dedup
  → Order sync
```

Stripe webhooks: `POST /api/payments/callback/Payment.Stripe` with `Stripe-Signature` header.

---

## Admin / Configuration

1. Install + enable plugin (`Payment.ZarinPal` or `Payment.Stripe`)
2. Configure store settings via `/api/admin/plugins/{systemName}/settings`
3. Activate payment method (`zarinpal` / `stripe`) — seeded inactive in dev seeder

Dev seeder adds inactive ZarinPal and Stripe methods when `Commerce:Payments:SeedDevelopmentData` is true.

---

## Tests

**Project:** `Commerce.Tests.Unit.PaymentProviders` — **9 passing**

| Test | Coverage |
|---|---|
| ZarinPal redirect initiation | Successful request → RedirectRequired |
| ZarinPal verify rejection | Browser OK + failed verify API → Failed |
| ZarinPal verify capture | Verify code 100 → Captured |
| ZarinPal cancel | Browser NOK → Cancelled |
| Stripe session creation | Redirect URL returned |
| Stripe session verify | paid session → Captured |
| Stripe webhook signature | Invalid rejected, valid accepted |
| Stripe unknown state | unpaid session → unknown_state / Initiated |

Tests use stub HTTP handlers — no live provider credentials required.

---

## Limitations (honest)

- **ZarinPal refunds:** Not implemented via API in v1 (panel-only); returns `not_supported`
- **ZarinPal currency:** IRR only (amount in Rials)
- **Live sandbox verification:** Requires merchant test credentials configured in admin; unit tests mock HTTP
- **Full solution build:** May still fail on pre-existing unrelated errors (Themes, Plugins, etc.)

---

**Phase 35 complete. STOP.**
