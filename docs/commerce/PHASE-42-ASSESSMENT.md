# Phase 42 — Marketplace / Multi-Vendor (Assessment)

**Status:** Deferred — not implemented  
**Date:** 2026-08-13  
**Decision:** Skip Phase 42 for this project

---

## Decision

Phase 42 is **optional** and was **not implemented** because the Commerce Framework project does not currently require multi-vendor marketplace functionality.

Per phase instructions:

> Implement ONLY if the project requires marketplace functionality.  
> Do not implement this phase merely because it exists in the roadmap.

This assessment found no product requirement, architecture commitment, or existing code path that needs sellers, commissions, or seller payouts.

---

## Evidence reviewed

| Source | Finding |
|--------|---------|
| `docs/commerce/IMPLEMENTATION-ROADMAP.md` | Phases 0–41 documented; no Phase 42 scope or deliverables |
| `docs/commerce/PHASE-18-REPORT.md` | Explicit: **"No online marketplace"** (plugin ZIP install only) |
| `docs/commerce/PHASE-19-*` | "Marketplace" refers to a **plugin store**, not multi-vendor commerce |
| Codebase search | No `Seller`, `Marketplace`, `MultiVendor`, commission, or payout entities |
| Current commerce model | **Multi-store** (`StoreId` on catalog, orders, pricing) — operator-owned storefronts, not third-party sellers |

The platform today is a **modular monolith for store operators** (single or multi-store). That is different from a **marketplace** where independent sellers list products and receive settlements.

---

## Entity model (when/if marketplace is needed later)

Do **not** conflate these roles. Each has distinct ownership, permissions, and ledger boundaries.

### Platform

The commerce operator running the marketplace software.

- Owns global configuration, fee rules, payout policies, dispute workflow
- Holds the **platform revenue** ledger (commissions, fees)
- Does **not** own seller catalog or seller settlement balances as store inventory

### Store

A **commercial storefront context** in the platform (URL, currency, theme, shipping/tax scope).

- Today: `Commerce.Store.*` — catalog, checkout, and orders are scoped by `StoreId`
- In a marketplace: a store may remain the **customer-facing shopfront**, while products are **offered by sellers** within that store
- Store configuration ≠ seller identity. A store is not a seller.

### Seller

An independent merchant onboarded to list and fulfill products on the platform.

- Has onboarding state, KYC/business profile, seller-specific permissions
- Owns **seller catalog** and **seller product** listings (linked to platform catalog SKUs or seller-specific offers)
- Receives **seller orders** (order lines attributed to seller) and **commissions** deducted by the platform
- Has a **seller balance** (payable) separate from store operator accounts and customer wallets

### Customer

The buyer.

- Existing `Commerce.Customers.*` — accounts, addresses, orders, loyalty, wallet/credit where enabled
- Pays the **platform/store checkout**; marketplace settlement to sellers happens **after** payment capture, via seller accounting — not by mixing customer identity with seller ownership

---

## What Phase 42 would include (future scope)

If the project later requires marketplace functionality, implement as a **new bounded module** (e.g. `Commerce.Modules.Marketplace`) — not as extensions to `Store` ownership.

| Capability | Notes |
|------------|-------|
| Sellers | Aggregate root; lifecycle: draft → pending → approved → suspended |
| Seller onboarding | Application, verification, agreement acceptance |
| Seller permissions | Separate from admin/store permissions (`Seller.*` permission tree) |
| Seller catalog | Seller-scoped product listings; link to shared catalog or seller-owned SKUs |
| Seller products | `SellerId` on listing/offer — **never** replace `StoreId` with seller as store owner |
| Seller orders | Order line attribution + split fulfillment; sub-order per seller |
| Seller commissions | Rule engine (%, fixed, category-based); immutable commission records on capture |
| Seller balances | **Transaction-safe ledger** (double-entry or append-only journal + balance snapshots) |
| Seller payouts | Payout batches, hold periods, reconciliation; idempotent payout commands |
| Seller dashboard | Seller portal API + UI (distinct from admin and storefront) |

### Financial rules (mandatory when implemented)

- Seller balances must use **transaction-safe accounting** (DB transactions, optimistic concurrency on balance rows, append-only ledger entries)
- **Never** mix seller ownership with store ownership — `StoreId` scopes storefront; `SellerId` scopes marketplace attribution
- Platform commission, seller payable, and customer payment are **separate ledger accounts**
- Reuse patterns from existing wallet/credit modules only where semantics align; do not overload `StoreCredit` for seller settlements

---

## How to trigger implementation

Implement Phase 42 only when **all** of the following are true:

1. Stakeholder sign-off on multi-vendor marketplace as a **product requirement**
2. Updated `IMPLEMENTATION-ROADMAP.md` with Phase 42 deliverables and acceptance criteria
3. Architecture review confirming seller module boundaries vs. `Store`, `Customers`, `Orders`, `Payments`

Until then, treat Phase 42 as **out of scope**.

---

## Tests

No Phase 42 tests were added — nothing was implemented.

When implemented, minimum test coverage should include:

- Seller onboarding state transitions
- Permission isolation (seller cannot access other sellers' catalog/orders)
- Commission calculation on order capture/refund
- Ledger concurrency (parallel order captures do not corrupt balance)
- Payout idempotency
- Clear separation: `StoreId` vs `SellerId` on products and order lines

---

## Related documentation

- [ARCHITECTURE.md](./ARCHITECTURE.md) — modular monolith, multi-store model
- [MODULE-MAP.md](./MODULE-MAP.md) — current module boundaries
- [PHASE-41-REPORT.md](./PHASE-41-REPORT.md) — latest completed phase
