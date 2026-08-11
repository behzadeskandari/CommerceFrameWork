# PHASE 7 REPORT — Store, Localization, Currency & Settings

## PHASE 7 COMPLETE

Store: PASS  
Multi-Store: PASS  
Store Resolution: PASS  
Store Isolation: PASS  

Languages: PASS  
Persian: PASS  
English: PASS  
RTL: PASS  
LTR: PASS  

Currencies: PASS  
IRR: PASS  
USD: PASS  
EUR: PASS  

Settings: PASS  
Global Settings: PASS  
Store Settings: PASS  

Installation Regression: PASS  
Catalog Regression: PASS  
Customer Regression: PASS  
Authentication: PASS  
Authorization: PASS  

Admin Store UI: PASS  
Admin Language UI: PASS  
Admin Currency UI: PASS  
Admin Settings UI: PASS  

Storefront Localization: PASS  
Storefront Currency: PASS  

Backend Tests: PASS (65 unit + 17 architecture + 12 integration = 94)  
Architecture Tests: PASS  
Integration Tests: PASS  
Frontend Tests: PASS  
Frontend Build: PASS  

Cart: NOT IMPLEMENTED  
Checkout: NOT IMPLEMENTED  
Orders: NOT IMPLEMENTED  
Payments: NOT IMPLEMENTED  
Shipping: NOT IMPLEMENTED  
Tax: NOT IMPLEMENTED  
Inventory: NOT IMPLEMENTED  
Discounts: NOT IMPLEMENTED  
CMS: NOT IMPLEMENTED  
Media: NOT IMPLEMENTED  
Downloads: NOT IMPLEMENTED  
Search: NOT IMPLEMENTED  
Plugins: NOT IMPLEMENTED  
Smartstore Import: NOT STARTED  

Next Phase: PHASE 8

---

## Store Architecture

The `Commerce.Store` module was added under `src/Commerce/Modules/Store/` with the standard layered structure:

- `Commerce.Store.Domain` — aggregates and entities
- `Commerce.Store.Contracts` — DTOs and reader contracts
- `Commerce.Store.Application` — application services
- `Commerce.Store.Infrastructure` — EF Core, resolution, settings, seeding
- `Commerce.Modules.Store` — module facade registered in `Program.cs`

The existing `IStoreContext` in `Commerce.Framework.Contracts/Tenancy/` remains the **single authoritative** store context. It was extended with language and currency properties; no competing abstraction was introduced.

## Store Resolution

`IStoreResolver` resolves stores at request time from:

1. **Host/domain** — non-localhost requests match `StoreDomain.Host`
2. **Default active store** — fallback when no domain match (includes all `localhost` / `127.0.0.1` dev hosts)
3. **Development fallback** — seeded primary store with `localhost:5100` domains

Resolution flow:

```text
HTTP Request (Host header)
        ↓
StoreResolver
        ↓
IStoreContextAccessor
        ↓
IStoreContext (StoreId, Store, Language, Currency)
```

`StoreContextMiddleware` runs after installation completes and skips `/installation` paths and pre-installation requests.

## Store Domains

`StoreDomain` supports multi-domain stores with `Host`, `Port`, `Scheme`, `IsPrimary`, and `IsSslRequired`.

## Language Architecture

`Language` entity includes `LanguageCode`, `CultureCode`, `NativeName`, `IsRtl`, `IsActive`, and `DisplayOrder`.

Development seed data includes **English** (`en` / `en-US`) and **Persian** (`fa` / `fa-IR`, RTL).

### Language Resolution (`ILanguageResolver`)

Priority:

1. Cookie `commerce.language`
2. `Accept-Language` header
3. Store default language

API endpoint `POST /api/languages/select/{languageCode}` sets the cookie for storefront language switching.

## Localization Architecture

`EntityTranslation` provides a generalized model:

```text
EntityType + EntityId + LanguageId + Property + Value
```

This decouples localization from Catalog entities and supports future modules without per-language tables.

## Currency Architecture

`StoreCurrency` stores ISO 4217 codes with `Rate`, `Symbol`, `DecimalPlaces`, and `DisplayName`.

Development seed: **IRR**, **USD**, **EUR**.

### Money Handling

`Money` remains the canonical monetary value object. `ICurrencyConverter` and `ICurrencyExchangeRateProvider` perform explicit conversions with source/target/rate — never silent currency reassignment.

`FixedExchangeRateProvider` reads rates from the database for development/testing.

## Settings Architecture

Key/value settings stored in the shared `Setting` table with `DataType` support.

Resolution order: **Store value → Global value → Default**

Registered settings (Phase 7):

- `Store.DefaultLanguage`
- `Catalog.ProductsPerPage`

## Permissions

Registered via `StorePermissionContributor` and enforced with existing `[RequirePermission]` on admin APIs.

## Admin API

| Resource | Endpoints |
|---|---|
| Stores | `GET/POST/PUT/DELETE /api/stores`, `POST /api/stores/{id}/domains` |
| Languages | `GET/POST/PUT /api/languages`, `POST /api/languages/select/{code}` |
| Currencies | `GET/POST/PUT /api/currencies` |
| Settings | `GET/PUT /api/settings` |
| Store context | `GET /api/store/context` (anonymous, resolved store) |

## Angular Implementation

Extended existing libraries:

- `@commerce/api` — `StoreApi`, store/language/currency models
- `@commerce/localization` — `StoreContextService`, `CurrencyFormatPipe`
- Admin routes: `/admin/stores`, `/admin/languages`, `/admin/currencies`, `/admin/settings`
- Storefront language switcher syncs with backend cookie via `StoreContextService`

### RTL/LTR

`LocalizationService` sets `document.documentElement.dir` and `lang`. Persian activates RTL; English activates LTR.

### Currency Display

`StoreContextService.formatAmount()` uses backend currency configuration (symbol, decimal places, culture).

## Installation Integration

`IStoreInstallationProvisioningService` delegates store/language/currency creation to the Store module. Provisioning is **idempotent** when seed data already exists.

## Multi-Store Isolation

Integration test `MultiStoreIsolationTests` verifies host-based resolution for `store-a.test` and `store-b.test` with no cross-store leakage.

## Tests

| Suite | Count | Status |
|---|---|---|
| Unit | 65 | PASS |
| Architecture | 17 | PASS |
| Integration | 12 | PASS |
| Angular | 4+ | PASS |

## Development URLs (unchanged)

| Service | URL |
|---|---|
| Commerce.Host | https://localhost:5100 |
| Admin | http://localhost:4200/admin |
| Storefront | http://localhost:4201 |
