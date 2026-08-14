# PHASE 35 — Real Payment Providers — Pre-Implementation

**Status:** Pre-implementation  
**Date:** 2026-08-13

---

## Scope

Phase 35 adds production-grade real payment providers as plugins on top of the existing Phase 17 payment engine and Phase 19 plugin system.

| Provider | Type | Notes |
|---|---|---|
| **ZarinPal** | Redirect (IRR) | Sandbox + production API, server-side verify |
| **Stripe** | Checkout Session | Test mode keys, webhook signature verification |

## Design Principles

- **No redesign** of `IPaymentProvider` or plugin runtime unless critical defect found
- **Never trust browser callbacks** — always verify server-side with provider API
- **Secrets in plugin settings** (`IPluginSettingDefinitionProvider`, store-scoped, `IsSecret: true`)
- **Idempotency** — pass through `PaymentRequest.IdempotencyKey` / `RefundRequest.IdempotencyKey`; callback dedup via `PaymentCallbackRecord`
- **Correlation** — `paymentId`, `orderId`, `storeId` in metadata / callback URL

## Lifecycle Support

Initiation → Redirect → Callback → Server verification → Capture (Stripe) → Refund (Stripe) → Unknown state / reconciliation

## Core Extensions (minimal)

- `IPaymentProviderSettingsReader` — plugins read store settings without referencing Infrastructure
- `IPaymentCallbackDispatcher` + provider-specific `IPaymentCallbackHandler`
- `ProviderPaymentId` on `PaymentRequest` / `RefundRequest` for capture/void/refund

## Out of Scope

- Card tokenization in Commerce DB
- ZarinPal refund API (document panel-only for v1)
- Additional gateways beyond ZarinPal + Stripe (same contract pattern for future)

---

**Next:** Implementation per this plan.
