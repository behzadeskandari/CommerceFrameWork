# PHASE 39 — Caching / Performance / Scalability — Pre-Implementation

**Status:** Complete  
**Date:** 2026-08-13

---

## Profiling baseline (before optimization)

Hot paths profiled via code inspection and unit benchmarks:

| Path | Issue observed | Action |
|---|---|---|
| `StorefrontCatalogService.MapStorefrontDetailAsync` | N+1 attribute option lookups per variant | Batch `GetOptionsByIdsAsync` |
| `EfProductRepository` read methods | Tracking overhead on read-only queries | `AsNoTracking()` |
| `CatalogProduct` public listing filter | Separate single-column indexes | Composite index `(Published, IsVisible, IsAvailable, Deleted)` |
| `SettingService.GetRawAsync` | 1–2 DB round-trips per call | Application cache (60 min TTL) |
| `SearchQueryService` | Repeated identical queries | Search cache (2 min TTL) |
| Storefront catalog/search GET endpoints | Full pipeline per request | Output cache (anonymous GET only) |

## Explicit denylist (never cached)

`CacheGuard` blocks keys containing: `cart`, `checkout`, `payment`, `order`, `inventory`, `giftcard`, `wallet`, `reservation`.

Not decorated: `CartService`, `CheckoutService`, `PaymentService`, `OrderService`, customer-specific pricing.

## Architecture

```
Commerce.Framework.Contracts.Caching     ICacheManager, ICacheKeyBuilder, ICacheInvalidator, IDistributedLockProvider
Commerce.Framework.Infrastructure.Caching Memory, Distributed, Composite managers; CacheGuard; locks
Commerce.Cache.Application               Decorators + CacheCatalogInvalidator + profiler
Commerce.Cache.Infrastructure            Redis wiring, output cache policies, DI
Commerce.Modules.Cache                   Module registration
```

## TTL policy

| Category | TTL | Invalidation |
|---|---|---|
| Products | 15 min | `ICatalogChangeNotifier` on create/update/delete |
| Search | 2 min | Catalog change + prefix eviction |
| Settings | 60 min | `SetAsync` on same key/store |
| Output cache | 2 min | ASP.NET output cache tags |

## Redis configuration

```json
"Commerce": {
  "Cache": {
    "Enabled": true,
    "Provider": "Redis",
    "RedisConnectionString": "localhost:6379",
    "KeyPrefix": "commerce"
  }
}
```

Default development profile uses in-memory provider (`Provider: "Memory"`).

## Distributed locking

- **Redis:** `SET NX EX` with Lua release script (stampede protection in `CompositeCacheManager.GetOrCreateAsync`)
- **Memory:** `InMemoryDistributedLockProvider` (single-node)

## Tests planned

- Cache correctness, invalidation, concurrent access, stale data, failover
- Before/after performance measurement via `CachePerformanceProfiler`
