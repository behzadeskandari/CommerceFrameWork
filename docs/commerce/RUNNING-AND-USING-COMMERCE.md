# Running and Using Commerce

**Based on:** Actual repository inspection (2026-08-13). Commands and URLs are taken from project files — not invented.

---

## 1. System Requirements

| Component | Version (from repo) |
|-----------|---------------------|
| .NET SDK | **10.0** (`TargetFramework: net10.0` in `src/Commerce/Directory.Build.props`) |
| Node.js | Required for Angular (project uses Angular **19.2**) |
| npm | Required (`frontend/commerce-ui/package.json`) |
| SQL Server | **2022** (Docker: `mcr.microsoft.com/mssql/server:2022-latest`) |
| Redis | **7** (optional locally; required in Docker compose for cache) |
| Docker | Optional but recommended (`deploy/docker/`) |

No `global.json` pin found — use SDK matching `net10.0`.

---

## 2. Repository Structure

```
CommerceFrameWork/
├── Commerce.sln                 # Main backend solution
├── src/Commerce/
│   ├── Host/Commerce.Host/      # ASP.NET Core API host
│   ├── Framework/               # Core, Data, Plugins, etc.
│   ├── Modules/                 # 29 business modules
│   ├── Plugins/                 # Payment, shipping, search, theme plugins
│   └── PluginSdk/               # Plugin CLI + SDK
├── tests/Commerce/              # 18 test projects
├── frontend/commerce-ui/        # Angular admin + storefront
├── deploy/docker/               # Docker Compose stacks
├── scripts/                     # test, migration, deploy scripts
├── data/smartstore/             # Smartstore import placeholder (no SQL file yet)
└── docs/commerce/               # Phase and operations documentation
```

---

## 3. Required Infrastructure

### Minimum (local IDE development)

- **SQL Server** (local instance or Docker) — primary database provider
- File system write access for `App_Data/media`, `App_Data/backups`, `Plugins/`

### Docker Compose stack (`deploy/docker/docker-compose.yml`)

- **sqlserver** — SQL Server 2022
- **redis** — Redis 7 with persistence
- **commerce** — built from `deploy/docker/Dockerfile`

### Optional

- SMTP for real email (notifications module supports providers; dev may log-only)
- Caddy (staging/production compose files) for HTTPS

---

## 4. Configuration

### Backend (`appsettings.json` + environment variables)

Nested keys use `__` in environment variables (ASP.NET Core convention).

| Setting | Example placeholder |
|---------|---------------------|
| Database provider | `Commerce__Database__Provider=SqlServer` |
| Connection string | `Commerce__Database__ConnectionString=<YOUR_CONNECTION_STRING>` |
| Public base URL | `Commerce__BaseUrl=https://localhost:5100` |
| Cache | `Commerce__Cache__Provider=Memory` (local) or `Redis` |
| Redis | `Commerce__Cache__RedisConnectionString=<HOST>:6379` |

**Never commit real passwords.** Use `deploy/docker/.env` (gitignored) copied from `.env.example`.

### Frontend (`frontend/commerce-ui/libs/core/src/lib/environment.ts`)

```typescript
apiBaseUrl: 'https://localhost:5100'
```

Production: `environment.production.ts`.

### Docker `.env` (from `deploy/docker/.env.example`)

| Variable | Purpose |
|----------|---------|
| `MSSQL_SA_PASSWORD` | SQL Server SA password |
| `COMMERCE_BASE_URL` | Public API URL (install + callbacks) |

---

## 5. Database Setup

### Supported providers

| Provider | Status |
|----------|--------|
| **SqlServer** | Supported (default) |
| PostgreSql | Not supported (throws if selected) |
| InMemory | Tests only (`__InMemory__` token) |

### Installation wizard (first run)

1. Start backend (see §6).
2. Navigate to **`http://localhost:5101/installation`** or **`https://localhost:5100/installation`** (or Docker `http://localhost:8080/installation`).
3. POST steps via API (or use installation UI if present):

| Step | Endpoint |
|------|----------|
| Requirements | `POST /installation/requirements` |
| Database | `POST /installation/database` — body: `{ "provider": "SqlServer", "connectionString": "..." }` |
| Migrate | `POST /installation/migrate` |
| Seed | `POST /installation/seed` |
| Admin user | `POST /installation/admin` |
| Store | `POST /installation/store` |
| Language | `POST /installation/language` |
| Currency | `POST /installation/currency` |
| Complete | `POST /installation/complete` |

Connection string is persisted to `App_Data/commerce.database.json`.

### Docker SQL connection string (from compose)

```
Server=sqlserver,1433;Database=Commerce;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True;Encrypt=True;
```

---

## 6. Backend Startup

From repository root:

```powershell
dotnet restore Commerce.sln
dotnet build Commerce.sln -c Release
dotnet run --project src/Commerce/Host/Commerce.Host/Commerce.Host.csproj
```

**Development URLs** (`Properties/launchSettings.json`):

| URL | Protocol |
|-----|----------|
| `https://localhost:5100` | HTTPS |
| `http://localhost:5101` | HTTP |

**Health checks:**

- `GET /health/live`
- `GET /health/ready`
- `GET /health`

**Swagger:** Not configured.

---

## 7. Admin Startup

```powershell
cd frontend/commerce-ui
npm install
npm run start:admin
```

**URL:** `http://localhost:4200` (from `package.json` script `ng serve admin --port 4200`)

---

## 8. Storefront Startup

```powershell
cd frontend/commerce-ui
npm install
npm run start:storefront
```

**URL:** `http://localhost:4201`

---

## 9. Docker (full stack)

```powershell
cd deploy/docker
Copy-Item .env.example .env
# Edit .env — set MSSQL_SA_PASSWORD and COMMERCE_BASE_URL
docker compose --env-file .env up -d --build
```

**API:** `http://localhost:8080`  
**Install:** `http://localhost:8080/installation`

Clean install validation script:

```powershell
.\scripts\deploy\test-clean-install.ps1
```

---

## 10. First Login / Installation

1. Complete installation wizard (§5).
2. Admin login via **`POST /api/auth/login`** or admin UI login page (`/login`).
3. Default permissions seeded during installation seed step.

---

## 11–34. Operational Tasks

The following use **admin UI routes** where available; otherwise use **admin API** (see `src/Commerce/Host/Commerce.Host/` controllers).

| Task | Admin UI route | API fallback |
|------|----------------|--------------|
| Create store | `/stores/new` | `POST /installation/store` (install only) or store API |
| Configure currency | `/currencies` | `POST /installation/currency` or currencies API |
| Configure tax | `/tax/*` | Admin tax controllers |
| Create category | `/catalog/categories/new` | Categories API |
| Create product | `/catalog/products/new` | Products API |
| Digital product | Product form (type Digital) | Catalog API |
| Configure download | ⚠️ No dedicated admin route | `AdminProductDownloadsController` |
| Configure pricing/discounts | `/pricing/discounts` | Admin pricing API |
| Customer groups | `/pricing/customer-groups` | Pricing API |
| CMS pages/topics/menus/widgets | `/cms/*` | Cms controllers |
| Theme | `/themes` | Theme controllers |
| Search | Storefront + search plugin | Search API |
| Reviews / wishlist | `/reviews` | Reviews API |
| Promotions | `/marketing/promotions` | Promotions API |
| Notifications | `/notifications/templates` | Notifications API |
| Payment methods | `/payments/methods` | Admin payments API |
| Test checkout | Storefront | Cart → checkout flow |
| Test digital download | Storefront account downloads | Downloads API |
| Install plugin | `/plugins` | Admin plugins API |
| Configure plugin | `/plugins/:systemName` | Plugin settings API |
| Enable/disable plugin | Plugin detail page | Admin plugins API |
| Uninstall plugin | Plugin detail page | Admin plugins API |

### API-only operations (no Admin UI — use REST or scripts)

| Task | Method | Endpoint |
|------|--------|----------|
| List/create shipments | GET/POST | `/api/admin/shipping/shipments` |
| Manage returns/RMA | GET/POST | `/api/admin/orders/{id}/returns` (order lifecycle API) |
| View audit log | GET | `/api/admin/audit` |
| Analytics dashboards | GET | `/api/admin/analytics/*` |
| Backup/restore | GET/POST | `/api/admin/disaster-recovery/*` |
| Webhooks | GET/POST | `/api/admin/integration/webhooks` |
| Product download files | GET/POST/PUT | `/api/admin/downloads/products/{productId}/*` |
| Smartstore import | Script | `scripts/migration/run-smartstore-import.ps1` |

Authenticate as admin (`POST /api/auth/login`) and include the session cookie or bearer token on all admin calls.

---

## 35. Troubleshooting

| Problem | Cause | Action |
|---------|-------|--------|
| Host crashes on startup | DI validation (historical) | Ensure Phase 49+ code; check logs for circular dependency |
| `Connection string not configured` | DB not installed | Run installation database step |
| Frontend CORS errors | Wrong API URL | Match `environment.ts` to backend URL |
| HTTPS cert errors (dev) | Dev certificate | Trust dev cert: `dotnet dev-certs https --trust` |
| Redis errors with Memory cache | Cache misconfigured | Set `Commerce:Cache:Provider=Memory` for local IDE |
| Integration tests timeout | Suite hang / SQL | Stop orphaned `testhost`; integration uses in-memory DB; see Phase 50 report |
| Plugin not loading | Not in `Plugins/` folder | Build plugin; SDK copies to Host `Plugins/` |
| `scriptWithData.sql` missing | Not in repo | Add SQL file to `data/smartstore/` for import scripts |

---

## Build & Test Commands (verified)

### Backend

```powershell
dotnet build Commerce.sln -c Release
dotnet test Commerce.sln -c Release
.\scripts\test\run-verification.ps1
```

### Frontend

```powershell
cd frontend/commerce-ui
npm run build
npm run test:admin
npm run test:storefront
```

### Smartstore migration scripts

```powershell
.\scripts\migration\run-smartstore-import.ps1 -SqlFile <path-to.sql>
.\scripts\migration\run-smartstore-reconciliation.ps1
```

---

## Related documentation

- [ENVIRONMENT-CONFIGURATION.md](./ENVIRONMENT-CONFIGURATION.md)
- [DEPLOYMENT.md](./DEPLOYMENT.md)
- [COMMERCE-OPERATIONS.md](./COMMERCE-OPERATIONS.md)
- [DEVELOPER-WORKFLOW.md](./DEVELOPER-WORKFLOW.md)
