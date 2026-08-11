# Phase 5 Report — Commerce Customers & Identity

**Date:** 2026-08-11  
**Status:** Complete  
**Solution:** `Commerce.sln`

---

## 1. Identity Architecture

Phase 5 establishes a shared identity/security foundation used by both customers and administrators.

```
Commerce Identity/Security
        │
        ├── CommerceIdentityUser (ASP.NET Core Identity)
        │        authentication, password, lockout, email confirmation
        │
        ├── CommerceIdentityRole
        │        Administrator, Customer (+ future roles)
        │
        ├── Customer (commerce profile aggregate)
        │        profile, status, addresses
        │
        └── Permissions (role claims: commerce:permission)
```

### IdentityUser vs Customer

| Concern | Owner |
|---|---|
| Password hashing, lockout, login tokens | `CommerceIdentityUser` (Identity) |
| Email confirmation, reset tokens | Identity |
| Roles and permission claims | Identity roles + claims |
| Stable commerce identity (`CustomerId`) | `Customer` aggregate |
| Profile (name, phone) | `Customer` |
| Addresses | `CustomerAddress` entities |
| Active / inactive / deleted status | `Customer` |

**Link key:** `Customer.IdentityUserId` → `CommerceIdentityUser.Id` (string GUID). Email is **not** the relationship key.

**Administrators** are Identity users in the `Administrator` role. They do **not** receive a `Customer` record. This keeps admin and customer concerns separate while sharing the same authentication infrastructure.

**Guest checkout** is not implemented. The architecture allows future checkout to reference `CustomerId` for authenticated users without creating fake guest Identity accounts.

---

## 2. Authentication

### Mechanism

Cookie-based authentication via **ASP.NET Core Identity** (`AddIdentity<CommerceIdentityUser, CommerceIdentityRole>`).

- Registration and login use `UserManager` / `SignInManager`
- Browser sessions use the Identity application cookie (`Identity.Application`)
- Cookie options: `HttpOnly`, `SameSite=Lax`, `SecurePolicy=SameAsRequest`
- No custom JWT implementation in Phase 5
- No social login

### Endpoints

| Method | Route | Auth |
|---|---|---|
| POST | `/api/customers/register` | Anonymous |
| POST | `/api/customers/login` | Anonymous |
| POST | `/api/customers/logout` | Authenticated |
| GET | `/api/customers/me` | Authenticated (customer) |
| PUT | `/api/customers/me` | Authenticated (customer) |
| GET/POST/PUT/DELETE | `/api/customers/me/addresses[...]` | Authenticated (customer) |
| GET/PUT | `/api/admin/customers[...]` | Permission-based |

Registration creates:

1. `CommerceIdentityUser` with hashed password (Identity)
2. `Customer` aggregate linked by `IdentityUserId`
3. `Customer` role assignment
4. Application cookie sign-in

Email verification foundation: `EmailConfirmed = false` by default for customers; installer administrator is created with `EmailConfirmed = true`.

Password reset foundation: Identity token APIs are available through `UserManager`; no email UI in Phase 5.

---

## 3. Authorization

### Model

```
User → Roles → Permissions (role claims)
```

Permissions are stored as role claims with type `commerce:permission`.

### Infrastructure

| Component | Location |
|---|---|
| `IModulePermissionContributor` | Framework contracts — modules register their own permissions |
| `PermissionRegistry` | Collects permissions from all contributors at startup |
| `IPermissionService` | Resolves effective permissions for a user/principal |
| `RequirePermissionAttribute` | Host — `[RequirePermission("Catalog.Products.Create")]` |
| `PermissionAuthorizationHandler` | Host — evaluates permission policies |
| `PermissionPolicyProvider` | Host — dynamic `Permission:{name}` policies |
| `CommerceUserClaimsPrincipalFactory` | Adds permission + `commerce:customer_id` claims at sign-in |

### Roles (seeded)

| Role | Purpose |
|---|---|
| `Administrator` | Full access; receives all registered permissions via seeder |
| `Customer` | Registered shopper; no catalog mutation permissions |

### Permissions registered in Phase 5

**Catalog** (`CatalogPermissionContributor` — owned by Catalog module):

- `Catalog.Products.View/Create/Update/Delete`
- `Catalog.Categories.View/Create/Update/Delete`

**Customers** (`CustomersPermissionContributor`):

- `Customers.View`
- `Customers.Update`

Catalog mutations require the corresponding permission. Public GET endpoints remain anonymous.

The temporary Phase 4 **admin API key** (`CatalogAdminRequiredAttribute`, `Commerce:Catalog:AdminApiKey`) has been **removed**.

---

## 4. Customer Model

### Customer aggregate

- `CustomerId` (int, aggregate root id — stable commerce identity)
- `IdentityUserId` (string, unique link to Identity)
- `Email`, `NormalizedEmail` (unique among non-deleted customers)
- `FirstName`, `LastName`, `PhoneNumber`
- `Active`, `Deleted`
- `CreatedAtUtc`, `UpdatedAtUtc`

Status transitions: active ↔ inactive, soft delete. Invalid transitions are prevented in the domain.

### CustomerAddress

Uses the Phase 1 `Address` value object pattern (flattened columns). Supports:

- `Label`, default billing/shipping flags
- Full address fields (country, city, lines, postal code)
- Ownership enforced by `CustomerId` — APIs scope all operations to the authenticated customer's id

### Store relationship

Customers are not store-scoped in Phase 5. `Customer` has no store FK; future store association can be added without redesigning the identity link.

### Contracts (for other modules)

- `ICustomerReader` — read customer DTOs by id
- `ICurrentCustomerContext` — resolve current customer from HTTP context
- DTOs in `Commerce.Customers.Contracts`

Other modules must **not** reference `Commerce.Customers.Infrastructure`.

---

## 5. Administrator Bootstrap

Phase 2 installer administrator creation now uses the **same Identity system** as runtime:

- `IAdministratorProvisioningService` (framework contract)
- `AdministratorProvisioningService` (Customers infrastructure)
- Creates `CommerceIdentityUser` with Identity password hashing
- Assigns `Administrator` role
- No duplicate administrator table; legacy `BootstrapAdministrator` remains only as fallback when Customers module is not loaded (unit tests)

Installation order preserved: migrate → seed roles/permissions → create administrator → store/language/currency → complete → module runtime.

---

## 6. Catalog Authorization

Catalog module registers permissions via `IModulePermissionContributor`. Mutations on `ProductsController` and `CategoriesController` use `[RequirePermission(...)]`.

Integration tests verify:

- Administrator (cookie login after installation) can create catalog data
- Registered customer receives 403 on catalog mutations
- Unauthenticated requests receive 401
- Admin API key no longer works

---

## 7. Security

| Topic | Implementation |
|---|---|
| Password hashing | ASP.NET Core Identity (`PasswordHasher`) |
| Password policy | Min 8 chars, upper/lower/digit/special required |
| Lockout | 5 failed attempts, 15-minute lockout |
| Email verification | `EmailConfirmed` flag; no outbound email in Phase 5 |
| Reset tokens | Identity `UserManager` token providers (not logged) |
| CSRF | Cookie auth with standard ASP.NET Core antiforgery-ready setup |
| Sensitive logging | Passwords/tokens not logged or returned in API responses |
| Rate limiting | Not added (no existing host rate limiter to extend) |

---

## 8. Database & Migrations

All data lives in the shared Commerce database.

### New commerce tables

| Table | Purpose |
|---|---|
| `CustomerCustomer` | Customer aggregate |
| `CustomerAddress` | Customer-owned addresses |

### Identity tables (via `IdentityDbContext<CommerceIdentityUser, CommerceIdentityRole, string>`)

`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`

### Migration

`CustomersInitialMigration` registered through the Phase 3 module migration system (no-op scaffold pattern consistent with Catalog).

### Indexes

- Unique: `CustomerCustomer.IdentityUserId`
- Unique: `CustomerCustomer.NormalizedEmail` (filtered `[Deleted] = 0`)
- Index: `CustomerAddress.CustomerId`

---

## 9. Domain Events

- `CustomerRegisteredEvent`
- `CustomerUpdatedEvent`
- `CustomerDeactivatedEvent`
- `CustomerAddressAddedEvent`
- `CustomerAddressRemovedEvent`

---

## 10. Module Structure

```
src/Commerce/Modules/Customers/
├── Commerce.Customers.Domain
├── Commerce.Customers.Contracts
├── Commerce.Customers.Application
├── Commerce.Customers.Infrastructure
└── Commerce.Modules.Customers
```

Framework additions:

- `Commerce.Framework.Contracts.Security.*`
- `Commerce.Framework.Data.Identity.*`
- `CommerceDbContext` now inherits `IdentityDbContext`

**Preserved from Phase 4:** `CommerceDbContextRegistration.AddCommerceDbContext()` and `CommerceModelCacheKeyFactory` — EF model cache includes all `ICommerceModelContributor` registrations.

---

## 11. Testing

| Suite | Result |
|---|---|
| Unit Tests | 63/63 PASS |
| Architecture Tests | 15/15 PASS |
| Integration Tests | 9/9 PASS |

### Coverage highlights

- Customer domain: creation, validation, email normalization, status, addresses
- Catalog regression: permission-based admin mutations, customer forbidden, no admin key
- Customer auth flow: register → login → GET `/api/customers/me`
- Address ownership: customer B cannot read/delete customer A's address
- Architecture: Customers.Domain free of Identity/EF; Catalog does not reference Customers.Infrastructure
- Installation flow regression (with module runtime + Customers + Catalog)

---

## 12. Build Validation

```
dotnet build Commerce.sln --configuration Release
```

```
0 errors
0 warnings
```

---

## 13. Acceptance Criteria

- [x] Customer module exists
- [x] Customer aggregate implemented
- [x] Customer identity established (`CustomerId` + `IdentityUserId` link)
- [x] ASP.NET Core Identity integrated
- [x] Registration, login, logout implemented
- [x] Current customer endpoint implemented
- [x] Customer profile and addresses implemented
- [x] Address ownership enforced
- [x] Roles implemented (Administrator, Customer)
- [x] Permission foundation implemented
- [x] Module permission registration implemented
- [x] Administrator bootstrap migrated to real Identity
- [x] Catalog admin-key authentication removed/replaced
- [x] Catalog permissions registered and enforced
- [x] Customer migrations and seed implemented
- [x] Customer domain events implemented
- [x] All tests pass
- [x] Installation regression passes
- [x] Build 0 errors / 0 warnings

---

## 14. Not Implemented (Phase 5 scope boundary)

Orders, Cart, Checkout, Payments, Shipping, Tax, Promotions, CMS, Search, Media, Plugin engine, ZarinPal, Smartstore import, Admin UI, social login, guest checkout, username-based login (email-based auth only).

---

## PHASE 5 COMPLETE

```
Customer:                  PASS
Identity:                  PASS
Registration:              PASS
Authentication:            PASS
Authorization:             PASS
Roles:                     PASS
Permissions:               PASS
Addresses:                 PASS
Administrator Bootstrap:   PASS
Catalog Authorization:     PASS
Migrations:                PASS
Installation Regression:   PASS

Unit Tests:                63/63 PASS
Architecture Tests:        15/15 PASS
Integration Tests:         9/9 PASS

Build:
0 errors
0 warnings

Orders:                    NOT IMPLEMENTED
Checkout:                  NOT IMPLEMENTED
Payments:                  NOT IMPLEMENTED
Shipping:                  NOT IMPLEMENTED
Plugin Engine:             NOT IMPLEMENTED
Smartstore Import:         NOT STARTED

Next Phase:                PHASE 6
```

Wait for explicit approval before starting Phase 6.
