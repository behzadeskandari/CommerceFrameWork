# PHASE 25 — Reviews, Ratings & Wishlist — Preimplementation

**Status:** Preimplementation  
**Date:** 2026-08-12

---

## 1. Inspection Summary

| Area | Current state |
|---|---|
| Reviews / ratings | **Does not exist** |
| Wishlist | **Does not exist** |
| Search index | `Rating` field reserved (currently `null`) |
| Customer auth | `ICurrentCustomerContext` + `commerce:customer_id` claim |
| Store isolation | `IStoreContext.CurrentStoreId` via middleware |
| Verified purchase | Orders + `PaymentStatus.Paid` pattern (Downloads module) |
| Moderation | No prior pattern; new `ReviewModerationStatus` enum |

---

## 2. Architecture

```
Commerce.Reviews.Domain/        ← ProductReview, Wishlist, WishlistItem
Commerce.Reviews.Contracts/     ← Admin + storefront DTOs and service interfaces
Commerce.Reviews.Application/   ← Review + wishlist services, rating aggregation
Commerce.Reviews.Infrastructure/← EF configs, repository, permissions, migration
Commerce.Modules.Reviews/       ← Module registration
Commerce.Host/Reviews/          ← Admin + storefront controllers
```

**Cross-module contract:** `IOrderPurchaseVerifier` in `Commerce.Orders.Contracts`, implemented in Orders infrastructure.

---

## 3. Domain Model

### ProductReview

| Field | Notes |
|---|---|
| ProductId, CustomerId, StoreId | Store-scoped, unique per customer+product |
| Rating | 1–5 (`RatingScale`) |
| Title, Content | Required, length-limited |
| ModerationStatus | Pending (default), Approved, Rejected |
| IsVerifiedPurchase | Set at submit via order lookup |
| CreatedAtUtc, UpdatedAtUtc | Audit |

### Wishlist

| Field | Notes |
|---|---|
| CustomerId, StoreId | One wishlist per customer per store |
| Items | ProductId + AddedAtUtc |

---

## 4. Public vs Admin Behavior

| Endpoint | Visibility |
|---|---|
| Storefront product reviews | **Approved only** |
| Storefront rating summary | **Approved only** |
| Customer own review | Any status (authenticated) |
| Admin review list | All statuses, filterable |

Unapproved reviews are never returned on public storefront endpoints.

---

## 5. Security

| Threat | Mitigation |
|---|---|
| Review spoofing | CustomerId taken from `ICurrentCustomerContext`, never from request body |
| Unauthorized edit | Ownership check + only Pending reviews editable by customer |
| Customer impersonation | `[Authorize]` on mutating endpoints |
| Duplicate abuse | Unique index + conflict on second review per product |

---

## 6. Permissions

| Permission | Purpose |
|---|---|
| `Reviews.View` | List reviews, ratings, wishlists (admin) |
| `Reviews.Manage` | Moderate, delete reviews |

---

## 7. API Surface

**Storefront**

- `GET /api/reviews/products/{productId}` — approved reviews + summary
- `GET /api/reviews/products/{productId}/summary` — rating aggregate
- `POST /api/reviews/products/{productId}` — submit review (auth)
- `PUT /api/reviews/{reviewId}` — update own pending review (auth)
- `GET /api/reviews/me/products/{productId}` — own review (auth)
- `GET /api/wishlist` — list items with availability (auth)
- `POST /api/wishlist/items` — add product (auth)
- `DELETE /api/wishlist/items/{productId}` — remove (auth)

**Admin**

- `GET /api/admin/reviews` — filter by store, product, status
- `POST /api/admin/reviews/{id}/approve|reject`
- `DELETE /api/admin/reviews/{id}`
- `GET /api/admin/wishlists` — operational wishlist browse

---

## 8. Rating Aggregation

Computed from **approved** reviews only:

- Average rating (rounded to 1 decimal)
- Rating count
- Distribution (counts per star 1–5)

---

## 9. Frontend

| App | Pages |
|---|---|
| Admin | Review list + moderation actions, wishlist browse |
| Storefront | Product detail reviews/ratings, account wishlist |

---

## 10. Testing

- Domain: moderation transitions, rating validation, wishlist add/remove
- Application: authorization, duplicate prevention, verified purchase flag
- Aggregation: only approved reviews counted
- Store isolation: reviews and wishlists scoped by store

---

## 11. Out of Scope (Phase 25)

- Search index rating sync (field reserved in Phase 24)
- Review photos, helpful votes, merchant replies
- Guest wishlist / shareable lists
