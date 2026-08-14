# PHASE 26 — Marketing / Promotions / SEO — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## Summary

Phase 26 adds a rule-based promotion engine integrated with Phase 21 pricing, plus centralized SEO (friendly URLs, metadata, robots.txt, sitemap) with admin management.

---

## Backend Delivered

### Module: `Commerce.Promotions`

| Layer | Contents |
|---|---|
| Domain | `Promotion`, `PromotionCondition`, `PromotionAction`, `PromotionUsage`, combination/enums |
| Contracts | Admin CRUD DTOs, `IPromotionEvaluationService`, `PromotionEvaluationContext` |
| Application | Condition evaluators, action executors, `PromotionRuleEngine`, `PromotionCombinationSelector`, `PromotionEvaluationService`, `PromotionAdminService` |
| Infrastructure | EF configs, repository, permissions, migration contributor |
| Module | `PromotionsModule` registered in host |

**Promotion capabilities**

- Percentage / fixed / Buy X Get Y / linked discount actions
- Conditions: min cart subtotal/qty, customer group, product/category in cart, product/category restrictions, store, usage limits
- Combination rules: `Exclusive`, `Stackable`, `SameGroupExclusive`
- Date windows, global/per-customer usage limits, coupon code requirement, store scope

**Pricing integration**

- `PromotionPricingIntegrator` wired into `PriceCalculationService`
- `CustomerGroupId` added to discount eligibility and price calculation contexts (Phase 21 extension)

### Module: `Commerce.Seo`

| Layer | Contents |
|---|---|
| Framework | `Commerce.Framework.Seo` — `SlugNormalizer`, `SeoEntityNames` |
| Domain | `UrlRecord`, `SeoMetadata`, `SeoSettings` |
| Application | `SeoAdminService`, `SeoStorefrontService` (slug resolve, metadata, robots, sitemap XML) |
| Infrastructure | EF configs, repository, permissions, migration contributor |
| Module | `SeoModule` registered in host |

### Permissions

| Module | Permissions |
|---|---|
| Promotions | `Promotions.View`, `Promotions.Manage` |
| SEO | `Seo.View`, `Seo.Manage` |

### API Endpoints

**Admin — Promotions**

| Method | Route | Permission |
|---|---|---|
| GET | `/api/admin/promotions` | Promotions.View |
| GET | `/api/admin/promotions/{id}` | Promotions.View |
| POST | `/api/admin/promotions` | Promotions.Manage |
| PUT | `/api/admin/promotions/{id}` | Promotions.Manage |
| POST | `/api/admin/promotions/{id}/activate` | Promotions.Manage |
| POST | `/api/admin/promotions/{id}/deactivate` | Promotions.Manage |
| DELETE | `/api/admin/promotions/{id}` | Promotions.Manage |

**Admin — SEO**

| Method | Route | Permission |
|---|---|---|
| GET | `/api/admin/seo/url-records` | Seo.View |
| PUT | `/api/admin/seo/url-records` | Seo.Manage |
| PUT | `/api/admin/seo/metadata` | Seo.Manage |
| GET/PUT | `/api/admin/seo/settings/{storeId}` | Seo.View / Seo.Manage |

**Storefront / public**

| Method | Route | Auth |
|---|---|---|
| GET | `/api/seo/resolve/{slug}` | Public |
| GET | `/api/seo/metadata/{entityName}/{entityId}` | Public |
| GET | `/robots.txt` | Public |
| GET | `/sitemap.xml` | Public |

---

## Rule Engine Architecture

```
Promotion
  → Conditions (pluggable IPromotionConditionEvaluator)
  → Eligibility gate
  → Actions (IPromotionActionExecutor)
  → PromotionDiscountEffect
  → PromotionCombinationSelector
  → PriceCalculationService (cart/line totals)
```

Linked discounts delegate amount math to the existing `DiscountCalculationEngine` to avoid duplicate pricing logic.

---

## Frontend Delivered

| Area | Files |
|---|---|
| API | `promotions.models.ts`, `promotions-api.service.ts`, `seo.models.ts`, `seo-api.service.ts` |
| Admin | Promotion list/form, SEO settings, URL record management |
| Nav | Marketing promotions + SEO links with permission guards |
| i18n | English + Persian keys |

---

## Testing

**Project:** `tests/Commerce/Commerce.Tests.Unit.PromotionsSeo`

| Area | Coverage |
|---|---|
| Eligibility | Min cart subtotal, customer group, coupon required |
| Expiration | End date blocks promotion |
| Usage limits | Per-customer limit enforced |
| Combination | Exclusive + same-group exclusive selection |
| Store isolation | Wrong store blocked |
| Price calculation | Percentage cart discount, Buy X Get Y unit math |
| SEO | Slug normalization, path traversal rejection, URL record + settings domain |

**Result:** 14 tests passed (`dotnet test tests/Commerce/Commerce.Tests.Unit.PromotionsSeo`)

---

## Build Notes

- `Commerce.Modules.Promotions` and `Commerce.Modules.Seo` build successfully
- Full solution host build may still depend on unrelated pre-existing module errors outside Phase 26 scope
- Minor ripple fixes applied for Phase 21 `CustomerGroupId` on shared pricing contexts

---

## Out of Scope (Phase 26)

- Gift cards, affiliate tracking, email campaign automation
- Visual rule builder (conditions/actions edited via JSON parameters in admin form v1)

---

**STOP — Phase 26 complete. Do not proceed to Phase 27 without explicit approval.**
