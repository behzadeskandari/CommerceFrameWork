# Smartstore → Commerce Import Mapping

**Phase 46** — explicit mapping for SQL export migration tooling.

Schema is **discovered at runtime** from the supplied file (`CREATE TABLE` + `INSERT`). This document describes implemented mappings as of Phase 46. When `data/smartstore/scriptWithData.sql` is added, run schema inspection first:

```powershell
./scripts/migration/run-smartstore-import.ps1 -SqlFile data/smartstore/scriptWithData.sql -InspectOnly
```

---

## 1. Principles

| Principle | Implementation |
|-----------|----------------|
| Repeatable | Same SQL file hash tracked; duplicate runs blocked unless `AllowDuplicateRun=true` |
| Transaction-safe | Per-importer DB transaction on relational providers |
| Idempotent | `ImportIdMapping` stores `(EntityType, SourceId) → TargetId`; re-run skips mapped rows |
| Error reporting | `ImportIssue` rows + `SmartstoreImportResult.Issues` |
| Relationship validation | Missing FKs emit warnings/errors; rows not silently dropped |
| Legacy ID mapping | `ImportIdMapping` table per import run |

---

## 2. Entity mapping overview

| Smartstore table | Commerce entity | Status |
|------------------|-----------------|--------|
| `Language` | `Language` | ✅ Full |
| `Currency` | `StoreCurrency` | ✅ Full |
| `Store` | `Store` | ✅ Full |
| `Setting` | `Setting` | ✅ Full (global when store ref missing) |
| `Customer` | `Customer` + `CommerceIdentityUser` | ✅ Full |
| `Category` | `Category` | ✅ Full |
| `Manufacturer` | — | ⚠️ Warn-only (no Commerce entity) |
| `Product` | `Product` | ✅ Full |
| `Product` (Price) | `ProductOffer` | ✅ Per default store/currency |
| `Product_Category_Mapping` | `ProductCategory` | ✅ Full |
| `ProductVariantAttributeCombination` | — | ⚠️ Warn-only (partial; attributes not imported) |
| `ProductAttribute` / `ProductAttributeOption` | — | ❌ Not in Phase 46 |
| `MediaFile` | `MediaAsset` | ✅ Metadata; binary not copied |
| `Product_MediaFile_Mapping` | `ProductMedia` | ✅ When refs resolve |
| `Discount` | `Discount` | ✅ Percentage/fixed mapping |
| `ProductReview` | `ProductReview` | ✅ Full |
| `Order` | `Order` | ✅ Historical import |
| `OrderItem` | `OrderItem` | ✅ Full |
| `Topic` | `Topic` | ✅ Full |
| `UrlRecord` | `UrlRecord` | ✅ Product/Category/Topic |
| `LocalizedProperty` | `EntityTranslation` | ✅ Product/Category/Topic |
| `LocaleStringResource` | — | ⚠️ Logged as warning (framework resources differ) |
| `Download` | — | ❌ Not in Phase 46 |
| `CustomerRole` / `Address` | — | ❌ Not in Phase 46 |
| `MenuRecord` / `MenuItemRecord` | — | ❌ Not in Phase 46 |
| `StoreMapping` | — | ❌ Not in Phase 46 |

Importers run **only when source tables exist** in the parsed SQL.

---

## 3. Field-level mapping

### 3.1 Language (`Language` → `Language`)

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `Id` | — | Mapped via `ImportIdMapping` |
| `Name` | `Name` | Required |
| `UniqueSeoCode` | `LanguageCode` | Fallback from culture |
| `LanguageCulture` | `Culture` | |
| `Rtl` | `IsRtl` | |
| `DisplayOrder` | `DisplayOrder` | |
| `Published` | `IsPublished` | |

### 3.2 Currency (`Currency` → `StoreCurrency`)

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `CurrencyCode` | `Code` | Required |
| `Name` | `Name` | |
| `Rate` | `Rate` | Invalid rate → warning, default `1` |
| `DisplayOrder` | `DisplayOrder` | |
| `Published` | `IsPublished` | |

### 3.3 Store (`Store` → `Store`)

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `Name` | `Name`, `SystemName` | System name slugified |
| `Url` | `Url` | |
| `DisplayOrder` | `DisplayOrder` | |
| `PrimaryStoreCurrencyId` / `DefaultCurrencyId` | `DefaultCurrencyId` | Via ID mapping |
| — | `DefaultLanguageId` | From mapped language or first in DB |

**Incompatible:** Smartstore multi-store mapping tables not imported in Phase 46.

### 3.4 Setting (`Setting` → `Setting`)

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `Name` | `Name` | |
| `Value` | `Value` | |
| `StoreId` | `StoreId` | `0` = global; missing store → warning, imported global |

### 3.5 Customer (`Customer` → `Customer`)

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `Email` | `Email`, identity user | |
| `FirstName` / `LastName` | `FirstName` / `LastName` | |
| `Active` / `Deleted` | `IsActive` / soft delete | |
| `IsSystemAccount` | Skipped with warning | |

**Incompatible:** Password hashes not migrated (identity recreated; login requires reset). `Username`, addresses, roles not in Phase 46.

### 3.6 Category (`Category` → `Category`)

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `Name` | `Name`, slug | |
| `Description` | `Description` | |
| `ParentCategoryId` | `ParentCategoryId` | Missing parent → warning, root |
| `Published` | `IsPublished` | |
| `DisplayOrder` | `DisplayOrder` | |

### 3.7 Manufacturer (`Manufacturer` → —)

No Commerce `Manufacturer` entity. Each row produces warning `unsupported_entity` with source ID and name.

### 3.8 Product (`Product` → `Product` + `ProductOffer`)

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `Name` | `Name`, slug | |
| `Sku` / `Gtin` | `Sku` | |
| `ShortDescription` | `ShortDescription` | |
| `FullDescription` / `Description` | `Description` | |
| `Published` / `Deleted` | `IsPublished` / soft delete | |
| `DisplayOrder` | `DisplayOrder` | |
| `ProductTypeId` | `ProductType` | Mapped: 5=Grouped, 10=Bundle, 20=Digital, 25=Downloadable, 30=Variant, else Simple |
| `Weight` | `WeightGrams` | kg → grams (`× 1000`) |
| `Price` | `ProductOffer` | Default store + first currency |

**Incompatible:** Smartstore-specific templates, ACL, tier prices, attribute combinations (except warn-only pass).

### 3.9 Media (`MediaFile` → `MediaAsset`)

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `Name` | `OriginalFileName` | |
| `MimeType` | `ContentType` | |
| `Extension` | `Extension` | |
| `Size` | `Size` | |
| `Path` | `StorageKey` | Normalized (`smartstore-import/...`); leading `/` stripped |
| `Width` | `Width` | |

**Incompatible:** Binary files are **not** copied from Smartstore storage. Missing path → placeholder key + warning `missing_media`. Physical file migration is a separate ops step.

### 3.10 Discount (`Discount` → `Discount`)

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `Name` | `Name`, system name | |
| `DiscountTypeId` | Type mapping | Percentage vs fixed amount |
| `DiscountPercentage` / `DiscountAmount` | Value fields | |
| `IsActive` | `IsActive` | |

**Incompatible:** Smartstore rule sets, requirements, and category/product mappings not fully imported.

### 3.11 Product review (`ProductReview` → `ProductReview`)

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `ProductId` / `CustomerId` | FKs | Via mapping; missing → warning |
| `Rating` | Rating | Clamped 1–5 with warning if out of range |
| `Title` / `ReviewText` | Content fields | |
| `IsApproved` | Moderation status | |
| `IsVerifiedPurchase` | Verified flag | |

### 3.12 Order (`Order` + `OrderItem` → `Order` + `OrderItem`)

Historical import — not live checkout replay.

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `OrderNumber` | Order number | |
| `StoreId` / `CustomerId` | FKs | Missing customer → guest-linked warning |
| `CustomerCurrencyCode` | Currency | Fallback to store default |
| `OrderSubtotalInclTax` … `OrderTotal` | Totals | |
| `OrderStatusId` / `PaymentStatusId` / `ShippingStatusId` | Status enums | Best-effort mapping + warnings |

Synthetic IDs used for legacy checkout/cart references: bases `900M` / `800M` / `700M` + source IDs.

**Incompatible:** Shipments, payments, addresses, gift cards not reconstructed.

### 3.13 Topic (`Topic` → `Topic`)

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `SystemName` | `SystemName` | |
| `Title` / `Body` | Content | |
| `IsPublished` | Published flag | Requires target store |

### 3.14 SEO URL (`UrlRecord` → `UrlRecord`)

| Smartstore column | Commerce field | Notes |
|-------------------|----------------|-------|
| `EntityName` + `EntityId` | Entity reference | Product, Category, Topic |
| `Slug` | `Slug` | |
| `LanguageId` / `StoreId` | Optional FKs | |
| `IsActive` | `IsActive` | |

Unmapped entity → warning `entity_ref_missing` (row not imported).

### 3.15 Localization

| Source | Target | Notes |
|--------|--------|-------|
| `LocalizedProperty` (Product/Category/Topic) | `EntityTranslation` | Key/value per language |
| `LocaleStringResource` | — | Warning `unsupported_resource` — Commerce uses its own resource files |

---

## 4. Import audit tables

| Table | Purpose |
|-------|---------|
| `ImportRun` | Run metadata, file path/hash, status, counts |
| `ImportIdMapping` | Legacy `(EntityType, SourceId) → TargetId` |
| `ImportIssue` | Warning/error log per run |

---

## 5. Issue codes (representative)

| Code | Severity | Meaning |
|------|----------|---------|
| `unsupported_entity` | Warning | Source entity has no Commerce target (e.g. Manufacturer) |
| `missing_id` | Error | Required source ID column empty |
| `create_failed` | Error | Domain/DB exception during insert |
| `language_missing` / `currency_missing` | Error | Store prerequisites missing |
| `store_ref_missing` | Warning | FK remapped or defaulted |
| `customer_ref_missing` | Warning | Order/review customer not found |
| `product_ref_missing` | Warning | Order item / media mapping / variant |
| `category_ref_missing` | Warning | Product-category mapping skipped |
| `missing_media` / `media_ref_missing` | Warning | Media path or mapping issue |
| `invalid_rate` / `invalid_rating` | Warning | Value normalized |
| `entity_ref_missing` | Warning | UrlRecord or localization target missing |
| `variant_partial` | Warning | Variant row noted; full attribute import deferred |
| `importer_failed` | Error | Unhandled importer exception |

---

## 6. Usage

```powershell
# Verify tooling (runs unit tests with fixtures)
./scripts/migration/run-smartstore-import.ps1 -SqlFile data/smartstore/scriptWithData.sql

# Programmatic (host module registered)
# ISmartstoreImportService.InspectSchemaAsync(path)
# ISmartstoreImportService.ImportAsync(new SmartstoreImportOptions(path, AllowDuplicateRun: false))
```

Place production SQL at `data/smartstore/scriptWithData.sql`. See [`data/smartstore/README.md`](../../data/smartstore/README.md).

---

## 7. Validation checklist (production import)

1. Run `InspectSchemaAsync` — confirm expected tables/row counts
2. Run import on staging with `AllowDuplicateRun=false`
3. Compare entity summaries vs source row counts
4. Review all `ImportIssue` warnings/errors
5. Re-run with `AllowDuplicateRun=true` — expect high `SkippedCount`, zero duplicate PK errors
6. Manually copy media binaries to match normalized `StorageKey` paths if needed
