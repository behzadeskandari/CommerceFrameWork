# PHASE 26 — Marketing / Promotions / SEO — Preimplementation

**Status:** Preimplementation  
**Date:** 2026-08-12

---

## 1. Inspection Summary

| Area | Current state |
|---|---|
| Discounts (Phase 14) | `Discount`, `Coupon`, `DiscountCalculationEngine` — percentage/fixed, targets, stacking |
| Advanced pricing (Phase 21) | Customer groups, tier prices, pipeline before discounts |
| Promotions / rule engine | **Does not exist** |
| SEO module | **Does not exist** — CMS pages have inline SEO fields only |
| Catalog slugs | Product/category `Slug` on entities |

---

## 2. Architecture

```
Commerce.Promotions.*          ← rule-based promotions (conditions → actions)
Commerce.Seo.*                 ← UrlRecord, metadata, sitemap, robots
Commerce.Framework.Seo/        ← slug normalization contracts
Commerce.Pricing (extended)    ← CustomerGroup eligibility; promotion hook
```

**Principle:** Promotion actions delegate discount math to existing `DiscountCalculationEngine`. Buy-X-Get-Y handled as promotion-specific action.

---

## 3. Promotion Model

```
Promotion
  ├── Conditions[]     (reusable IPromotionConditionHandler)
  ├── Actions[]        (reusable IPromotionActionHandler)
  ├── CombinationRule  (Exclusive | Stackable | SameGroupExclusive)
  ├── Usage limits     (global + per-customer)
  └── Store / date scope
```

### Condition types

MinimumCartSubtotal, MinimumQuantity, CustomerGroup, ProductInCart, CategoryInCart, ProductRestriction, CategoryRestriction, StoreRestriction, UsageLimitRemaining, PerCustomerUsageRemaining

### Action types

PercentageDiscount, FixedAmountDiscount, BuyXGetY, ApplyLinkedDiscount

---

## 4. Combination Rules

| Rule | Behavior |
|---|---|
| Exclusive | Only this promotion applies (highest priority wins among exclusives) |
| Stackable | Combines with other stackable promotions |
| SameGroupExclusive | At most one promotion per `CombinationGroup` |

---

## 5. SEO Model

| Entity | Purpose |
|---|---|
| `UrlRecord` | Friendly URL slug → entity routing |
| `SeoMetadata` | Title, description, canonical, structured JSON-LD |
| `SeoSettings` | Store robots.txt, defaults, sitemap toggle |

---

## 6. API Surface

**Admin:** `/api/admin/promotions`, `/api/admin/seo/settings`, `/api/admin/seo/url-records`, `/api/admin/seo/metadata`  
**Storefront:** `/robots.txt`, `/sitemap.xml`, `/api/seo/resolve/{slug}`, `/api/seo/metadata/{entityType}/{entityId}`

---

## 7. Permissions

- `Promotions.View`, `Promotions.Manage`
- `Seo.View`, `Seo.Manage`

---

## 8. Testing

Eligibility, expiration, usage limits, customer/product/category restrictions, combination rules, store isolation, price calculation integration, SEO slug resolution, sitemap generation.

---

## 9. Out of Scope

Gift cards, affiliate program, email campaigns, manufacturer targeting (no Catalog entity yet).
