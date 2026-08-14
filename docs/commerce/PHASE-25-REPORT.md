# PHASE 25 — Reviews, Ratings & Wishlist — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## Summary

Phase 25 adds customer product reviews, star ratings, moderation workflow, and per-store wishlists across backend, admin UI, and storefront.

---

## Backend Delivered

### Module: `Commerce.Reviews`

| Layer | Projects |
|---|---|
| Domain | `ProductReview`, `Wishlist`, `WishlistItem`, `ReviewModerationStatus`, `RatingScale` |
| Contracts | Storefront + admin DTOs and service interfaces |
| Application | Review/wishlist services, rating calculator |
| Infrastructure | EF configs, `EfReviewsRepository`, permissions, migration contributor |
| Module | `ReviewsModule` registered in host |

### Cross-module

- `IOrderPurchaseVerifier` in Orders contracts — verifies paid-order purchase for verified-review badge

### Permissions

- `Reviews.View` — browse reviews, ratings, wishlists (admin)
- `Reviews.Manage` — approve/reject/delete reviews

### API Endpoints

**Storefront**

| Method | Route | Auth |
|---|---|---|
| GET | `/api/reviews/products/{productId}` | Public (approved only) |
| GET | `/api/reviews/products/{productId}/summary` | Public |
| POST | `/api/reviews/products/{productId}` | Customer |
| PUT | `/api/reviews/{reviewId}` | Customer (own, pending) |
| GET | `/api/reviews/me/products/{productId}` | Customer |
| GET/POST/DELETE | `/api/wishlist` | Customer |

**Admin**

| Method | Route | Permission |
|---|---|---|
| GET | `/api/admin/reviews` | Reviews.View |
| POST | `/api/admin/reviews/{id}/approve` | Reviews.Manage |
| POST | `/api/admin/reviews/{id}/reject` | Reviews.Manage |
| DELETE | `/api/admin/reviews/{id}` | Reviews.Manage |
| GET | `/api/admin/wishlists` | Reviews.View |

---

## Security

- Customer ID always from `ICurrentCustomerContext`, never request body
- One review per customer/product/store (unique index + conflict response)
- Pending/rejected reviews excluded from public listings
- Wishlist mutations scoped to authenticated customer + current store

---

## Frontend Delivered

| Area | Files |
|---|---|
| API | `reviews.models.ts`, `reviews-api.service.ts` (`ReviewsApi`, `WishlistApi`) |
| Admin | Review list with moderation, wishlist browse |
| Storefront | Product detail ratings/reviews + wishlist button, account wishlist page |
| Nav | Admin sidebar, account links, localization (en/fa) |

---

## Testing

Unit tests in `tests/Commerce/Commerce.Tests.Unit/Reviews/`:

- Domain: moderation, rating validation, wishlist add/remove/duplicate
- Application: duplicate prevention, verified purchase, authorization, approved-only listing, store-scoped wishlist

---

## Database

Tables via EF model contributor:

- `ProductReviews` — unique `(ProductId, CustomerId, StoreId)`
- `Wishlists` — unique `(CustomerId, StoreId)`
- `WishlistItems` — unique `(WishlistId, ProductId)`

Migration contributor: `Reviews_Initial` (schema via EF ensure/migration pipeline).

---

## Out of Scope (deferred)

- Search index rating sync (field reserved in Phase 24)
- Review photos, helpful votes, merchant replies
- Guest wishlist

---

## Build Notes

- `Commerce.Reviews.*` projects build successfully with .NET 10 SDK
- Full solution build may fail on pre-existing unrelated module errors (Pricing, Shipping, Tax, CMS, Payments, Themes)

---

## Next

Phase 26 — awaiting explicit approval.
