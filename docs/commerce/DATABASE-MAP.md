# Commerce Framework — Database Map (PHASE 0)

**Reference source:** Smartstore 6.4 SQL Server export (`scriptWithData.sql`)  
**Analysis status:** Bounded-context classification complete; full schema validation pending SQL file placement in repository

---

## 1. Overview

The Smartstore 6.4 database export serves as a **compatibility reference model**, not a schema to blindly replicate. Our commerce framework will:

1. Classify all ~118 tables into bounded contexts
2. Design domain entities representing business concepts (not 1:1 table mapping)
3. Create EF Core persistence entities with explicit configurations
4. Support Smartstore data import via explicit mapping layer (Phase 17)

### Record Counts (from SQL export metadata)

| Entity group | Approximate records |
|---|---|
| Tables | ~118 |
| INSERT statements | ~18,655 |
| LocaleStringResource | ~15,272 |
| Setting | ~703 |
| Log | ~674 |
| ScheduleTaskHistory | ~314 |
| Permission | ~286 |
| Country | ~239 |
| LocalizedProperty | ~114 |
| UrlRecord | ~97 |
| MediaFile | ~36 |
| Topic | ~20 |
| Product | ~15 |
| Category | ~11 |
| Order | ~8 |
| OrderItem | ~8 |
| Customer | ~7 |
| Store | ~3 |
| Language | ~4 |
| Currency | ~15 |

---

## 2. Bounded Context Classification

### 2.1 Core Platform

| Table | Purpose | Domain entity | Import priority |
|---|---|---|---|
| `MigrationVersionInfo` | Migration history registry | `MigrationRecord` | Phase 2 |
| `Setting` | Key-value settings (global/store/plugin) | `Setting` | Phase 4 |
| `GenericAttribute` | EAV attributes for any entity | `GenericAttribute` | Phase 6 |
| `Log` | Application log entries | `LogEntry` | Phase 1 (infra) |
| `ActivityLog` | User activity audit trail | `ActivityLog` | Phase 6 |
| `ActivityLogType` | Activity type definitions | `ActivityLogType` | Phase 6 |
| `ScheduleTask` | Scheduled job definitions | `ScheduleTask` | Phase 2 |
| `ScheduleTaskHistory` | Job execution history | `ScheduleTaskHistory` | Phase 2 |

**Design notes:**
- `Setting` uses `Name` (string key) + `Value` (string) + optional `StoreId` — map to strongly typed settings in application layer
- `GenericAttribute` is polymorphic (`KeyGroup`, `Key`, `Value`, `EntityId`) — use for customer/product attributes without schema changes
- Do not expose `MigrationVersionInfo` as a domain entity; it is infrastructure-only

### 2.2 Identity / Customer

| Table | Purpose | Domain entity |
|---|---|---|
| `Customer` | Customer accounts (registered + system) | `Customer` |
| `CustomerRole` | Role definitions | `CustomerRole` |
| `CustomerRoleMapping` | Customer ↔ role M:N | (junction, no entity) |
| `CustomerAddresses` | Customer ↔ address M:N | (junction) |
| `Address` | Physical addresses | `Address` |
| `ExternalAuthenticationRecord` | OAuth/external login | `ExternalAuthRecord` |
| `NewsLetterSubscription` | Email subscriptions | `NewsletterSubscription` |
| `Affiliate` | Affiliate program | `Affiliate` |

**Design notes:**
- Smartstore `Customer` includes `Username`, `Email`, `Password`, `PasswordSalt`, `Active`, `Deleted`, `IsSystemAccount`, `SystemName`, `LastIpAddress`, `CreatedOnUtc`, `LastLoginDateUtc`, `LastActivityDateUtc`, `BillingAddress_Id`, `ShippingAddress_Id`, `CustomerGuid`
- Password fields map to ASP.NET Core Identity in our implementation — never store plain text
- System customers (`IsSystemAccount=true`) used for background operations — preserve during import
- Guest customers identified by `CustomerGuid` cookie

### 2.3 Catalog

| Table | Purpose | Domain entity |
|---|---|---|
| `Product` | Core product entity | `Product` |
| `Category` | Hierarchical categories | `Category` |
| `Manufacturer` | Brand/manufacturer | `Manufacturer` |
| `ProductTemplate` | Product display template | `ProductTemplate` |
| `CategoryTemplate` | Category display template | `CategoryTemplate` |
| `ManufacturerTemplate` | Manufacturer display template | `ManufacturerTemplate` |
| `Product_Category_Mapping` | Product ↔ category M:N | (junction) |
| `Product_Manufacturer_Mapping` | Product ↔ manufacturer M:N | (junction) |
| `Product_MediaFile_Mapping` | Product ↔ media M:N | (junction) |
| `Product_ProductAttribute_Mapping` | Product ↔ attribute M:N | (junction) |
| `Product_ProductTag_Mapping` | Product ↔ tag M:N | (junction) |
| `Product_SpecificationAttribute_Mapping` | Product ↔ spec M:N | (junction) |
| `ProductAttribute` | Attribute definitions (color, size) | `ProductAttribute` |
| `ProductAttributeOption` | Attribute option values | `ProductAttributeOption` |
| `ProductAttributeOptionsSet` | Grouped attribute options | `ProductAttributeOptionsSet` |
| `ProductVariantAttributeCombination` | SKU-level variant combinations | `ProductVariantCombination` |
| `ProductVariantAttributeValue` | Variant attribute values | `ProductVariantAttributeValue` |
| `ProductBundleItem` | Bundle composition | `ProductBundleItem` |
| `ProductBundleItemAttributeFilter` | Bundle attribute filters | `ProductBundleItemAttributeFilter` |
| `ProductTag` | Product tags | `ProductTag` |
| `CrossSellProduct` | Cross-sell relationships | (junction) |
| `RelatedProduct` | Related product relationships | (junction) |
| `TierPrice` | Quantity-based pricing | `TierPrice` |
| `SpecificationAttribute` | Spec attribute definitions | `SpecificationAttribute` |
| `SpecificationAttributeOption` | Spec option values | `SpecificationAttributeOption` |
| `PriceLabel` | Price display labels | `PriceLabel` |

**Design notes:**
- `Product.ProductTypeId` drives behavior: Simple, Grouped, Bundled, etc.
- `Product.ManageInventoryMethodId` + `StockQuantity` for inventory-ready design
- `Category` uses tree path pattern (`TreePath`, `ParentCategoryId`, `DisplayOrder`) for efficient hierarchy queries
- Do not create one giant `Product` aggregate — decompose into `ProductPricingService`, `ProductAttributeService`, `ProductInventoryService`, etc.
- Junction tables remain as EF configurations, not domain entities

### 2.4 Cart

| Table | Purpose | Domain entity |
|---|---|---|
| `ShoppingCartItem` | Cart line items | `ShoppingCartItem` |
| `CheckoutAttribute` | Checkout form fields | `CheckoutAttribute` |
| `CheckoutAttributeValue` | Checkout attribute options | `CheckoutAttributeValue` |

**Design notes:**
- Cart items reference `CustomerId`, `ProductId`, `Quantity`, `AttributesXml`, `CustomerEnteredPrice`
- Guest carts use customer GUID; merge on login
- Cart calculation is deterministic: price → discount → tax → shipping

### 2.5 Orders

| Table | Purpose | Domain entity |
|---|---|---|
| `Order` | Order header | `Order` |
| `OrderItem` | Order line items | `OrderItem` |
| `OrderNote` | Internal/customer notes | `OrderNote` |
| `ReturnCase` | Return/refund requests | `ReturnCase` |
| `Shipment` | Shipment records | `Shipment` |
| `ShipmentItem` | Shipment line items | `ShipmentItem` |
| `RecurringPayment` | Subscription payments | `RecurringPayment` |
| `RecurringPaymentHistory` | Recurring payment log | `RecurringPaymentHistory` |

**Design notes:**
- Order state machine: `OrderStatus`, `PaymentStatus`, `ShippingStatus` as explicit enums
- `Order.CustomOrderNumber` for display; `Order.Id` for internal reference
- Smartstore 6.4 renamed `ReturnRequest` → `ReturnCase` — use `ReturnCase` in our schema

### 2.6 Payment / Wallet

| Table | Purpose | Domain entity |
|---|---|---|
| `PaymentMethod` | Registered payment methods | (provider metadata, not domain) |
| `WalletHistory` | Customer wallet transactions | `WalletHistoryEntry` |
| `GiftCard` | Gift card definitions | `GiftCard` |
| `GiftCardUsageHistory` | Gift card usage log | `GiftCardUsageHistory` |

**Design notes:**
- Payment provider logic lives in plugins, not core tables
- `PaymentMethod` table stores provider registration metadata only

### 2.7 Shipping

| Table | Purpose | Domain entity |
|---|---|---|
| `ShippingMethod` | Available shipping methods | `ShippingMethod` |
| `ShippingByTotal` | Rate by order total | `ShippingRateByTotal` |
| `ShippingByWeight` | Rate by weight | `ShippingRateByWeight` |
| `DeliveryTime` | Delivery time estimates | `DeliveryTime` |

### 2.8 Tax

| Table | Purpose | Domain entity |
|---|---|---|
| `TaxCategory` | Tax classification | `TaxCategory` |
| `TaxRate` | Rate by category + location | `TaxRate` |
| `Country` | Country definitions | `Country` |
| `StateProvince` | State/province definitions | `StateProvince` |

### 2.9 Pricing / Promotions

| Table | Purpose | Domain entity |
|---|---|---|
| `Discount` | Discount definitions | `Discount` |
| `Discount_AppliedToCategories` | Discount ↔ category | (junction) |
| `Discount_AppliedToManufacturers` | Discount ↔ manufacturer | (junction) |
| `Discount_AppliedToProducts` | Discount ↔ product | (junction) |
| `DiscountUsageHistory` | Discount usage tracking | `DiscountUsageHistory` |
| `Campaign` | Marketing campaigns | `Campaign` |
| `Rule` | Rule condition/action | `Rule` |
| `RuleSet` | Grouped rules | `RuleSet` |
| `RuleSet_Category_Mapping` | Rule ↔ category | (junction) |
| `RuleSet_CustomerRole_Mapping` | Rule ↔ role | (junction) |
| `RuleSet_Discount_Mapping` | Rule ↔ discount | (junction) |
| `RuleSet_PaymentMethod_Mapping` | Rule ↔ payment | (junction) |
| `RuleSet_ShippingMethod_Mapping` | Rule ↔ shipping | (junction) |

**Design notes:**
- Rule engine uses composable conditions — do not hardcode discount logic
- `Rule` stores serialized condition/action expressions

### 2.10 Media

| Table | Purpose | Domain entity |
|---|---|---|
| `MediaFile` | File metadata | `MediaFile` |
| `MediaFolder` | Folder hierarchy | `MediaFolder` |
| `MediaStorage` | Binary storage reference | (infrastructure) |
| `MediaTag` | File tags | `MediaTag` |
| `MediaTrack` | Media tracking/analytics | `MediaTrack` |
| `MediaFile_Tag_Mapping` | File ↔ tag M:N | (junction) |

**Design notes:**
- Physical storage separated from metadata (`MediaStorage` vs `MediaFile`)
- Support local filesystem, S3, Azure Blob, MinIO via `IMediaStorage` plugin

### 2.11 CMS

| Table | Purpose | Domain entity |
|---|---|---|
| `Topic` | CMS pages/topics | `Topic` |
| `MenuRecord` | Navigation menus | `Menu` |
| `MenuItemRecord` | Menu items | `MenuItem` |

### 2.12 Localization

| Table | Purpose | Domain entity |
|---|---|---|
| `Language` | Language definitions | `Language` |
| `LocaleStringResource` | UI string translations | `LocaleStringResource` |
| `LocalizedProperty` | Entity property translations | `LocalizedProperty` |

**Design notes:**
- `LocalizedProperty` uses (`EntityId`, `LocaleKeyGroup`, `LocaleKey`, `LanguageId`, `LocaleValue`) pattern
- Persian (fa-IR) RTL support required
- ~15,272 locale resources in export — bulk import in Phase 17

### 2.13 SEO

| Table | Purpose | Domain entity |
|---|---|---|
| `UrlRecord` | SEO slugs and routing | `UrlRecord` |

**Design notes:**
- `UrlRecord` maps (`EntityName`, `EntityId`, `Slug`, `LanguageId`, `IsActive`) → slug resolution
- Language-specific slugs supported

### 2.14 Store

| Table | Purpose | Domain entity |
|---|---|---|
| `Store` | Multi-store definitions | `Store` |
| `StoreMapping` | Entity ↔ store visibility | (junction/filter) |

### 2.15 Search / Synchronization

| Table | Purpose | Domain entity |
|---|---|---|
| `SyncMapping` | External sync mappings | `SyncMapping` |
| `GoogleProduct` | Google Merchant feed | `GoogleProduct` (plugin table) |

### 2.16 Import / Export

| Table | Purpose | Domain entity |
|---|---|---|
| `ImportProfile` | Import configurations | `ImportProfile` |
| `ExportProfile` | Export configurations | `ExportProfile` |
| `ExportDeployment` | Export deployment targets | `ExportDeployment` |

### 2.17 Messaging

| Table | Purpose | Domain entity |
|---|---|---|
| `EmailAccount` | SMTP accounts | `EmailAccount` |
| `MessageTemplate` | Email templates | `MessageTemplate` |
| `QueuedEmail` | Outbound email queue | `QueuedEmail` |
| `QueuedEmailAttachment` | Email attachments | `QueuedEmailAttachment` |

### 2.18 Themes

| Table | Purpose | Domain entity |
|---|---|---|
| `ThemeVariable` | Theme customization variables | `ThemeVariable` |

---

## 3. Tables NOT Mapped to Domain Layer

These remain as persistence/infrastructure concerns:

| Category | Tables | Reason |
|---|---|---|
| Junction tables | All `*_Mapping` tables | EF many-to-many configurations |
| Migration registry | `MigrationVersionInfo` | Infrastructure-only |
| Media binary | `MediaStorage` | Storage provider concern |
| Payment metadata | `PaymentMethod` | Plugin registration, not business entity |
| Plugin tables | e.g., `GoogleProduct`, `ZarinPalTransaction` | Plugin-owned, namespaced |

---

## 4. Domain Entity Design Strategy

### Layer separation

```
Domain Entity (business concept)
  ↓ mapped by
Application Contract (DTO / read model)
  ↓ served by
Application Service (IProductService, ICategoryService, ...)
  ↓ persisted via
Infrastructure Entity (EF Core POCO, may differ from domain)
  ↓ configured by
EF Core Entity Configuration (IEntityTypeConfiguration<T>)
```

### Naming conventions

| Smartstore table | Our persistence table | Our domain entity |
|---|---|---|
| `Product` | `Products` (EF convention) | `Product` |
| `Customer` | `Customers` | `Customer` |
| `Order` | `Orders` | `Order` |
| `UrlRecord` | `UrlRecords` | `UrlRecord` |
| Plugin: `ZarinPalTransaction` | `ZarinPalTransactions` | `ZarinPalTransaction` (plugin domain) |

### Aggregate roots (initial)

| Aggregate | Root entity | Child entities |
|---|---|---|
| Catalog | `Product`, `Category` | Attributes, variants, tier prices (separate services) |
| Customer | `Customer` | Addresses (owned/value objects) |
| Cart | `ShoppingCart` (conceptual) | `ShoppingCartItem` |
| Order | `Order` | `OrderItem`, `OrderNote`, `Shipment` |
| Store | `Store` | Settings (via `ISettingService`) |
| CMS | `Topic`, `Menu` | `MenuItem` |

---

## 5. Database Provider Strategy

| Provider | Role | Notes |
|---|---|---|
| SQL Server | Primary | Smartstore compatibility; production default |
| PostgreSQL | Secondary | Provider abstraction in `Commerce.Framework.Data`; use Npgsql where practical |

**Provider abstraction:**
```csharp
// Commerce.Framework.Data
public interface IDatabaseProvider
{
    DbProvider Provider { get; }
    void Configure(DbContextOptionsBuilder options, string connectionString);
    string GetMigrationAssembly();
}
```

EF Core configurations must avoid SQL Server-specific types where possible. Use:
- `decimal(18,4)` for money (both providers)
- `datetimeoffset` or `datetime2` for timestamps (UTC)
- `nvarchar` → `string` with max length (provider-agnostic)

---

## 6. Smartstore Import Mapping Strategy (Phase 17)

Import is **never** raw SQL execution. Each bounded context gets an explicit importer:

| Importer | Source tables | Validation |
|---|---|---|
| `SmartstoreSettingImporter` | `Setting` | Count ≈ 703 |
| `SmartstoreLanguageImporter` | `Language` | Count ≈ 4 |
| `SmartstoreCurrencyImporter` | `Currency` | Count ≈ 15 |
| `SmartstoreCustomerImporter` | `Customer`, `CustomerRole*`, `Address` | Count ≈ 7 customers |
| `SmartstoreCategoryImporter` | `Category`, templates | Count ≈ 11 |
| `SmartstoreProductImporter` | `Product`, attributes, variants, bundles, mappings | Count ≈ 15 |
| `SmartstoreManufacturerImporter` | `Manufacturer` | As present |
| `SmartstoreMediaImporter` | `MediaFile`, `MediaFolder`, mappings | Count ≈ 36 |
| `SmartstoreOrderImporter` | `Order`, `OrderItem` | Count ≈ 8 |
| `SmartstoreDiscountImporter` | `Discount`, rule sets | As present |
| `SmartstoreLocalizationImporter` | `LocaleStringResource`, `LocalizedProperty` | Count ≈ 15,272 + 114 |
| `SmartstoreTopicImporter` | `Topic` | Count ≈ 20 |
| `SmartstoreMenuImporter` | `MenuRecord`, `MenuItemRecord` | As present |
| `SmartstoreUrlImporter` | `UrlRecord` | Count ≈ 97 |
| `SmartstoreStoreImporter` | `Store`, `StoreMapping` | Count ≈ 3 |

**Import pipeline:**
1. Parse SQL export into in-memory datasets (table → rows)
2. Map Smartstore IDs to commerce IDs (maintain ID mapping table)
3. Import in dependency order (stores → languages → categories → products → customers → orders)
4. Validate record counts against expected totals
5. Report discrepancies

---

## 7. Plugin Table Namespacing

Plugin-owned tables use plugin system name as prefix:

| Plugin | Example table |
|---|---|
| `Payment.ZarinPal` | `ZarinPalTransaction` |
| `Payment.Stripe` | `StripePaymentIntent` |
| `Search.Elasticsearch` | `ElasticsearchIndexMapping` |
| `Marketing.Telegram` | `TelegramCampaign` |

Plugin migrations register through `ICommerceMigration` with plugin scope. Core migrations never create plugin tables.

---

## 8. Index and Performance Considerations

Based on Smartstore query patterns (conceptual):

| Table | Recommended indexes |
|---|---|
| `Product` | `Published`, `Deleted`, `ProductTypeId`, `ParentGroupedProductId` |
| `Category` | `ParentCategoryId`, `TreePath`, `Published`, `DisplayOrder` |
| `UrlRecord` | `(Slug, LanguageId, IsActive)`, `(EntityName, EntityId)` |
| `LocaleStringResource` | `(LanguageId, ResourceName)` |
| `LocalizedProperty` | `(EntityId, LocaleKeyGroup, LanguageId)` |
| `Setting` | `(Name, StoreId)` |
| `Order` | `CustomerId`, `CreatedOnUtc`, `OrderStatusId` |
| `ShoppingCartItem` | `CustomerId`, `ProductId` |

---

## 9. Pending Validation

When `scriptWithData.sql` is placed in the repository:

- [ ] Parse all `CREATE TABLE` statements to confirm 118 tables
- [ ] Verify column names and types against this map
- [ ] Count INSERT statements per table against expected totals
- [ ] Identify any tables not classified in this document
- [ ] Document FK relationships for import ordering

**Recommended location:** `data/smartstore/scriptWithData.sql` or `docs/commerce/data/scriptWithData.sql`
