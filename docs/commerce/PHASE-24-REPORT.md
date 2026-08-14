# PHASE 24 — Search Engine — Report

**Status:** Complete  
**Date:** 2026-08-12

---

## 1. Summary

Phase 24 delivers a provider-independent search engine with pluggable backends, database-backed default provider, denormalized index storage, job queue for indexing (Phase 28 ready), storefront search/suggest APIs, and catalog change hooks for incremental indexing.

---

## 2. Architecture

| Layer | Responsibility |
|---|---|
| `Commerce.Framework.Search` | `ISearchProvider`, `ISearchIndexer`, query models, resolver |
| `Commerce.Search.*` module | Index entities, document builder, job queue, admin/storefront services |
| `Commerce.Plugin.Search.Database` | Default SQL index provider |

Core Commerce never references Elasticsearch/OpenSearch directly.

---

## 3. Search Features

- **Fields:** name, SKU, description, categories, attributes, store, language
- **Filters:** category, manufacturer (reserved), price range, product type, availability, attributes
- **Sort:** relevance, price, newest, popularity, rating
- **Pagination:** page + pageSize (max 100)
- **Suggestions:** prefix match on index (min 2 chars, debounced on storefront)

---

## 4. Indexing

| Trigger | Behavior |
|---|---|
| Product create/update/delete | `ICatalogChangeNotifier` → job queue → immediate process |
| Admin rebuild | Full reindex via `/api/admin/search/rebuild` |
| Job table | `SearchIndexJob` — ready for Phase 28 background worker |

Index entries are **per product per store** with store default language.

---

## 5. API

| Route | Purpose |
|---|---|
| `GET /api/search/products` | Storefront search |
| `GET /api/search/suggest?q=` | Suggestions |
| `GET /api/admin/search/status` | Index stats |
| `POST /api/admin/search/rebuild` | Full rebuild |
| `POST /api/admin/search/process-jobs` | Process pending jobs |

Permissions: `Search.View`, `Search.Manage`

---

## 6. Storefront

- `/products` page uses `SearchApi` with search form, sort, debounced suggestions
- Falls back to empty results when index not built (admin rebuild required initially)

---

## 7. Security

- Store-scoped queries via `IStoreContext`
- Admin endpoints require permissions
- Parameterized EF queries (no raw SQL injection)

---

## 8. Tests

### Unit (`Commerce.Tests.Unit/Search/`)
- Suggestion minimum length
- Pagination clamping
- Default provider name

---

## 9. Development SDK

.NET 10 SDK required (`net10.0`). Install via:

```powershell
Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
./dotnet-install.ps1 -Channel 10.0
```

Or download from https://dotnet.microsoft.com/download

---

## 10. Known Limitations

1. Manufacturer and tags not indexed (Catalog entities not yet present)
2. Catalog localization not indexed (single-locale product fields)
3. Suggestions use index prefix match, not dedicated suggest index
4. Incremental jobs processed synchronously (Phase 28 async worker deferred)

---

## 11. Key Files

```
docs/commerce/PHASE-24-PREIMPLEMENTATION.md
src/Commerce/Framework/Search/
src/Commerce/Modules/Search/
src/Commerce/Plugins/Search/Commerce.Plugin.Search.Database/
src/Commerce/Host/Commerce.Host/Search/SearchControllers.cs
src/Commerce/Modules/Catalog/Commerce.Catalog.Contracts/Products/ICatalogChangeNotifier.cs
frontend/commerce-ui/libs/api/src/lib/search-api.service.ts
frontend/commerce-ui/apps/storefront/src/app/pages/products.page.ts
tests/Commerce/Commerce.Tests.Unit/Search/SearchTests.cs
```

---

**Phase 24 complete. Stopped — awaiting explicit approval before Phase 25.**
