# PHASE 24 — Search Engine — Preimplementation

**Status:** Preimplementation  
**Date:** 2026-08-12

---

## 1. Inspection Summary

| Area | Current state |
|---|---|
| Catalog search | `IProductRepository.SearchAsync` — SQL `Contains` on Name/Sku/Slug only |
| Storefront | `?term=` on `/api/catalog/storefront/products` |
| Manufacturer | Not implemented in Catalog |
| Tags | Not implemented |
| Localization | Catalog entities are single-locale; index uses store `DefaultLanguageId` |
| Domain events | Raised on Product CRUD but **no dispatcher** |
| Plugin pattern | `IPaymentProvider`, `IShippingProvider`, `ITaxProvider` via DI + resolver |
| Search module | **Does not exist** |

---

## 2. Architecture

```
Commerce.Framework.Search/     ← ISearchProvider, ISearchIndexer, models
Commerce.Search.* module/      ← index storage, query facade, job queue
Commerce.Plugin.Search.Database/ ← default database provider
```

Core Commerce depends only on **Framework.Search** abstractions. Providers are plugins.

---

## 3. Contracts

| Interface | Responsibility |
|---|---|
| `ISearchProvider` | Execute search + suggestions against a backend |
| `ISearchIndexer` | Upsert/delete documents, rebuild |
| `ISearchQueryService` | Storefront/admin facade (provider resolution) |
| `ISearchIndexCoordinator` | Queue indexing jobs (Phase 28 background worker ready) |

---

## 4. Index Document Fields

Per product **per store** (store-scoped via offers):

- Name, SKU, Description, Slug
- Category IDs/names
- Manufacturer (nullable — reserved)
- Attributes (code → value JSON for facets)
- Tags (empty until Catalog supports tags)
- ProductType, Price, Availability flags
- LanguageId (store default)
- PopularityScore, Rating (defaults until data exists)
- SearchText (denormalized for DB provider)

---

## 5. Query Features

**Search:** term across indexed fields  
**Filters:** category, manufacturer, price range, attributes, availability, product type  
**Sort:** relevance, price asc/desc, newest, popularity, rating  
**Pagination:** page + pageSize  
**Suggestions:** prefix match on indexed terms (min 2 chars), capped results — no per-keystroke full table scan from storefront

---

## 6. Indexing

| Trigger | Action |
|---|---|
| Product created | Queue `ProductUpsert` job |
| Product updated | Queue `ProductUpsert` job |
| Product deleted | Queue `ProductDelete` job |
| Admin rebuild | Queue `FullRebuild` job |
| Job processor | Processes pending jobs (sync MVP; Phase 28 async worker) |

**Catalog integration:** `ICatalogChangeNotifier` in Catalog.Contracts — Search implements notifier without Catalog → Search dependency.

---

## 7. Security

- Storefront search: anonymous, store-scoped via `IStoreContext`
- Admin reindex: `Search.Manage` permission
- No arbitrary query injection — parameterized EF queries

---

## 8. API Plan

| Route | Purpose |
|---|---|
| `GET /api/search/products` | Storefront search |
| `GET /api/search/suggest?q=` | Suggestions (min length enforced) |
| `GET /api/admin/search/status` | Index stats |
| `POST /api/admin/search/rebuild` | Full reindex |

Permissions: `Search.View`, `Search.Manage`

---

## 9. Frontend

- `SearchApi` service
- Update storefront `/products` page with search, filters, sort
- Route already uses `themeLayout: 'Search'`

---

**Ready for implementation.**
