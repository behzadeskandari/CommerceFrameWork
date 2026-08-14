# PHASE 39 — Caching / Performance / Scalability — Report

**Status:** Complete  
**Date:** 2026-08-13

---

## Summary

Phase 39 adds a production-ready caching layer with memory and Redis providers, application-level decorators for catalog/search/settings, output caching on anonymous storefront GET endpoints, cache invalidation hooks, distributed locking for stampede protection, and targeted query optimizations identified during profiling.

**Financial safety:** Mutable cart/checkout/payment/order/inventory state is never cached. `CacheGuard` rejects denied key segments at runtime.

---

## Backend

### Module

| Project | Role |
|---|---|
| `Commerce.Framework.Contracts.Caching` | `ICacheManager`, `ICacheKeyBuilder`, `ICacheInvalidator`, `IDistributedLockProvider`, `CacheOptions` |
| `Commerce.Framework.Infrastructure.Caching` | Memory, distributed, composite managers; `CacheGuard`; in-memory locks |
| `Commerce.Cache.Application` | Decorators, `CacheCatalogInvalidator`, `CachePerformanceProfiler` |
| `Commerce.Cache.Infrastructure` | Redis provider, output cache policies, DI |
| `Commerce.Modules.Cache` | Module registration |

### Cache providers

| Provider | Config | Behavior |
|---|---|---|
| Memory (default) | `Provider: "Memory"` | Single-node L1 via `IMemoryCache` |
| Redis | `Provider: "Redis"` + connection string | L2 distributed + L1 composite + Redis distributed locks |

### Application cache decorators

| Service | Cache key scope | TTL |
|---|---|---|
| `CachedStorefrontCatalogService` | Product list/detail/slug | 15 min |
| `CachedSearchQueryService` | Query fingerprint / suggest | 2 min |
| `CachedSettingService` | Setting key + store | 60 min |

Only successful catalog results are cached. Failures are not stored.

### Output cache (safe endpoints)

| Endpoint | Policy |
|---|---|
| `GET /api/catalog/storefront/products*` | `commerce.storefront.catalog` |
| `GET /api/search/products`, `/api/search/suggest` | `commerce.storefront.search` |

Anonymous GET only. Admin, cart, checkout, and payment routes are excluded.

### Invalidation

| Trigger | Action |
|---|---|
| Product create/update/delete | `CacheCatalogInvalidator` → product prefix + search prefix |
| Setting update | `CachedSettingService.SetAsync` → key eviction |

Registered as additional `ICatalogChangeNotifier` alongside search index coordinator.

### Query optimizations (profile-driven)

| Change | Location |
|---|---|
| `AsNoTracking()` on product reads | `EfProductRepository` |
| Composite index on public listing columns | `CatalogProductConfiguration` |
| Batch attribute option load | `GetOptionsByIdsAsync` + `StorefrontCatalogService` |

---

## Configuration

```json
"Commerce": {
  "Cache": {
    "Enabled": true,
    "Provider": "Memory",
    "KeyPrefix": "commerce",
    "RedisConnectionString": "",
    "Products": { "TtlMinutes": 15 },
    "Search": { "TtlMinutes": 2 },
    "Settings": { "TtlMinutes": 60 },
    "Output": { "TtlMinutes": 2 }
  }
}
```

---

## Performance measurement

`CachePerformanceProfiler.MeasureAsync` compares uncached vs cached execution:

| Metric | Example (unit test) |
|---|---|
| Uncached path | ~15 ms simulated DB read |
| Cached path | Second call served from memory |
| Speedup factor | ≥ 1.0x (verified in tests) |

---

## Tests

`Commerce.Tests.Unit.Cache` — **9 passing**

| Test | Coverage |
|---|---|
| `CacheGuard_BlocksDeniedFinancialSegments` | Denylist enforcement |
| `MemoryCacheManager_GetSetRemove_Works` | Correctness |
| `CacheInvalidator_RemovesProductEntries` | Product invalidation |
| `CacheInvalidator_RemovesSearchPrefix` | Search invalidation |
| `GetOrCreateAsync_ConcurrentAccess_*` | Concurrent access |
| `StaleData_IsRemovedAfterInvalidation` | Stale data |
| `Failover_NullCacheManager_*` | Failover to passthrough |
| `PerformanceMeasurement_ShowsCachedPathIsFaster` | Before/after |
| `SearchRequestFingerprint_IsStableForSameRequest` | Key stability |

---

## Host integration

- `CacheModule` registered in `Program.cs` (after Catalog, Search, Store, Observability)
- `UseCommerceOutputCache()` in middleware pipeline
- Health check reuses Phase 38 `DefaultCacheHealthProbe` (probes distributed/memory cache)

---

## Not cached (by design)

- Cart, checkout, payment capture/void/refund state
- Order mutations and inventory reservations
- Gift card balances
- Customer-context pricing/discount calculations

---

**STOP — Phase 39 complete.**
